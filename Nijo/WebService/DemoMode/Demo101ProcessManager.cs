using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nijo.CodeGenerating;
using Nijo.SchemaParsing;
using Nijo.Util.DotnetEx;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// デモ101(publish済みWebApi単体。APIとSPAの両方を配信する)の常駐プロセスを
/// 起動・停止・再起動し、出力をSignalR経由で全クライアントへストリーミングする。
///
/// 以前は dotnet watch(WebApi) + vite dev(client) の2プロセス構成だったが、
/// fly.ioのマシン再起動(トライアルの5分強制停止等)をまたぐとdevサーバーの
/// キャッシュが壊れて起動失敗する問題があったため、publish済みの単一プロセスに変更した。
/// ビルドは <see cref="EnsureBuiltAsync"/> が Task/RELEASE_BUILD.sh を呼んで行う。
/// </summary>
public class Demo101ProcessManager : IDisposable {

    private const int MAX_LOG_LINES = 500;

    /// <summary>
    /// AIチャットへのビルドエラー差し戻し(<see cref="ClaudeAgentService.RunBuildRepairAsync"/>)に使うため、
    /// 直近のビルド(コード自動生成 + RELEASE_BUILD.sh)の出力を保持しておく最大行数。
    /// エラーはログ末尾に出るため、末尾からこの行数だけ保持する。
    /// </summary>
    private const int MAX_BUILD_LOG_LINES = 400;

    private readonly DemoModeOptions _options;
    private readonly IHubContext<DemoHub, IDemoHubClient> _hub;
    private readonly ILogger<Demo101ProcessManager> _logger;

    private readonly LongRunningProcess _webApi = new();
    private readonly object _logLock = new();
    private readonly List<(string Stream, string Line)> _recentLogs = new();

    private readonly object _buildLogLock = new();
    private readonly List<string> _lastBuildLog = new();

    public Demo101ProcessManager(DemoModeOptions options, IHubContext<DemoHub, IDemoHubClient> hub, ILogger<Demo101ProcessManager> logger) {
        _options = options;
        _hub = hub;
        _logger = logger;

        _webApi.OutputReceived += (std, line) => OnOutput("webapi", std, line);
        _webApi.Exited += code => _logger.LogWarning("demo101 WebApi process exited (code={code})", code);
    }

    public string Status { get; private set; } = "stopped";

    public IReadOnlyList<(string Stream, string Line)> RecentLogs {
        get {
            lock (_logLock) {
                return _recentLogs.ToList();
            }
        }
    }

    /// <summary>
    /// 直近のビルド(コード自動生成 + RELEASE_BUILD.sh)の出力ログ(末尾のみ)。
    /// ビルド失敗時にAIチャットへエラー内容を差し戻すために使う。
    /// </summary>
    public string LastBuildLog {
        get {
            lock (_buildLogLock) {
                return string.Join("\n", _lastBuildLog);
            }
        }
    }

    /// <summary>
    /// 起動する。publish済み成果物が無ければ先にビルドする。
    /// </summary>
    /// <returns>ビルドに成功しプロセスの起動まで到達したらtrue(起動後のヘルスチェックは待たない)</returns>
    public Task<bool> StartAsync() {
        return StartInternalAsync(forceRebuild: false);
    }

    /// <summary>
    /// 現在のプロセスを止め、成果物の有無に関わらず必ずフルビルドしなおしてから起動する。
    /// スキーマ変更後・環境リセット後など、ソースが変わった可能性がある場合に使う。
    /// </summary>
    /// <param name="resetDatabase">
    /// trueの場合、起動前にSQLiteのDBファイルを削除する。DBはファイルが存在しないときだけ
    /// 起動時に現在のデータモデルから再作成される(WebApi/Program.cs参照)ため、
    /// AIチャットでdata-modelが変更された場合、古いDBファイルを残したままだと
    /// テーブル定義の不一致で画面が実行時エラーになる。スキーマ変更起因のリビルドではtrueを渡すこと。
    /// </param>
    /// <returns>ビルドに成功しプロセスの起動まで到達したらtrue(起動後のヘルスチェックは待たない)</returns>
    public Task<bool> RebuildAndRestartAsync(bool resetDatabase = false) {
        Stop();
        if (resetDatabase) DeleteDatabaseFiles();
        return StartInternalAsync(forceRebuild: true);
    }

    /// <summary>
    /// 現在のプロセスを止めて、成果物があればそのまま(無ければビルドして)起動しなおす。
    /// 手動の「デモ101を再起動」ボタンから呼ばれる。
    /// </summary>
    public Task<bool> RestartAsync() {
        Stop();
        return StartInternalAsync(forceRebuild: false);
    }

    public void Stop() {
        _webApi.Stop();
        _ = SetStatusAsync("stopped");
    }

    private async Task<bool> StartInternalAsync(bool forceRebuild) {
        await SetStatusAsync("starting");

        if (!await EnsureBuiltAsync(forceRebuild)) {
            _logger.LogError("demo101のビルドに失敗したため起動できません。上の [release-build] ログを参照してください。");
            await SetStatusAsync("error");
            return false;
        }

        var dllPath = FindPublishedDllOrNull();
        if (dllPath == null) {
            _logger.LogError("demo101のpublish済みDLLが見つかりません。");
            await SetStatusAsync("error");
            return false;
        }

        var webApiDir = Path.Combine(_options.WorkspaceRoot, "WebApi");
        // ASPNETCORE_URLS は 0.0.0.0 でバインドする必要がある(WebApiUrlはヘルスチェック用の
        // localhost向けURLなので、そのままではバインドアドレスとして使えない)。
        var bindUrl = _options.WebApiUrl.Replace("localhost", "0.0.0.0");

        _logger.LogInformation("demo101 WebApi(単一プロセス)を起動します。dll={dll}", dllPath);
        try {
            _webApi.Start(psi => {
                psi.FileName = "dotnet";
                psi.ArgumentList.Add(dllPath);
                // contentRootをWebApiディレクトリに固定する。appsettings.jsonのDB接続文字列
                // (../DEBUG.sqlite3等)がこのディレクトリからの相対パスで書かれているため。
                psi.ArgumentList.Add("--contentRoot");
                psi.ArgumentList.Add(webApiDir);
                psi.WorkingDirectory = webApiDir;
                psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
                psi.Environment["ASPNETCORE_URLS"] = bindUrl;
            });
        } catch (Exception ex) {
            _logger.LogError(ex, "demo101 WebApi の起動に失敗しました。dll={dll}", dllPath);
            await SetStatusAsync("error");
            return false;
        }

        _ = Task.Run(WaitUntilHealthyAsync);
        return true;
    }

    /// <summary>
    /// publish済み成果物(WebApi/bin/Release/publish/*.WebApi.dll・WebApi/wwwroot/index.html)が
    /// 揃っているか確認し、無ければ(forceRebuildがtrueなら無条件に)ソースコード自動生成をかけなおしたうえで
    /// Task/RELEASE_BUILD.sh --skip-generate を実行してビルドする。
    /// </summary>
    private async Task<bool> EnsureBuiltAsync(bool forceRebuild) {
        if (!forceRebuild && IsBuilt()) {
            return true;
        }

        lock (_buildLogLock) {
            _lastBuildLog.Clear();
        }

        _logger.LogInformation("demo101 のソースコードを自動生成します。");
        if (!GenerateCode()) {
            _logger.LogError("demo101 のソースコード自動生成に失敗しました。");
            AppendBuildLog("demo101 のソースコード自動生成(nijo generate相当)に失敗しました。");
            return false;
        }

        var buildScript = Path.Combine(_options.WorkspaceRoot, "Task", "RELEASE_BUILD.sh");
        if (!File.Exists(buildScript)) {
            _logger.LogError("ビルドスクリプトが見つかりません: {path}", buildScript);
            AppendBuildLog($"ビルドスクリプトが見つかりません: {buildScript}");
            return false;
        }

        _logger.LogInformation("demo101 をビルドします({script})。数十秒〜数分かかります。", buildScript);
        var exitCode = await ProcessExtension.ExecuteProcessAsync(psi => {
            psi.FileName = "bash";
            psi.ArgumentList.Add(buildScript);
            // ソースコード自動生成は直前にこのプロセス内(GenerateCode)で実行済みのため、
            // スクリプト側では二重に実行しない。
            psi.ArgumentList.Add("--skip-generate");
            psi.WorkingDirectory = _options.WorkspaceRoot;
            // 共有デモサイトのリバースプロキシ配下(/demo/, /demo-api/)で動くようビルドする
            psi.Environment["VITE_DEMO_BASE"] = "/demo/";
            psi.Environment["VITE_API_BASE_URL"] = "/demo-api/";
        }, (std, line) => {
            AppendBuildLog(line);
            if (std == ProcessExtension.E_STD.StdErr) {
                _logger.LogWarning("[release-build] {line}", line);
            } else {
                _logger.LogInformation("[release-build] {line}", line);
            }
        }, TimeSpan.FromMinutes(5));

        if (exitCode != 0) {
            _logger.LogError("demo101 のビルドが失敗しました(exit code={code})。", exitCode);
            AppendBuildLog($"demo101 のビルドが失敗しました(exit code={exitCode})。");
            return false;
        }

        if (!IsBuilt()) {
            _logger.LogError("demo101 のビルドは成功しましたが、想定した成果物が見つかりませんでした。");
            return false;
        }

        return true;
    }

    private void AppendBuildLog(string line) {
        lock (_buildLogLock) {
            _lastBuildLog.Add(line);
            if (_lastBuildLog.Count > MAX_BUILD_LOG_LINES) {
                _lastBuildLog.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// `nijo generate` 相当の処理を、サブプロセスを起動せずこのプロセス内で直接実行する。
    /// スキーマ不正等のエラーメッセージはAIチャットへの差し戻しに使うためビルドログにも取り込む。
    /// </summary>
    private bool GenerateCode() {
        if (!GeneratedProject.TryOpen(_options.WorkspaceRoot, out var project, out var error)) {
            _logger.LogError("demo101のプロジェクトを開けませんでした: {error}", error);
            AppendBuildLog($"demo101のプロジェクトを開けませんでした: {error}");
            return false;
        }

        try {
            var rule = SchemaParseRule.Default();
            var xDocument = XDocument.Load(project.SchemaXmlPath);
            var parseContext = new SchemaParseContext(xDocument, rule, GeneratedProjectOptions.Parse(xDocument, true));
            var renderingOptions = new CodeRenderingOptions {
                AllowNotImplemented = false,
            };

            var teeLogger = new BuildLogCapturingLogger(_logger, AppendBuildLog);
            return project.GenerateCode(parseContext, renderingOptions, teeLogger);
        } catch (Exception ex) {
            // nijo.xml が整形式でない(XDocument.Loadで例外)等。AIが修正できるようログに残す。
            _logger.LogError(ex, "demo101 のソースコード自動生成で例外が発生しました。");
            AppendBuildLog($"ソースコード自動生成で例外が発生しました: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 元のロガーへ流しつつ、警告以上のメッセージをビルドログにも取り込むロガー。
    /// コード自動生成(スキーマ解析)のエラー内容をAIチャットへ差し戻せるようにするためのもの。
    /// </summary>
    private class BuildLogCapturingLogger : ILogger {
        public BuildLogCapturingLogger(ILogger inner, Action<string> capture) {
            _inner = inner;
            _capture = capture;
        }
        private readonly ILogger _inner;
        private readonly Action<string> _capture;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            _inner.Log(logLevel, eventId, state, exception, formatter);
            if (logLevel >= LogLevel.Warning) {
                _capture(formatter(state, exception) + (exception == null ? "" : $" ({exception.Message})"));
            }
        }
    }

    /// <summary>
    /// ワークスペース直下のSQLiteのDBファイル(DEBUG.sqlite3とそのWALファイル等)を削除する。
    /// DBはファイルが存在しないとき、次回起動時に現在のデータモデルから再作成され
    /// ダミーデータが投入される(WebApi/Program.cs参照)。
    /// スキーマ変更後に古いテーブル定義のDBが残ることによる実行時エラーを防ぐためのもの。
    /// </summary>
    private void DeleteDatabaseFiles() {
        try {
            foreach (var file in Directory.GetFiles(_options.WorkspaceRoot, "*.sqlite3*")) {
                File.Delete(file);
                _logger.LogInformation("スキーマ変更に伴いDBファイルを削除しました(次回起動時に再作成されます): {file}", file);
            }
        } catch (Exception ex) {
            _logger.LogWarning(ex, "DBファイルの削除に失敗しました。古いテーブル定義のまま起動する可能性があります。");
        }
    }

    private bool IsBuilt() {
        var wwwrootIndex = Path.Combine(_options.WorkspaceRoot, "WebApi", "wwwroot", "index.html");
        return FindPublishedDllOrNull() != null && File.Exists(wwwrootIndex);
    }

    /// <summary>
    /// publish出力(Task/RELEASE_BUILD.shが生成する WebApi/bin/Release/publish 配下)から
    /// "*.WebApi.dll" を探す(プロジェクト毎にアセンブリ名が異なるため)。
    /// </summary>
    private string? FindPublishedDllOrNull() {
        var publishDir = Path.Combine(_options.WorkspaceRoot, "WebApi", "bin", "Release", "publish");
        if (!Directory.Exists(publishDir)) return null;
        return Directory.GetFiles(publishDir, "*.WebApi.dll").FirstOrDefault();
    }

    private async Task WaitUntilHealthyAsync() {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        var deadline = DateTime.UtcNow.AddMinutes(3);

        while (DateTime.UtcNow < deadline) {
            if (await RespondsAsync(http, _options.WebApiUrl)) {
                _logger.LogInformation("demo101 が起動しました。");
                await SetStatusAsync("running");
                return;
            }
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        _logger.LogError(
            "demo101 が3分以内に起動しませんでした。WebApi({webApiUrl})が応答していません。" +
            "直前のプロセス出力については上の [webapi] ログを参照してください。",
            _options.WebApiUrl);
        await SetStatusAsync("error");
    }

    private static async Task<bool> RespondsAsync(HttpClient http, string url) {
        try {
            var response = await http.GetAsync(url);
            return response.IsSuccessStatusCode || (int)response.StatusCode < 500;
        } catch {
            // まだ起動していない
            return false;
        }
    }

    private async Task SetStatusAsync(string status) {
        Status = status;
        await _hub.Clients.All.DemoAppStatusChanged(status);
    }

    private void OnOutput(string stream, ProcessExtension.E_STD std, string line) {
        lock (_logLock) {
            _recentLogs.Add((stream, line));
            if (_recentLogs.Count > MAX_LOG_LINES) {
                _recentLogs.RemoveAt(0);
            }
        }
        // SignalR(画面のログタブ)だけでなくアプリのログ(fly logs等)にも流す。
        if (std == ProcessExtension.E_STD.StdErr) {
            _logger.LogWarning("[{stream}] {line}", stream, line);
        } else {
            _logger.LogInformation("[{stream}] {line}", stream, line);
        }
        _ = _hub.Clients.All.ProcessOutput(stream, line);
    }

    public void Dispose() {
        Stop();
    }
}
