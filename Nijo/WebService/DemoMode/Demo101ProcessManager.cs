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

    private readonly DemoModeOptions _options;
    private readonly IHubContext<DemoHub, IDemoHubClient> _hub;
    private readonly ILogger<Demo101ProcessManager> _logger;

    private readonly LongRunningProcess _webApi = new();
    private readonly object _logLock = new();
    private readonly List<(string Stream, string Line)> _recentLogs = new();

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
    /// 起動する。publish済み成果物が無ければ先にビルドする。
    /// </summary>
    public Task StartAsync() {
        return StartInternalAsync(forceRebuild: false);
    }

    /// <summary>
    /// 現在のプロセスを止め、成果物の有無に関わらず必ずフルビルドしなおしてから起動する。
    /// スキーマ変更後・環境リセット後など、ソースが変わった可能性がある場合に使う。
    /// </summary>
    public Task RebuildAndRestartAsync() {
        Stop();
        return StartInternalAsync(forceRebuild: true);
    }

    /// <summary>
    /// 現在のプロセスを止めて、成果物があればそのまま(無ければビルドして)起動しなおす。
    /// 手動の「デモ101を再起動」ボタンから呼ばれる。
    /// </summary>
    public Task RestartAsync() {
        Stop();
        return StartInternalAsync(forceRebuild: false);
    }

    public void Stop() {
        _webApi.Stop();
        _ = SetStatusAsync("stopped");
    }

    private async Task StartInternalAsync(bool forceRebuild) {
        await SetStatusAsync("starting");

        if (!await EnsureBuiltAsync(forceRebuild)) {
            _logger.LogError("demo101のビルドに失敗したため起動できません。上の [release-build] ログを参照してください。");
            await SetStatusAsync("error");
            return;
        }

        var dllPath = FindPublishedDllOrNull();
        if (dllPath == null) {
            _logger.LogError("demo101のpublish済みDLLが見つかりません。");
            await SetStatusAsync("error");
            return;
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
        }

        _ = Task.Run(WaitUntilHealthyAsync);
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

        _logger.LogInformation("demo101 のソースコードを自動生成します。");
        if (!GenerateCode()) {
            _logger.LogError("demo101 のソースコード自動生成に失敗しました。");
            return false;
        }

        var buildScript = Path.Combine(_options.WorkspaceRoot, "Task", "RELEASE_BUILD.sh");
        if (!File.Exists(buildScript)) {
            _logger.LogError("ビルドスクリプトが見つかりません: {path}", buildScript);
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
            if (std == ProcessExtension.E_STD.StdErr) {
                _logger.LogWarning("[release-build] {line}", line);
            } else {
                _logger.LogInformation("[release-build] {line}", line);
            }
        }, TimeSpan.FromMinutes(5));

        if (exitCode != 0) {
            _logger.LogError("demo101 のビルドが失敗しました(exit code={code})。", exitCode);
            return false;
        }

        if (!IsBuilt()) {
            _logger.LogError("demo101 のビルドは成功しましたが、想定した成果物が見つかりませんでした。");
            return false;
        }

        return true;
    }

    /// <summary>
    /// `nijo generate` 相当の処理を、サブプロセスを起動せずこのプロセス内で直接実行する。
    /// </summary>
    private bool GenerateCode() {
        if (!GeneratedProject.TryOpen(_options.WorkspaceRoot, out var project, out var error)) {
            _logger.LogError("demo101のプロジェクトを開けませんでした: {error}", error);
            return false;
        }

        var rule = SchemaParseRule.Default();
        var xDocument = XDocument.Load(project.SchemaXmlPath);
        var parseContext = new SchemaParseContext(xDocument, rule, GeneratedProjectOptions.Parse(xDocument, true));
        var renderingOptions = new CodeRenderingOptions {
            AllowNotImplemented = false,
        };

        return project.GenerateCode(parseContext, renderingOptions, _logger);
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
