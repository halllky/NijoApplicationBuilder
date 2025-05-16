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
    /// 標準出力か標準エラー出力かの別
    /// </summary>
    public enum E_STD {
        StdOut,
        StdErr,
    }

    /// <summary>
    /// Process.Startのラッパーメソッド
    /// </summary>
    /// <param name="editStartInfo">StartInfoをカスタマイズする</param>
    /// <param name="logOut">標準出力 or 標準エラー出力</param>
    /// <param name="timeout">タイムアウト。既定は30秒</param>
    /// <returns>EXIT CODE</returns>
    public static async Task<int> ExecuteProcessAsync(Action<ProcessStartInfo> editStartInfo, Action<E_STD, string> logOut, TimeSpan? timeout = null) {
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
                    logOut(E_STD.StdOut, e.Data);
                } else {
                    logOut(E_STD.StdOut, $"OutputDataReceived: Stream closed (Data is null).");
                }
            } catch (InvalidOperationException ioex) {
                // プロセス終了時などにストリームが閉じられてこの例外が発生することがあるため、無視する
                logOut(E_STD.StdOut, $"Caught InvalidOperationException in OutputDataReceived (likely harmless): {ioex.Message}");
            } catch (Exception ex) {
                logOut(E_STD.StdOut, $"EXCEPTION in OutputDataReceived: {ex.ToString()}");
            }
        };
        process.ErrorDataReceived += (sender, e) => {
            try {
                if (e.Data != null) {
                    logOut(E_STD.StdErr, e.Data);
                } else {
                    logOut(E_STD.StdErr, $"ErrorDataReceived: Stream closed (Data is null).");
                }
            } catch (InvalidOperationException ioex) {
                // プロセス終了時などにストリームが閉じられてこの例外が発生することがあるため、ログのみ出力して無視する
                logOut(E_STD.StdErr, $"Caught InvalidOperationException in ErrorDataReceived (likely harmless): {ioex.Message}");
            } catch (Exception ex) {
                logOut(E_STD.StdErr, $"EXCEPTION in ErrorDataReceived: {ex.ToString()}");
            }
        };

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        var timeoutLimit = DateTime.Now.Add(timeout ?? TimeSpan.FromSeconds(30));
        while (true) {
            if (DateTime.Now > timeoutLimit) {
                EnsureKill(process);
                process.CancelOutputRead();
                process.CancelErrorRead();
                throw new TimeoutException();

            } else if (process.HasExited) {
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
