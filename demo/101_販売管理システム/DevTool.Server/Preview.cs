using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace DevTool.Server;

/// <summary>
/// 開発中のアプリケーションを実際に起動して動かしている状態。
/// コンストラクタで受け取ったプロセス群を起動・停止する。
/// OSプロセスとログファイルハンドルを保持するため <see cref="IDisposable"/> を実装する。
/// </summary>
public class Preview : IDisposable {

    public Preview(string projectRoot, IReadOnlyList<PreviewProcess> processes) {
        ProjectRoot = projectRoot;
        _settings = processes;
    }

    /// <summary>このプレビューが対象とするプロジェクトのルートディレクトリの絶対パス</summary>
    public string ProjectRoot { get; }

    private readonly IReadOnlyList<PreviewProcess> _settings;
    private readonly ConcurrentDictionary<string, RunningProcess> _processes = new();

    /// <summary>
    /// Start / Stop / Restart は複数ステップの手続きであり、
    /// ConcurrentDictionary の個々の操作がアトミックでも手続き全体はアトミックにならない。
    /// GUIの二重クリックや、個別プロセスの操作と全体操作が同時に来ても
    /// 二重起動・停止中の横入り起動が起きないよう、この3メソッド全体を排他する。
    /// 内部処理はすべて同期処理（await を含まない）なので SemaphoreSlim ではなく lock で足りる。
    /// GetState / ReadLog はこのロックを取らない
    /// （ConcurrentDictionary自体が読み取りには安全であり、Stopの最大10秒のブロッキングに
    /// 状態表示・ログ表示のポーリングまで巻き込まないため）。
    /// </summary>
    private readonly Lock _gate = new();

    /// <summary>
    /// プロセスを起動する。既に起動中のプロセスはスキップする。
    /// <paramref name="processName"/> を指定した場合はそのプロセスのみ、未指定なら全プロセスを対象とする。
    /// </summary>
    public void Start(ILogger logger, string? processName = null) {
        lock (_gate) {
            foreach (var setting in TargetSettings(processName)) {
                StartProcess(setting, logger);
            }
        }
    }

    /// <summary>
    /// 起動中のプロセスをツリーごと停止する。
    /// <paramref name="processName"/> を指定した場合はそのプロセスのみ、未指定なら全プロセスを対象とする。
    /// </summary>
    public void Stop(ILogger logger, string? processName = null) {
        lock (_gate) {
            foreach (var name in TargetSettings(processName).Select(s => s.Name)) {
                StopProcess(name, logger);
            }
        }
    }

    /// <summary>
    /// プロセスを停止してから起動しなおす。
    /// <paramref name="processName"/> を指定した場合はそのプロセスのみ、未指定なら全プロセスを対象とする。
    /// </summary>
    public void Restart(ILogger logger, string? processName = null) {
        lock (_gate) {
            foreach (var setting in TargetSettings(processName)) {
                StopProcess(setting.Name, logger);
                StartProcess(setting, logger);
            }
        }
    }

    /// <summary>
    /// IDisposableの実装。<see cref="Stop(ILogger, string?)"/> がログ付きの本来の停止経路であり、
    /// これは using 文や呼び出し漏れに対する最終防衛ラインとして、ログ出力なしで同じ停止処理を行う。
    /// </summary>
    public void Dispose() {
        Stop(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
    }

    /// <summary>
    /// <paramref name="processName"/> が指定されていればその1件、未指定なら全件の設定を返す。
    /// </summary>
    private IEnumerable<PreviewProcess> TargetSettings(string? processName) {
        return processName == null
            ? _settings
            : _settings.Where(s => s.Name == processName);
    }

    /// <summary>起動中の各プロセスの稼働状態を返す</summary>
    public IReadOnlyList<PreviewProcessState> GetState() {
        return _processes.Values.Select(p => new PreviewProcessState {
            Name = p.Name,
            IsRunning = !p.Process.HasExited,
            ProcessId = p.Process.HasExited ? null : p.Process.Id,
            ExitCode = p.Process.HasExited ? p.Process.ExitCode : null,
        }).ToList();
    }

    /// <summary>
    /// 指定プロセスのログファイルの、指定オフセット以降の増分を読む。
    /// ファイルがオフセットより小さくなっていた場合（起動しなおしでクリアされた場合）は先頭から読み直す。
    /// </summary>
    public PreviewLogIncrement ReadLog(string processName, E_STD stdOutOrErr, long fromOffset) {
        if (!_processes.TryGetValue(processName, out var running)) {
            return new PreviewLogIncrement();
        }
        var path = stdOutOrErr == E_STD.StdOut ? running.StdoutLogPath : running.StderrLogPath;
        return ReadLogIncrement(path, fromOffset);
    }

    private void StartProcess(PreviewProcess setting, ILogger logger) {
        // 既存エントリが「稼働中」でなければ（自発終了直後でExitedの後始末が済んでいない場合を含む）起動する。
        // ContainsKeyだけで判定すると、クラッシュ後に登録が残ったままの状態を「稼働中」と誤認し、
        // 再度Start()しても永久に何も起きなくなる。
        if (_processes.TryGetValue(setting.Name, out var existing) && !existing.Process.HasExited) {
            return;
        }

        var stdoutLogPath = ResolveLogPath(setting.StdoutLog);
        var stderrLogPath = ResolveLogPath(setting.StderrLog);
        var stdoutWriter = CreateLogWriter(stdoutLogPath, setting.AppendStdout);
        var stderrWriter = CreateLogWriter(stderrLogPath, setting.AppendStderr);

        var process = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = ResolveExecutablePath(setting.FileName),
                Arguments = setting.Args,
                WorkingDirectory = Path.Combine(ProjectRoot, setting.Cwd),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            },
            EnableRaisingEvents = true,
        };
        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdoutWriter?.WriteLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderrWriter?.WriteLine(e.Data); };
        process.Exited += (_, _) => {
            logger.LogInformation("プレビュープロセス '{name}' が終了しました (ExitCode={exitCode})", setting.Name, process.ExitCode);
            stdoutWriter?.Dispose();
            stderrWriter?.Dispose();
        };

        try {
            process.Start();
        } catch (Exception ex) {
            logger.LogError(ex, "プレビュープロセス '{name}' の起動に失敗しました", setting.Name);
            stdoutWriter?.Dispose();
            stderrWriter?.Dispose();
            return;
        }
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        _processes[setting.Name] = new RunningProcess(setting.Name, process, stdoutLogPath, stderrLogPath, stdoutWriter, stderrWriter);
        logger.LogInformation("プレビュープロセス '{name}' を起動しました (PID={pid})", setting.Name, process.Id);
    }

    private void StopProcess(string name, ILogger logger) {
        if (!_processes.TryRemove(name, out var running)) return;
        try {
            if (!running.Process.HasExited) {
                running.Process.Kill(entireProcessTree: true);
                // タイムアウト付きの WaitForExit(int) は Exited イベントの配信完了を保証しないため、
                // ログライターの破棄はここで明示的に行う（Exited 側でも破棄するが Dispose は冪等なので安全）。
                running.Process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
            }
        } catch (Exception ex) {
            logger.LogWarning(ex, "プレビュープロセス '{name}' の停止に失敗しました", name);
        } finally {
            running.StdoutWriter?.Dispose();
            running.StderrWriter?.Dispose();
            running.Process.Dispose();
        }
    }

    private string? ResolveLogPath(string relativePath) {
        return string.IsNullOrEmpty(relativePath) ? null : Path.Combine(ProjectRoot, relativePath);
    }

    /// <summary>
    /// ログファイル書き込み用のライターを作成する。
    /// 書き込み中でも同時に読み取れるよう FileShare.ReadWrite で開く。
    /// </summary>
    private static StreamWriter? CreateLogWriter(string? path, bool append) {
        if (path == null) return null;

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var fileMode = append ? FileMode.Append : FileMode.Create;
        var stream = new FileStream(path, fileMode, FileAccess.Write, FileShare.ReadWrite);
        return new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
    }

    private static PreviewLogIncrement ReadLogIncrement(string? path, long fromOffset) {
        if (path == null || !File.Exists(path)) {
            return new PreviewLogIncrement();
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var length = stream.Length;
        // ファイルが起動しなおしでクリアされてオフセットより短くなっていたら先頭から読み直す
        var offset = fromOffset > length ? 0 : fromOffset;
        stream.Seek(offset, SeekOrigin.Begin);

        using var reader = new StreamReader(stream, new UTF8Encoding(false));
        return new PreviewLogIncrement {
            Text = reader.ReadToEnd(),
            Offset = length,
        };
    }

    /// <summary>
    /// 実行ファイル名をOSの実行可能ファイル探索規則に従って解決する。
    /// <see cref="ProcessStartInfo.UseShellExecute"/> = false のとき、
    /// .NET は Windows の PATHEXT による拡張子解決を行わない
    /// （"npm" を指定しても実体の "npm.cmd" を見つけられない）ため、これを補う。
    /// Windows以外では入力をそのまま返す（シェルを介さずとも実行ファイルとして解決できるため）。
    /// </summary>
    private static string ResolveExecutablePath(string fileName) {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return fileName;
        if (string.IsNullOrEmpty(fileName) || Path.IsPathRooted(fileName)) return fileName;

        var pathExtensions = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD")
            .Split(';', StringSplitOptions.RemoveEmptyEntries);
        var searchDirectories = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        // 拡張子が既に指定されている場合はそのファイル名のみを探す。無指定ならPATHEXTの全候補を試す
        var candidateNames = Path.HasExtension(fileName)
            ? [fileName]
            : pathExtensions.Select(ext => fileName + ext).ToArray();

        foreach (var directory in searchDirectories) {
            foreach (var candidateName in candidateNames) {
                var candidatePath = Path.Combine(directory, candidateName);
                if (File.Exists(candidatePath)) return candidatePath;
            }
        }

        // 見つからなければ元の指定のまま返し、以降の解決は Process.Start に委ねる
        return fileName;
    }

    private class RunningProcess {
        public RunningProcess(string name, Process process, string? stdoutLogPath, string? stderrLogPath, StreamWriter? stdoutWriter, StreamWriter? stderrWriter) {
            Name = name;
            Process = process;
            StdoutLogPath = stdoutLogPath;
            StderrLogPath = stderrLogPath;
            StdoutWriter = stdoutWriter;
            StderrWriter = stderrWriter;
        }
        public string Name { get; }
        public Process Process { get; }
        public string? StdoutLogPath { get; }
        public string? StderrLogPath { get; }
        public StreamWriter? StdoutWriter { get; }
        public StreamWriter? StderrWriter { get; }
    }
}

/// <summary>
/// 並列起動するプロセス1件の定義。
/// パスはいずれも <see cref="Preview.ProjectRoot"/> からの相対パス。
/// </summary>
/// <param name="Name">プロセスの識別名。状態取得やログ取得のキーに使う</param>
/// <param name="Cwd">作業ディレクトリ</param>
/// <param name="FileName">実行ファイル名</param>
/// <param name="Args">コマンドライン引数</param>
/// <param name="StdoutLog">標準出力の書き出し先。空文字ならログファイルを作らない</param>
/// <param name="StderrLog">標準エラー出力の書き出し先。空文字ならログファイルを作らない</param>
/// <param name="AppendStdout">true=起動の度に追記 / false=起動の度にクリア</param>
/// <param name="AppendStderr">true=起動の度に追記 / false=起動の度にクリア</param>
public record PreviewProcess(
    string Name,
    string Cwd,
    string FileName,
    string Args,
    string StdoutLog,
    string StderrLog,
    bool AppendStdout,
    bool AppendStderr);

/// <summary>標準出力か標準エラー出力かの別</summary>
public enum E_STD {
    StdOut,
    StdErr,
}

/// <summary>稼働中の1プロセスの状態</summary>
public class PreviewProcessState {
    public string Name { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
    public int? ProcessId { get; set; }
    public int? ExitCode { get; set; }
}

/// <summary>ログファイルの指定オフセット以降の増分</summary>
public class PreviewLogIncrement {
    public string Text { get; set; } = string.Empty;
    public long Offset { get; set; }
}
