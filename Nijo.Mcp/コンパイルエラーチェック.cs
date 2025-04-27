using System.Diagnostics.CodeAnalysis;

namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// ソースコード自動生成は済んでいる前提で、コンパイルエラーチェックを行う。
    /// 結果はワークディレクトリのログファイルに出力される。
    /// </summary>
    private static async Task<bool> コンパイルエラーチェック(WorkDirectory workDirectory, string nijoXmlDir) {

        workDirectory.AppendSectionTitle("コンパイルエラーチェック");

        var csharpCmd = Path.Combine(workDirectory.FullPath, "csharp_compile_check.cmd");
        var typeScriptCmd = Path.Combine(workDirectory.FullPath, "typescript_compile_check.cmd");

        RenderCmdFile(csharpCmd, $$"""
            chcp 65001
            @echo off
            setlocal
            set NO_COLOR=true
            set "PROJ_ROOT={{nijoXmlDir}}"

            @echo. >> "{{workDirectory.MainLogFile}}"
            @echo ******* C#コンパイルエラーチェック ******* >> "{{workDirectory.MainLogFile}}"

            dotnet build %PROJ_ROOT% -c Debug >> "{{workDirectory.MainLogFile}}" 2>&1

            exit /b %errorlevel%
            """);

        RenderCmdFile(typeScriptCmd, $$"""
            chcp 65001
            @echo off
            setlocal
            set NO_COLOR=true
            set "PROJ_ROOT={{nijoXmlDir}}"

            @echo. >> "{{workDirectory.MainLogFile}}"
            @echo ******* TypeScriptコンパイルエラーチェック ******* >> "{{workDirectory.MainLogFile}}"

            call tsc -b --noEmit >> "{{workDirectory.MainLogFile}}" 2>&1

            exit /b %errorlevel%
            """);

        var csharpExitCode = await ExecuteProcess(new() {
            WorkingDirectory = nijoXmlDir,
            FileName = "cmd",
            Arguments = $"/c \"{csharpCmd}\"",
            UseShellExecute = false,
        }, workDirectory, TimeSpan.FromSeconds(25));

        var typeScriptExitCode = await ExecuteProcess(new() {
            WorkingDirectory = Path.Combine(nijoXmlDir, "react"),
            FileName = "cmd",
            Arguments = $"/c \"{typeScriptCmd}\"",
            UseShellExecute = false,
        }, workDirectory, TimeSpan.FromSeconds(25));

        return csharpExitCode == 0 && typeScriptExitCode == 0;
    }
}

