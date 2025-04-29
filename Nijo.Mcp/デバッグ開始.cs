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

        // 診断用ログ出力
        workDirectory.WriteToMainLog($"開始: cmd /c を使って start-vite.mjs を起動します");

        var npmRunDir = Path.Combine(nijoXmlDir, "react");
        workDirectory.WriteToMainLog($"npm run devの作業ディレクトリ: {npmRunDir}");

        // UseShellExecute = true を使用するため StartNewProcess は使えない
        // また、UseShellExecute = true の場合は標準入出力のリダイレクトも不可のため、
        // WriteToNpmRunLog によるログ収集も機能しない
        Process? npmRun;
        try {
            var startInfo = new ProcessStartInfo {
                FileName = "cmd.exe",
                Arguments = "/c node start-vite.mjs",
                WorkingDirectory = npmRunDir,
                UseShellExecute = true,
            };
            npmRun = Process.Start(startInfo);

            if (npmRun == null) {
                workDirectory.WriteToMainLog($"[ERROR] cmd /c node start-vite.mjs プロセスの起動に失敗しました。Process.Startがnullを返しました。");
                // TODO: デバッグ中止処理を追加すべきか検討
                return false;
            }
            workDirectory.WriteToMainLog($"cmd /c node start-vite.mjs プロセスを起動しました (PID: {npmRun.Id})");

        } catch (Exception ex) {
            workDirectory.WriteToMainLog($"[ERROR] cmd /c node start-vite.mjs プロセスの起動中に例外が発生しました: {ex.Message}");
            // TODO: デバッグ中止処理を追加すべきか検討
            return false;
        }

        var dotnetRun = StartNewProcess("dotnet run", startInfo => {
            startInfo.WorkingDirectory = Path.Combine(nijoXmlDir, "WebApi");
            startInfo.FileName = "dotnet";
            startInfo.Arguments = "run --launch-profile https";
        }, workDirectory, workDirectory.WriteToDotnetRunLog);

        await File.WriteAllTextAsync(workDirectory.NpmRunPidFile, npmRun.Id.ToString());
        await File.WriteAllTextAsync(workDirectory.DotnetRunPidFile, dotnetRun.Id.ToString());

        // HTTP接続確認前に少し待機
        workDirectory.WriteToMainLog("サーバー起動待機中: 15秒間待機します");
        await Task.Delay(15000);  // 15秒に延長

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

