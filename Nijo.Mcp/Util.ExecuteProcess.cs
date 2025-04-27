using System.Diagnostics;
using System.Text;

namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// Process.Start のラッパー
    /// </summary>
    /// <param name="logName">ログ出力用の名前</param>
    /// <param name="startInfo">Process.StartInfo</param>
    /// <param name="workDirectory">ワークディレクトリ</param>
    /// <param name="timeout">タイムアウト</param>
    /// <returns>終了コード</returns>
    public static async Task<int> ExecuteProcess(
        string logName,
        ProcessStartInfo startInfo,
        WorkDirectory workDirectory,
        TimeSpan timeout) {

        var process = new Process();
        process.StartInfo = startInfo;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true; // 標準出力をリダイレクト
        process.StartInfo.RedirectStandardError = true;  // 標準エラーをリダイレクト
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

        process.OutputDataReceived += (sender, e) => {
            if (e.Data != null) workDirectory.WriteToMainLog($"[{logName} stdout] {e.Data}");
        };
        process.ErrorDataReceived += (sender, e) => {
            if (e.Data != null) workDirectory.WriteToMainLog($"[{logName} stderr] {e.Data}");
        };

        workDirectory.WriteToMainLog($"[{logName}] 開始");

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var timeoutLimit = DateTime.Now.Add(timeout);
        while (true) {
            if (DateTime.Now > timeoutLimit) {
                workDirectory.WriteToMainLog($"[{logName}] プロセスがタイムアウトしました。");
                process.Kill(entireProcessTree: true);
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
}
