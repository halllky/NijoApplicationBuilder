using System.Diagnostics;

namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// デバッグ開始。
    /// このメソッドが呼ばれる前に既にソースコード自動生成は終わっている前提。
    /// このメソッドが呼ばれる前に既にデバッグは中止されている前提。
    /// </summary>
    public static async Task<bool> デバッグ開始(WorkDirectory workDirectory, string nijoXmlDir) {

        workDirectory.WriteSectionTitle("デバッグ開始");

        var npmRun = StartNewProcess("npm run dev", startInfo => {
            startInfo.WorkingDirectory = Path.Combine(nijoXmlDir, "react");
            startInfo.FileName = "cmd";
            startInfo.Arguments = "/c \"npm.cmd run dev\"";
            startInfo.EnvironmentVariables["NO_COLOR"] = "true";
        }, workDirectory, workDirectory.WriteToNpmRunLog);

        var dotnetRun = StartNewProcess("dotnet run", startInfo => {
            startInfo.WorkingDirectory = Path.Combine(nijoXmlDir, "WebApi");
            startInfo.FileName = "dotnet";
            startInfo.Arguments = "run --launch-profile https";
        }, workDirectory, workDirectory.WriteToDotnetRunLog);

        await File.WriteAllTextAsync(workDirectory.NpmRunPidFile, npmRun.Id.ToString());
        await File.WriteAllTextAsync(workDirectory.DotnetRunPidFile, dotnetRun.Id.ToString());

        // HTTPサーバーが起動するまで待つ。
        // 一定時間経っても起動していなければ失敗と判断する。
        var timeout = TimeSpan.FromSeconds(20);
        var status = await ChecAliveDebugServer(workDirectory, timeout);
        if (!status.DotnetIsReady || !status.NpmIsReady) {
            workDirectory.WriteToMainLog("[nijo-mcp] デバッグ開始に失敗しました。");
            return false;
        }

        return true;
    }
}

