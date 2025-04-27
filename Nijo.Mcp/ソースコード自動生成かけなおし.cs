using System.Diagnostics;
using System.Text;

namespace Nijo.Mcp;

partial class NijoMcpTools {
    private static async Task<bool> ソースコード自動生成かけなおし(WorkDirectory workDirectory, string nijoXmlDir) {

        workDirectory.WriteSectionTitle("ソースコード自動生成かけなおし");

        var cmdFilePath = Path.Combine(workDirectory.DirectoryPath, "update-source-code.cmd");
        RenderCmdFile(cmdFilePath, $$"""
            chcp 65001
            @rem ↑dotnetコマンド実行時に強制的に書き換えられてしまいnpmの標準入出力が化けるので先に書き換えておく

            @echo off
            setlocal
            set NO_COLOR=true

            set "PROJ_ROOT={{nijoXmlDir}}"
            set "NIJO_ROOT={{NIJO_PROJ}}"
            set "NIJO_EXE={{NIJO_PROJ}}\bin\Debug\net9.0\nijo.exe"

            @echo.
            @echo *** nijo.exe最新化 ***
            @echo %NIJO_ROOT%
            @echo.

            dotnet build %NIJO_ROOT% -c Debug 2>&1

            if not "%errorlevel%"=="0" (
                @echo.
                @echo nijo.exeのビルドでエラーが発生しました。処理を中断します。
                exit /b 1
            )

            @echo.
            @echo *** ソースコードの自動生成のかけなおし ***
            @echo %PROJ_ROOT%
            @echo.

            %NIJO_EXE% generate %PROJ_ROOT% 2>&1

            if not "%errorlevel%"=="0" (
                @echo.
                @echo ソースコードの自動生成のかけなおしでエラーが発生しました。処理を中断します。
                exit /b 1
            )
            """);

        var exitCode = await ExecuteProcess("nijo generate", new() {
            FileName = "cmd",
            Arguments = $"/c \"{cmdFilePath}\"",
            UseShellExecute = false,
        }, workDirectory, TimeSpan.FromSeconds(25));

        return exitCode == 0;
    }
}

