using System.Diagnostics;
using System.Text;

namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// デバッグ開始。
    /// このメソッドが呼ばれる前に既にソースコード自動生成は終わっている前提。
    /// このメソッドが呼ばれる前に既にデバッグは中止されている前提。
    /// </summary>
    public static async Task<bool> デバッグ開始(WorkDirectory workDirectory, string nijoXmlDir) {

        workDirectory.WriteSectionTitle("デバッグ開始");

        var npmRunDir = Path.Combine(nijoXmlDir, "react");
        workDirectory.WriteToMainLog($"npm run devの作業ディレクトリ: {npmRunDir}");

        // npmをcmdを介して実行するスクリプトを作成
        RenderCmdFile(workDirectory.NpmRunCmdFile, $$"""
            chcp 65001
            @echo off
            setlocal

            set "NO_COLOR=true"
            set "LOG_FILE={{workDirectory.NpmRunLogFile}}"

            @echo. > "%LOG_FILE%"
            echo [%date% %time%] npm run dev開始 > "%LOG_FILE%"
            cd /d "{{npmRunDir}}"
            call npm run dev >> "%LOG_FILE%" 2>&1
            echo [%date% %time%] npm run dev終了（終了コード: %errorlevel%） >> "%LOG_FILE%"
            """);

        workDirectory.WriteToMainLog($"npmプロセス開始用のcmdファイルを作成しました: {workDirectory.NpmRunCmdFile}");

        // dotnet runをcmdを介して実行するスクリプトを作成
        var dotnetRunDir = Path.Combine(nijoXmlDir, "WebApi");
        workDirectory.WriteToMainLog($"dotnet runの作業ディレクトリ: {dotnetRunDir}");

        RenderCmdFile(workDirectory.DotnetRunCmdFile, $$"""
            chcp 65001
            @echo off
            setlocal

            set "LOG_FILE={{workDirectory.DotnetRunLogFile}}"

            @echo. > "%LOG_FILE%"
            echo [%date% %time%] dotnet run開始 > "%LOG_FILE%"
            cd /d "{{dotnetRunDir}}"
            call dotnet run --launch-profile https >> "%LOG_FILE%" 2>&1
            echo [%date% %time%] dotnet run終了（終了コード: %errorlevel%） >> "%LOG_FILE%"
            """);

        workDirectory.WriteToMainLog($"dotnetプロセス開始用のcmdファイルを作成しました: {workDirectory.DotnetRunCmdFile}");

        // cmdファイルをUseShellExecute=trueで実行
        Process? npmRun;
        try {
            var startInfo = new ProcessStartInfo {
                FileName = Path.GetFullPath(workDirectory.NpmRunCmdFile),
                UseShellExecute = true, // viteは UseShellExecute で実行しないとまともに動かない
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = npmRunDir,
            };

            workDirectory.WriteToMainLog($"npmプロセスを起動します: {workDirectory.NpmRunCmdFile}");
            npmRun = Process.Start(startInfo);

            if (npmRun == null) {
                workDirectory.WriteToMainLog($"[ERROR] npmプロセスの起動に失敗しました。Process.Startがnullを返しました。");
                return false;
            }

            workDirectory.WriteToMainLog($"npmプロセスを起動しました (PID: {npmRun.Id})");
        } catch (Exception ex) {
            workDirectory.WriteToMainLog($"[ERROR] npmプロセスの起動中に例外が発生しました: {ex.Message}");
            return false;
        }

        // dotnet runもcmdファイルをUseShellExecute=trueで実行
        Process? dotnetRun;
        try {
            var startInfo = new ProcessStartInfo {
                FileName = Path.GetFullPath(workDirectory.DotnetRunCmdFile),
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = dotnetRunDir,
            };

            workDirectory.WriteToMainLog($"dotnetプロセスを起動します: {workDirectory.DotnetRunCmdFile}");
            dotnetRun = Process.Start(startInfo);

            if (dotnetRun == null) {
                workDirectory.WriteToMainLog($"[ERROR] dotnetプロセスの起動に失敗しました。Process.Startがnullを返しました。");
                return false;
            }

            workDirectory.WriteToMainLog($"dotnetプロセスを起動しました (PID: {dotnetRun.Id})");
        } catch (Exception ex) {
            workDirectory.WriteToMainLog($"[ERROR] dotnetプロセスの起動中に例外が発生しました: {ex.Message}");
            return false;
        }

        await File.WriteAllTextAsync(workDirectory.NpmRunPidFile, npmRun.Id.ToString());
        await File.WriteAllTextAsync(workDirectory.DotnetRunPidFile, dotnetRun.Id.ToString());

        // HTTPサーバーが起動するまで待つ。
        // 一定時間経っても起動していなければ失敗と判断する。
        var timeout = TimeSpan.FromSeconds(30);
        var status = await ChecAliveDebugServer(workDirectory, timeout);
        if (!status.DotnetIsReady || !status.NpmIsReady) {
            workDirectory.WriteToMainLog("[nijo-mcp] デバッグ開始に失敗しました。");
            return false;
        }

        return true;
    }
}
