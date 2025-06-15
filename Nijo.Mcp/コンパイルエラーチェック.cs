using System.Diagnostics.CodeAnalysis;

namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// ソースコード自動生成は済んでいる前提で、コンパイルエラーチェックを行う。
    /// 結果はワークディレクトリのログファイルに出力される。
    /// </summary>
    private static async Task<bool> コンパイルエラーチェック(WorkDirectory workDirectory, string nijoXmlDir) {

        workDirectory.WriteSectionTitle("コンパイルエラーチェック");

        var csharpCmd = Path.Combine(workDirectory.DirectoryPath, "csharp_compile_check.cmd");
        var typeScriptCmd = Path.Combine(workDirectory.DirectoryPath, "typescript_compile_check.cmd");

        RenderCmdFile(csharpCmd, $$"""
            chcp 65001
            @echo off
            setlocal
            set NO_COLOR=true
            set "PROJ_ROOT={{nijoXmlDir}}"

            @echo.
            @echo *** C#コンパイルエラーチェック ***

            dotnet build %PROJ_ROOT% -c Debug 2>&1

            exit /b %errorlevel%
            """);

        RenderCmdFile(typeScriptCmd, $$"""
            chcp 65001
            @echo off
            setlocal
            set NO_COLOR=true
            set "PROJ_ROOT={{nijoXmlDir}}"

            @echo.
            @echo *** TypeScriptコンパイルエラーチェック ***

            call tsc -b --noEmit 2>&1

            exit /b %errorlevel%
            """);

        var csharpExitCode = await ExecuteProcess("dotnet build", startInfo => {
            startInfo.WorkingDirectory = nijoXmlDir;
            startInfo.FileName = "cmd";
            startInfo.Arguments = $"/c \"{csharpCmd}\"";
            startInfo.UseShellExecute = false;
        }, workDirectory, TimeSpan.FromSeconds(60));

        var typeScriptExitCode = await ExecuteProcess("tsc -b --noEmit", startInfo => {
            startInfo.WorkingDirectory = Path.Combine(nijoXmlDir, "react");
            startInfo.FileName = "cmd";
            startInfo.Arguments = $"/c \"{typeScriptCmd}\"";
            startInfo.UseShellExecute = false;
        }, workDirectory, TimeSpan.FromSeconds(60));

        return csharpExitCode == 0 && typeScriptExitCode == 0;
    }
}

