using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Nijo.Util.DotnetEx;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Nijo.Ui;

/// <summary>
/// Windows Form のUI上から、自動生成されたあとのアプリケーションのデバッグを開始したり終了したりする操作を提供する。
/// </summary>
internal class DebugTools {

    internal DebugTools(GeneratedProject project) {
        _project = project;
    }
    private readonly GeneratedProject _project;

    // とりあえずポート決め打ち
    private const string NPM_PORT = "5173";
    private const string DOTNET_PORT = "7098";

    /// <summary>
    /// UI上でデバッグ関連の操作をおこなうためのエンドポイントを構成します。
    /// </summary>
    /// <param name="app"></param>
    internal void ConfigureWebApplication(WebApplication app) {

        app.MapGet("/debug-state", DebugState);

        app.MapPost("/start-debugging", StartDebugging);
    }

    /// <summary>
    /// 現在実行中の NijoApplicationBuilder のデバッグ状態を、httpポート番号を基準に調べる。
    /// </summary>
    private static async Task DebugState(HttpContext context) {
        var consoleOutputBuilder = new StringBuilder();

        // Helper action to log to consoleOutputBuilder
        Action<string> logToConsole = (message) => {
            consoleOutputBuilder.AppendLine(message);
        };

        // Helper action for ProcessExtension.ExecuteProcessAsync
        // This will append process output to consoleOutputBuilder and also to a separate list if needed.
        Func<string, List<string>?, Action<ProcessExtension.E_STD, string>> createLogHandler =
            (processName, outputList) => {
                return (std, line) => {
                    var formattedLine = $"[{processName}-{std}] {line}";
                    consoleOutputBuilder.AppendLine(formattedLine);
                    if (std == ProcessExtension.E_STD.StdOut && outputList != null && line != null) { // line can be null if stream closed
                        outputList.Add(line);
                    }
                };
            };

        int? nodePid = null;
        int? aspPid = null;
        var netstatOutputLines = new List<string>();
        try {
            // ポート番号をベースにして実行中プロセスのpidを探す
            logToConsole("Executing: netstat -ano");
            await ProcessExtension.ExecuteProcessAsync(
                psi => {
                    psi.FileName = "netstat";
                    psi.ArgumentList.Add("-ano");
                },
                createLogHandler("netstat", netstatOutputLines),
                TimeSpan.FromSeconds(15) // Timeout for netstat
            );

            var listeningRegex = new Regex(@"\sLISTENING\s+(\d+)$");
            foreach (var line in netstatOutputLines.Where(l => l != null)) {
                if (!line.Contains($":{NPM_PORT}") && !line.Contains($":{DOTNET_PORT}")) continue;

                var match = listeningRegex.Match(line);
                if (match.Success) {
                    if (int.TryParse(match.Groups[1].Value, out var pid)) {
                        if (line.Contains($":{NPM_PORT}")) {
                            nodePid = pid;
                            logToConsole($"Found Node.js (port {NPM_PORT}) PID: {pid} from line: {line.Trim()}");
                        } else if (line.Contains($":{DOTNET_PORT}")) {
                            aspPid = pid;
                            logToConsole($"Found ASP.NET Core (port {DOTNET_PORT}) PID: {pid} from line: {line.Trim()}");
                        }
                    }
                }
            }

            // tasklist を使って Node.js のpidの詳細情報を得る
            if (nodePid.HasValue) {
                logToConsole($"Executing: tasklist /fi \"pid eq {nodePid.Value}\" /nh /fo csv");
                await ProcessExtension.ExecuteProcessAsync(
                    psi => {
                        psi.FileName = "tasklist";
                        psi.ArgumentList.Add("/fi");
                        psi.ArgumentList.Add($"pid eq {nodePid.Value}");
                        psi.ArgumentList.Add("/nh");
                        psi.ArgumentList.Add("/fo");
                        psi.ArgumentList.Add("csv");
                    },
                    createLogHandler($"tasklist-node({nodePid.Value})", null),
                    TimeSpan.FromSeconds(10)
                );
            } else {
                logToConsole($"Node.js PID (port {NPM_PORT}) not found or process not listening. Skipping tasklist.");
            }

            // tasklist を使って ASP.NET Core のpidの詳細情報を得る
            if (aspPid.HasValue) {
                logToConsole($"Executing: tasklist /fi \"pid eq {aspPid.Value}\" /nh /fo csv");
                await ProcessExtension.ExecuteProcessAsync(
                    psi => {
                        psi.FileName = "tasklist";
                        psi.ArgumentList.Add("/fi");
                        psi.ArgumentList.Add($"pid eq {aspPid.Value}");
                        psi.ArgumentList.Add("/nh");
                        psi.ArgumentList.Add("/fo");
                        psi.ArgumentList.Add("csv");
                    },
                    createLogHandler($"tasklist-asp({aspPid.Value})", null),
                    TimeSpan.FromSeconds(10)
                );
            } else {
                logToConsole($"ASP.NET Core PID (port {DOTNET_PORT}) not found or process not listening. Skipping tasklist.");
            }

        } catch (TimeoutException tex) {
            logToConsole($"A process execution timed out: {tex.Message}");
        } catch (Exception ex) {
            logToConsole($"An error occurred while gathering debug process info: {ex.ToString()}");
        }

        var state = new DebugProcessState {
            EstimatedPidOfNodeJs = nodePid,
            EstimatedPidOfAspNetCore = aspPid,
            NodeJsDebugUrl = $"http://localhost:{NPM_PORT}",
            AspNetCoreDebugUrl = $"https://localhost:{DOTNET_PORT}/swagger",
            ConsoleOut = consoleOutputBuilder.ToString(),
        };
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(state, context.RequestAborted);
    }

    /// <summary>
    /// デバッグを別プロセスで開始する。
    /// </summary>
    private async Task StartDebugging(HttpContext context) {

        //var npmRunDir = _project.ReactProjectRoot;
        //workDirectory.WriteToMainLog($"npm run devの作業ディレクトリ: {npmRunDir}");

        //// npmをcmdを介して実行するスクリプトを作成
        //ProcessExtension.RenderCmdFile(workDirectory.NpmRunCmdFile, $$"""
        //    chcp 65001
        //    @echo off
        //    setlocal

        //    set "NO_COLOR=true"
        //    set "LOG_FILE={{workDirectory.NpmRunLogFile}}"

        //    @echo. > "%LOG_FILE%"
        //    echo [%date% %time%] npm run dev開始 > "%LOG_FILE%"
        //    cd /d "{{npmRunDir}}"
        //    call npm run dev >> "%LOG_FILE%" 2>&1
        //    echo [%date% %time%] npm run dev終了（終了コード: %errorlevel%） >> "%LOG_FILE%"
        //    """);

        //workDirectory.WriteToMainLog($"npmプロセス開始用のcmdファイルを作成しました: {workDirectory.NpmRunCmdFile}");

        //// dotnet runをcmdを介して実行するスクリプトを作成
        //var dotnetRunDir = _project.WebapiProjectRoot;
        //workDirectory.WriteToMainLog($"dotnet runの作業ディレクトリ: {dotnetRunDir}");

        //ProcessExtension.RenderCmdFile(workDirectory.DotnetRunCmdFile, $$"""
        //    chcp 65001
        //    @echo off
        //    setlocal

        //    set "LOG_FILE={{workDirectory.DotnetRunLogFile}}"

        //    @echo. > "%LOG_FILE%"
        //    echo [%date% %time%] dotnet run開始 > "%LOG_FILE%"
        //    cd /d "{{dotnetRunDir}}"
        //    call dotnet run --launch-profile https >> "%LOG_FILE%" 2>&1
        //    echo [%date% %time%] dotnet run終了（終了コード: %errorlevel%） >> "%LOG_FILE%"
        //    """);

        //workDirectory.WriteToMainLog($"dotnetプロセス開始用のcmdファイルを作成しました: {workDirectory.DotnetRunCmdFile}");

        //// cmdファイルをUseShellExecute=trueで実行
        //Process? npmRun;
        //try {
        //    var startInfo = new ProcessStartInfo {
        //        FileName = Path.GetFullPath(workDirectory.NpmRunCmdFile),
        //        UseShellExecute = true, // viteは UseShellExecute で実行しないとまともに動かない
        //        WindowStyle = ProcessWindowStyle.Hidden,
        //        WorkingDirectory = npmRunDir,
        //    };

        //    workDirectory.WriteToMainLog($"npmプロセスを起動します: {workDirectory.NpmRunCmdFile}");
        //    npmRun = Process.Start(startInfo);

        //    if (npmRun == null) {
        //        workDirectory.WriteToMainLog($"[ERROR] npmプロセスの起動に失敗しました。Process.Startがnullを返しました。");
        //        return false;
        //    }

        //    workDirectory.WriteToMainLog($"npmプロセスを起動しました (PID: {npmRun.Id})");
        //} catch (Exception ex) {
        //    workDirectory.WriteToMainLog($"[ERROR] npmプロセスの起動中に例外が発生しました: {ex.Message}");
        //    return false;
        //}

        //// dotnet runもcmdファイルをUseShellExecute=trueで実行
        //Process? dotnetRun;
        //try {
        //    var startInfo = new ProcessStartInfo {
        //        FileName = Path.GetFullPath(workDirectory.DotnetRunCmdFile),
        //        UseShellExecute = true,
        //        WindowStyle = ProcessWindowStyle.Hidden,
        //        WorkingDirectory = dotnetRunDir,
        //    };

        //    workDirectory.WriteToMainLog($"dotnetプロセスを起動します: {workDirectory.DotnetRunCmdFile}");
        //    dotnetRun = Process.Start(startInfo);

        //    if (dotnetRun == null) {
        //        workDirectory.WriteToMainLog($"[ERROR] dotnetプロセスの起動に失敗しました。Process.Startがnullを返しました。");
        //        return false;
        //    }

        //    workDirectory.WriteToMainLog($"dotnetプロセスを起動しました (PID: {dotnetRun.Id})");
        //} catch (Exception ex) {
        //    workDirectory.WriteToMainLog($"[ERROR] dotnetプロセスの起動中に例外が発生しました: {ex.Message}");
        //    return false;
        //}

        //await File.WriteAllTextAsync(workDirectory.NpmRunPidFile, npmRun.Id.ToString());
        //await File.WriteAllTextAsync(workDirectory.DotnetRunPidFile, dotnetRun.Id.ToString());

        //// HTTPサーバーが起動するまで待つ。
        //// 一定時間経っても起動していなければ失敗と判断する。
        //var timeout = TimeSpan.FromSeconds(30);
        //var status = await ChecAliveDebugServer(workDirectory, timeout);
        //if (!status.DotnetIsReady || !status.NpmIsReady) {
        //    workDirectory.WriteToMainLog("[nijo-mcp] デバッグ開始に失敗しました。");
        //    return false;
        //}

        //return true;
    }
}

/// <summary>
/// 現在実行中のNijoApplicationBuilderのデバッグプロセスの状態。
/// このクラスのデータ構造はTypeScript側と合わせる必要あり
/// </summary>
public class DebugProcessState {
    /// <summary>
    /// 現在実行中のNijoApplicationBuilderのNode.jsのデバッグプロセスと推測されるPID
    /// </summary>
    [JsonPropertyName("estimatedPidOfNodeJs")]
    public int? EstimatedPidOfNodeJs { get; set; }
    /// <summary>
    /// 現在実行中のNijoApplicationBuilderのASP.NET Coreのデバッグプロセスと推測されるPID
    /// </summary>
    [JsonPropertyName("estimatedPidOfAspNetCore")]
    public int? EstimatedPidOfAspNetCore { get; set; }
    /// <summary>
    /// Node.jsのデバッグURL
    /// </summary>
    [JsonPropertyName("nodeJsDebugUrl")]
    public string? NodeJsDebugUrl { get; set; }
    /// <summary>
    /// ASP.NET CoreのデバッグURL（swagger-ui）
    /// </summary>
    [JsonPropertyName("aspNetCoreDebugUrl")]
    public string? AspNetCoreDebugUrl { get; set; }
    /// <summary>
    /// PID推測時のコンソール出力
    /// </summary>
    [JsonPropertyName("consoleOut")]
    public string ConsoleOut { get; set; } = string.Empty;
}
