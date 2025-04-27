using System.Diagnostics;

namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// Process.Start のラッパー
    /// </summary>
    /// <param name="startInfo">Process.StartInfo</param>
    /// <param name="workDirectory">ワークディレクトリ</param>
    /// <param name="timeout">タイムアウト</param>
    /// <returns>終了コード</returns>
    public static async Task<int> ExecuteProcess(
        ProcessStartInfo startInfo,
        WorkDirectory workDirectory,
        TimeSpan timeout) {

        var process = new Process();
        process.StartInfo = startInfo;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true; // 標準出力をリダイレクト
        process.StartInfo.RedirectStandardError = true;  // 標準エラーをリダイレクト

        process.OutputDataReceived += (sender, e) => {
            if (e.Data != null) workDirectory.AppendToMainLog($"[{startInfo.FileName} stdout] {e.Data}");
        };
        process.ErrorDataReceived += (sender, e) => {
            if (e.Data != null) workDirectory.AppendToMainLog($"[{startInfo.FileName} stderr] {e.Data}");
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        workDirectory.AppendToMainLog($"[{startInfo.FileName}] 開始");

        process.Start();

        var timeoutLimit = DateTime.Now.Add(timeout);
        while (true) {
            if (DateTime.Now > timeoutLimit) {
                workDirectory.AppendToMainLog($"[{startInfo.FileName}] プロセスがタイムアウトしました。");
                process.Kill(entireProcessTree: true);
                process.CancelOutputRead();
                process.CancelErrorRead();
                return 1;

            } else if (process.HasExited) {
                workDirectory.AppendToMainLog($"[{startInfo.FileName}] 終了");
                process.CancelOutputRead();
                process.CancelErrorRead();
                return process.ExitCode;

            } else {
                workDirectory.AppendToMainLog($"[{startInfo.FileName}] 待機中...");
                await Task.Delay(100);
            }
        }
    }
}