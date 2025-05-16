using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.DotnetEx;

public static class ProcessExtension {

    /// <summary>
    /// Process.Startのラッパーメソッド
    /// </summary>
    /// <param name="logName">ログ出力名</param>
    /// <param name="editStartInfo">StartInfoをカスタマイズする</param>
    /// <param name="logOut">ログ</param>
    /// <param name="timeout">タイムアウト。既定は30秒</param>
    /// <returns>EXIT CODE</returns>
    public static async Task<int> ExecuteProcessAsync(string logName, Action<ProcessStartInfo> editStartInfo, Action<string> logOut, TimeSpan? timeout = null) {
        using var process = new Process();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true; // 標準出力をリダイレクト
        process.StartInfo.RedirectStandardError = true;  // 標準エラーをリダイレクト
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
        editStartInfo(process.StartInfo);

        process.OutputDataReceived += (sender, e) => {
            try {
                if (e.Data != null) {
                    logOut($"[{logName} stdout] {e.Data}");
                } else {
                    logOut($"[{logName} event] OutputDataReceived: Stream closed (Data is null).");
                }
            } catch (InvalidOperationException ioex) {
                // プロセス終了時などにストリームが閉じられてこの例外が発生することがあるため、無視する
                logOut($"[{logName} event] Caught InvalidOperationException in OutputDataReceived (likely harmless): {ioex.Message}");
            } catch (Exception ex) {
                logOut($"[{logName} event] EXCEPTION in OutputDataReceived: {ex.ToString()}");
            }
        };
        process.ErrorDataReceived += (sender, e) => {
            try {
                if (e.Data != null) {
                    logOut($"[{logName} stderr] {e.Data}");
                } else {
                    logOut($"[{logName} event] ErrorDataReceived: Stream closed (Data is null).");
                }
            } catch (InvalidOperationException ioex) {
                // プロセス終了時などにストリームが閉じられてこの例外が発生することがあるため、ログのみ出力して無視する
                logOut($"[{logName} event] Caught InvalidOperationException in ErrorDataReceived (likely harmless): {ioex.Message}");
            } catch (Exception ex) {
                logOut($"[{logName} event] EXCEPTION in ErrorDataReceived: {ex.ToString()}");
            }
        };

        logOut($"[{logName}] 開始");

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        var timeoutLimit = DateTime.Now.Add(timeout ?? TimeSpan.FromSeconds(30));
        while (true) {
            if (DateTime.Now > timeoutLimit) {
                logOut($"[{logName}] プロセスがタイムアウトしました。");
                EnsureKill(process);
                process.CancelOutputRead();
                process.CancelErrorRead();
                return 1;

            } else if (process.HasExited) {
                logOut($"[{logName}] 終了（終了コード: {process.ExitCode}）");
                process.CancelOutputRead();
                process.CancelErrorRead();
                return process.ExitCode;

            } else {
                await Task.Delay(100);
            }
        }
    }

    /// <summary>
    /// プロセスツリーを確実に終了させます。
    /// </summary>
    /// <returns>処理結果</returns>
    public static string EnsureKill(this Process process) {
        int? pid = null;
        try {
            if (process.HasExited) return "Process is already exited. taskkill is skipped.";

            pid = process.Id;

            var kill = new Process();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                kill.StartInfo.FileName = "taskkill";
                kill.StartInfo.ArgumentList.Add("/PID");
                kill.StartInfo.ArgumentList.Add(pid.ToString()!);
                kill.StartInfo.ArgumentList.Add("/T");
                kill.StartInfo.ArgumentList.Add("/F");
            } else {
                kill.StartInfo.FileName = "kill";
                kill.StartInfo.ArgumentList.Add(pid.ToString()!);
            }
            kill.StartInfo.RedirectStandardOutput = true;
            kill.StartInfo.RedirectStandardError = true;

            kill.Start();
            kill.WaitForExit(TimeSpan.FromSeconds(5));

            if (kill.ExitCode == 0) {
                return $"Success to task kill (PID = {pid})";
            } else {
                return $"Exit code of TASKKILL is '{kill.ExitCode}' (PID = {pid})";
            }

        } catch (Exception ex) {
            return $"Failed to task kill (PID = {pid}): {ex.Message}";
        }
    }
}
