using System.Diagnostics;
using System.Text;

namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// Process.Start のラッパー
    /// </summary>
    /// <param name="logName">ログ出力用の名前</param>
    /// <param name="editStartInfo">Process.StartInfo</param>
    /// <param name="workDirectory">ワークディレクトリ</param>
    /// <param name="timeout">タイムアウト</param>
    /// <returns>終了コード</returns>
    private static async Task<int> ExecuteProcess(string logName, Action<ProcessStartInfo> editStartInfo, WorkDirectory workDirectory, TimeSpan timeout) {
        using var process = StartNewProcess(logName, editStartInfo, workDirectory, workDirectory.WriteToMainLog);

        var timeoutLimit = DateTime.Now.Add(timeout);
        while (true) {
            if (DateTime.Now > timeoutLimit) {
                workDirectory.WriteToMainLog($"[{logName}] プロセスがタイムアウトしました。");
                await EnsureKill(process);
                process.CancelOutputRead();
                process.CancelErrorRead();
                return 1;

            } else if (process.HasExited) {
                workDirectory.WriteToMainLog($"[{logName}] 終了（終了コード: {process.ExitCode}）");
                process.CancelOutputRead();
                process.CancelErrorRead();
                return process.ExitCode;

            } else {
                await Task.Delay(100);
            }
        }
    }
    /// <summary>
    /// Process.Start のラッパー
    /// </summary>
    /// <param name="logName">ログ出力用の名前</param>
    /// <param name="editStartInfo">Process.StartInfo の編集</param>
    /// <param name="workDirectory">ワークディレクトリ</param>
    /// <returns>終了コード</returns>
    private static Process StartNewProcess(string logName, Action<ProcessStartInfo> editStartInfo, WorkDirectory workDirectory, Action<string> outLog) {
        var process = new Process();
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
                    outLog($"[{logName} stdout] {e.Data}");
                } else {
                    workDirectory.WriteToMainLog($"[{logName} event] OutputDataReceived: Stream closed (Data is null).");
                }
            } catch (InvalidOperationException ioex) {
                // プロセス終了時などにストリームが閉じられてこの例外が発生することがあるため、ログのみ出力して無視する
                workDirectory.WriteToMainLog($"[{logName} event] Caught InvalidOperationException in OutputDataReceived (likely harmless): {ioex.Message}");
            } catch (Exception ex) {
                workDirectory.WriteToMainLog($"[{logName} event] EXCEPTION in OutputDataReceived: {ex.ToString()}");
            }
        };
        process.ErrorDataReceived += (sender, e) => {
            try {
                if (e.Data != null) {
                    outLog($"[{logName} stderr] {e.Data}");
                } else {
                    workDirectory.WriteToMainLog($"[{logName} event] ErrorDataReceived: Stream closed (Data is null).");
                }
            } catch (InvalidOperationException ioex) {
                // プロセス終了時などにストリームが閉じられてこの例外が発生することがあるため、ログのみ出力して無視する
                workDirectory.WriteToMainLog($"[{logName} event] Caught InvalidOperationException in ErrorDataReceived (likely harmless): {ioex.Message}");
            } catch (Exception ex) {
                workDirectory.WriteToMainLog($"[{logName} event] EXCEPTION in ErrorDataReceived: {ex.ToString()}");
            }
        };

        workDirectory.WriteToMainLog($"[{logName}] 開始");

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    /// <summary>
    /// プロセスツリーを確実に終了させます。
    /// </summary>
    /// <returns>処理結果</returns>
    private static async Task<string> EnsureKill(Process process) {
        int? pid = null;
        try {
            if (process.HasExited) return "Process is already exited. taskkill is skipped.";

            pid = process.Id;
            // 対象プロセスの情報をログ出力
            Console.Error.WriteLine($"[NijoMcpTools.EnsureKill] Killing process info - PID: {process.Id}, Name: {process.ProcessName}"); // 標準エラーに出力

            var kill = new Process();
            kill.StartInfo.FileName = "taskkill";
            kill.StartInfo.ArgumentList.Add("/PID");
            kill.StartInfo.ArgumentList.Add(pid.ToString()!);
            kill.StartInfo.ArgumentList.Add("/T");
            kill.StartInfo.ArgumentList.Add("/F");
            kill.StartInfo.RedirectStandardOutput = true;
            kill.StartInfo.RedirectStandardError = true;

            kill.Start();
            bool exited = await Task.Run(() => kill.WaitForExit(TimeSpan.FromSeconds(5)));

            if (!exited) {
                return $"taskkill timed out after 5 seconds (PID = {pid})";
            }

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
