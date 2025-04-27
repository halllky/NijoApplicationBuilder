namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// デバッグ開始。
    /// このメソッドが呼ばれる前に既にソースコード自動生成は終わっている前提。
    /// このメソッドが呼ばれる前に既にデバッグは中止されている前提。
    /// </summary>
    public static async Task<bool> デバッグ開始(WorkDirectory workDirectory, string nijoXmlDir) {

        // 別プロセスで nijo run を起動する。ここではプロセスの完了は待たない。
        var launchCmdPath = Path.Combine(nijoXmlDir, "launch.cmd");
        RenderCmdFile(launchCmdPath, $$"""
            chcp 65001
            @rem ↑dotnetコマンド実行時に強制的に書き換えられてしまいnpmの標準入出力が化けるので先に書き換えておく

            @echo off
            setlocal
            set NO_COLOR=true

            set "NIJO_EXE={{NIJO_PROJ}}\bin\Debug\net9.0\nijo.exe"
            set "CANCEL_FILE={{workDirectory.NijoExeCancelFile}}"

            @echo. >> "{{workDirectory.MainLogFile}}"
            @echo ******* デバッグ開始 ******* >> "{{workDirectory.MainLogFile}}"
            start /B /SEPARATE "%NIJO_EXE% run --no-browser --cancel-file %CANCEL_FILE% >> {{workDirectory.DebugLogFile}} 2>&1"

            exit /b %errorlevel%
            """);

        var exitCode = await ExecuteProcess(new() {
            FileName = launchCmdPath,
            WorkingDirectory = nijoXmlDir,
        }, workDirectory, TimeSpan.FromMinutes(5));

        if (exitCode != 0) {
            workDirectory.AppendToMainLog("[nijo-mcp] デバッグ開始に失敗しました。");
            return false;
        }

        // HTTPサーバーが起動するまで待つ。
        // 一定時間経っても起動していなければ失敗と判断する。
        var timeout = TimeSpan.FromSeconds(20);
        var status = await デバッグプロセス稼働判定(workDirectory, timeout);
        if (!status.DotnetIsReady || !status.NpmIsReady) {
            workDirectory.AppendToMainLog("[nijo-mcp] デバッグ開始に失敗しました。");
            return false;
        }

        return true;
    }
}

