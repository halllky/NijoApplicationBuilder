using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nijo.Util.DotnetEx;

namespace Nijo.Previewing;

/// <summary>
/// 生成後アプリを実際に起動して動かしている状態。
/// <see cref="PreviewSetting"/>（nijo.preview.json の内容）に従ってプロセス群を起動・停止・再起動する。
/// 1インスタンスが1プロジェクト分のプレビューに対応する。
/// OSプロセスとログファイルハンドルを保持するため <see cref="IDisposable"/> を実装する。
/// </summary>
public class Preview : IDisposable {

    public Preview(string projectRoot) {
        ProjectRoot = projectRoot;
    }

    /// <summary>このプレビューが対象とするプロジェクトのルートディレクトリの絶対パス</summary>
    public string ProjectRoot { get; }

    private readonly ConcurrentDictionary<string, RunningProcess> _processes = new();

    /// <summary>
    /// Start / Stop / RestartAfterCodeGenerating は複数ステップの手続きであり、
    /// ConcurrentDictionary の個々の操作がアトミックでも手続き全体はアトミックにならない。
    /// GUIの二重クリックや、手動停止とコード再生成トリガーの再起動が同時に来ても
    /// 二重起動・停止中の横入り起動が起きないよう、この3メソッド全体を排他する。
    /// 内部処理はすべて同期処理（await を含まない）なので SemaphoreSlim ではなく lock で足りる。
    /// GetState / ReadLog はこのロックを取らない
    /// （ConcurrentDictionary自体が読み取りには安全であり、Stopの最大10秒のブロッキングに
    /// 状態表示・ログ表示のポーリングまで巻き込まないため）。
    /// </summary>
    private readonly System.Threading.Lock _gate = new();

    /// <summary>
    /// 設定に含まれるすべてのプロセスを起動し、指定があればブラウザを開く。
    /// 既に起動中のプロセスはスキップする。
    /// </summary>
    public void Start(PreviewSetting setting, ILogger logger) {
        lock (_gate) {
            foreach (var processSetting in setting.Concurrently) {
                StartProcess(processSetting, logger);
            }
            if (!string.IsNullOrWhiteSpace(setting.Browser)) {
                ProcessExtension.OpenBrowser(setting.Browser);
            }
        }
    }

    /// <summary>起動中の全プロセスをツリーごと停止する</summary>
    public void Stop(ILogger logger) {
        lock (_gate) {
            foreach (var name in _processes.Keys.ToArray()) {
                StopProcess(name, logger);
            }
        }
    }

    /// <summary>
    /// IDisposableの実装。<see cref="Stop(ILogger)"/> がログ付きの本来の停止経路であり、
    /// これは using 文や呼び出し漏れに対する最終防衛ラインとして、ログ出力なしで同じ停止処理を行う。
    /// </summary>
    public void Dispose() {
        Stop(NullLogger.Instance);
    }

    /// <summary>
    /// コード再生成が成功した直後に呼ぶ。
    /// <paramref name="setting"/> の <see cref="PreviewProcessSetting.RestartOnGenerateCode"/> が true のプロセスだけを、
    /// ツリーごと停止してから起動しなおす。
    /// </summary>
    public void RestartAfterCodeGenerating(PreviewSetting setting, ILogger logger) {
        lock (_gate) {
            foreach (var processSetting in setting.Concurrently.Where(p => p.RestartOnGenerateCode)) {
                StopProcess(processSetting.Name, logger);
                StartProcess(processSetting, logger);
            }
        }
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

    private void StartProcess(PreviewProcessSetting processSetting, ILogger logger) {
        // 既存エントリが「稼働中」でなければ（自発終了直後でExitedの後始末が済んでいない場合を含む）起動する。
        // ContainsKeyだけで判定すると、クラッシュ後に登録が残ったままの状態を「稼働中」と誤認し、
        // 再度Start()しても永久に何も起きなくなる。
        if (_processes.TryGetValue(processSetting.Name, out var existing) && !existing.Process.HasExited) {
            return;
        }

        var stdoutLogPath = ResolveLogPath(processSetting.Log.Stdout);
        var stderrLogPath = ResolveLogPath(processSetting.Log.Stderr);
        var stdoutWriter = CreateLogWriter(stdoutLogPath, processSetting.Log.AppendStdout);
        var stderrWriter = CreateLogWriter(stderrLogPath, processSetting.Log.AppendStderr);

        var process = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = ProcessExtension.ResolveExecutablePath(processSetting.Process.FileName),
                Arguments = processSetting.Process.Args,
                WorkingDirectory = Path.Combine(ProjectRoot, processSetting.Process.Cwd),
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
            logger.LogInformation("プレビュープロセス '{name}' が終了しました (ExitCode={exitCode})", processSetting.Name, process.ExitCode);
            stdoutWriter?.Dispose();
            stderrWriter?.Dispose();
        };

        try {
            process.Start();
        } catch (Exception ex) {
            logger.LogError(ex, "プレビュープロセス '{name}' の起動に失敗しました", processSetting.Name);
            stdoutWriter?.Dispose();
            stderrWriter?.Dispose();
            return;
        }
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        _processes[processSetting.Name] = new RunningProcess(processSetting.Name, process, stdoutLogPath, stderrLogPath, stdoutWriter, stderrWriter);
        logger.LogInformation("プレビュープロセス '{name}' を起動しました (PID={pid})", processSetting.Name, process.Id);
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
    /// GUI側が起動中でも同時に読み取れるよう FileShare.ReadWrite で開く。
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

/// <summary>標準出力か標準エラー出力かの別</summary>
public enum E_STD {
    StdOut,
    StdErr,
}

/// <summary>稼働中の1プロセスの状態</summary>
public class PreviewProcessState {
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("isRunning")]
    public bool IsRunning { get; set; }
    [JsonPropertyName("processId")]
    public int? ProcessId { get; set; }
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; set; }
}

/// <summary>ログファイルの指定オフセット以降の増分</summary>
public class PreviewLogIncrement {
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    [JsonPropertyName("offset")]
    public long Offset { get; set; }
}
