using System.Diagnostics;
using System.Text;

namespace Nijo.Mcp;

partial class NijoMcpTools {
    private static async Task<bool> ソースコード自動生成かけなおし(WorkDirectory workDirectory, string nijoXmlDir) {

        workDirectory.AppendSectionTitle("ソースコード自動生成かけなおし");

        var mainLogPath = workDirectory.MainLogFile;
        var cmdFilePath = Path.Combine(workDirectory.FullPath, "update-source-code.cmd");
        RenderCmdFile(cmdFilePath, $$"""
            chcp 65001
            @rem ↑dotnetコマンド実行時に強制的に書き換えられてしまいnpmの標準入出力が化けるので先に書き換えておく

            @echo off
            setlocal
            set NO_COLOR=true

            set "PROJ_ROOT={{nijoXmlDir}}"
            set "NIJO_ROOT={{NIJO_PROJ}}"
            set "NIJO_EXE={{NIJO_PROJ}}\bin\Debug\net9.0\nijo.exe"

            @echo. >> "{{mainLogPath}}"
            @echo ******* nijo.exe最新化 ******* >> "{{mainLogPath}}"
            @echo %NIJO_ROOT% >> "{{mainLogPath}}"
            @echo. >> "{{mainLogPath}}"

            dotnet build %NIJO_ROOT% -c Debug >> "{{mainLogPath}}" 2>&1

            if not "%errorlevel%"=="0" (
                @echo. >> "{{mainLogPath}}"
                @echo nijo.exeのビルドでエラーが発生しました。処理を中断します。 >> "{{mainLogPath}}"
                exit /b 1
            )

            @echo. >> "{{mainLogPath}}"
            @echo ******* ソースコードの自動生成のかけなおし ******* >> "{{mainLogPath}}"
            @echo %PROJ_ROOT% >> "{{mainLogPath}}"
            @echo. >> "{{mainLogPath}}"

            %NIJO_EXE% generate %PROJ_ROOT% >> "{{mainLogPath}}" 2>&1

            if not "%errorlevel%"=="0" (
                @echo. >> "{{mainLogPath}}"
                @echo ソースコードの自動生成のかけなおしでエラーが発生しました。処理を中断します。 >> "{{mainLogPath}}"
                exit /b 1
            )
            """);

        var exitCode = await ExecuteProcess(new() {
            FileName = "cmd",
            Arguments = $"/c \"{cmdFilePath}\"",
            UseShellExecute = false,
        }, workDirectory, TimeSpan.FromSeconds(25));

        return exitCode == 0;
    }
}

