using System.Diagnostics;
using System.Text;

namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// `nijo run` したときにキャンセル用のファイルを指定できるので、
    /// そこにファイルを出力し、完了まで一定時間待つ。
    /// </summary>
    public static async Task<bool> 既存デバッグプロセス中止(WorkDirectory workDirectory) {

        workDirectory.WriteSectionTitle("既存デバッグプロセス中止");

        var shouldCheckHttp = false;

        // PIDでプロセスを探してkill
        try {
            var pid = workDirectory.ReadNpmRunPidFile();
            if (pid == null) {
                workDirectory.WriteToMainLog($"Node.js サーバーは起動されていないか、nijo-mcp以外によって起動されたため停止できません。");

            } else {
                var process = Process.GetProcessById(pid.Value);

                if (process == null) {
                    workDirectory.WriteToMainLog($"Node.js サーバーのPID {pid} を停止しようとしましたが、プロセスが見つかりません。");

                } else {
                    workDirectory.WriteToMainLog($"Node.js サーバーのPID {pid} を停止します。");
                    var result = EnsureKill(process);
                    workDirectory.WriteToMainLog(result);
                    shouldCheckHttp = true;
                }
                workDirectory.DeleteNpmRunPidFile();
            }
        } catch (Exception ex) {
            workDirectory.WriteToMainLog($"npm run のtaskkillで例外: {ex.Message}");
        }

        try {
            var pid = workDirectory.ReadDotnetRunPidFile();
            if (pid == null) {
                workDirectory.WriteToMainLog($"ASP.NET Core サーバーは起動されていないか、nijo-mcp以外によって起動されたため停止できません。");

            } else {
                var process = Process.GetProcessById(pid.Value);
                if (process == null) {
                    workDirectory.WriteToMainLog($"ASP.NET Core サーバーのPID {pid} を停止しようとしましたが、プロセスが見つかりません。");

                } else {
                    workDirectory.WriteToMainLog($"ASP.NET Core サーバーのPID {pid} を停止します。");
                    var result = EnsureKill(process);
                    workDirectory.WriteToMainLog(result);
                    shouldCheckHttp = true;
                }
                workDirectory.DeleteDotnetRunPidFile();
            }
        } catch (Exception ex) {
            workDirectory.WriteToMainLog($"dotnet run のtaskkillで例外: {ex.Message}");
        }

        // 終了したか確認
        if (shouldCheckHttp) {
            workDirectory.WriteToMainLog("Webサーバーが停止されたか確認します。");

            var timeoutLimit = DateTime.Now.AddSeconds(10);
            while (true) {
                var status = await ChecAliveDebugServer(workDirectory, TimeSpan.FromSeconds(2));

                // 両サーバーとも停止されていたら終了
                if (!status.DotnetIsReady && !status.NpmIsReady) break;

                if (DateTime.Now > timeoutLimit) {
                    workDirectory.WriteToMainLog("Webサーバーの停止に失敗しました。");
                    return false;
                }

                await Task.Delay(500);
            }
        }

        return true;
    }
}
