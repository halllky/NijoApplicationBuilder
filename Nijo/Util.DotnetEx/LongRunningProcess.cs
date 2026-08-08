using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Nijo.Util.DotnetEx;

/// <summary>
/// dotnet watch や npm run dev のような常駐プロセスを管理するためのラッパー。
/// <see cref="ProcessExtension.ExecuteProcessAsync"/> はタイムアウト前提のため、
/// 常駐プロセスにはこちらを使う。
/// </summary>
public class LongRunningProcess : IDisposable {

    private Process? _process;

    /// <summary>行単位の出力(標準出力・標準エラー出力どちらも)を通知する</summary>
    public event Action<ProcessExtension.E_STD, string>? OutputReceived;

    /// <summary>プロセスが(意図せず)終了したときに呼ばれる</summary>
    public event Action<int>? Exited;

    public bool IsRunning => _process != null && !_process.HasExited;

    /// <summary>
    /// プロセスを起動する。既に起動中の場合は何もしない。
    /// </summary>
    public void Start(Action<ProcessStartInfo> editStartInfo) {
        if (IsRunning) return;

        var process = new Process();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        editStartInfo(process.StartInfo);

        process.EnableRaisingEvents = true;
        process.OutputDataReceived += (_, e) => {
            if (e.Data != null) OutputReceived?.Invoke(ProcessExtension.E_STD.StdOut, e.Data);
        };
        process.ErrorDataReceived += (_, e) => {
            if (e.Data != null) OutputReceived?.Invoke(ProcessExtension.E_STD.StdErr, e.Data);
        };
        process.Exited += (_, _) => {
            Exited?.Invoke(process.ExitCode);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        _process = process;
    }

    /// <summary>
    /// プロセスツリーを確実に終了させる。
    /// </summary>
    public void Stop() {
        if (_process == null) return;
        _process.EnsureKill();
        _process.Dispose();
        _process = null;
    }

    public void Dispose() {
        Stop();
    }
}
