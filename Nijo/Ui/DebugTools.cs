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
using System.Reflection;

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

        app.MapPost("/start-npm-debugging", StartNpmDebugging);
        app.MapPost("/stop-npm-debugging", StopNpmDebugging);

        app.MapPost("/start-dotnet-debugging", StartDotnetDebugging);
        app.MapPost("/stop-dotnet-debugging", StopDotnetDebugging);
    }

    /// <summary>
    /// デバッグ状態を調べるだけ
    /// </summary>
    private static async Task DebugState(HttpContext context) {
        var state = await CheckDebugState();
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(state, context.RequestAborted);
    }

    /// <summary>
    /// npm run devを別プロセスで開始する。既に開始されている場合は何もしない。
    /// </summary>
    private async Task StartNpmDebugging(HttpContext context) {
        var state = await CheckDebugState();

        // 既に起動している場合は何もしない
        if (state.EstimatedPidOfNodeJs != null) {
            state.ConsoleOut += "npm run devは既に起動済みです。\n";
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(state, context.RequestAborted);
            return;
        }

        var errorSummary = new StringBuilder();
        var consoleOut = new StringBuilder();

        var npmRunDir = _project.ReactProjectRoot;

        // 一時ファイル
        var workDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "temp"));
        var npmCmdFile = Path.Combine(workDir, "npm-run-dev.cmd");
        var npmLogFile = Path.Combine(workDir, "npm-run-dev.log");

        Directory.CreateDirectory(workDir);

        consoleOut.AppendLine($"npm run devの作業ディレクトリ: {npmRunDir}");

        // npmをcmdを介して実行するスクリプトを作成
        ProcessExtension.RenderCmdFile(npmCmdFile, $$"""
            chcp 65001
            @echo off
            setlocal

            set "NO_COLOR=true"
            set "LOG_FILE={{npmLogFile}}"

            @echo. > "%LOG_FILE%"
            echo [%date% %time%] npm run dev開始 > "%LOG_FILE%"
            cd /d "{{npmRunDir}}"
            call npm run dev >> "%LOG_FILE%" 2>&1
            echo [%date% %time%] npm run dev終了（終了コード: %errorlevel%） >> "%LOG_FILE%"
            """);

        consoleOut.AppendLine($"npmプロセス開始用のcmdファイルを作成しました: {npmCmdFile}");

        // cmdファイルをUseShellExecute=trueで実行
        Process? npmRun;
        try {
            var startInfo = new ProcessStartInfo {
                FileName = Path.GetFullPath(npmCmdFile),
                UseShellExecute = true, // viteは UseShellExecute で実行しないとまともに動かない
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = npmRunDir,
            };

            consoleOut.AppendLine($"npmプロセスを起動します: {npmCmdFile}");
            npmRun = Process.Start(startInfo);

            if (npmRun == null) {
                throw new InvalidOperationException($"[ERROR] npmプロセスの起動に失敗しました。Process.Startがnullを返しました。");
            }

            consoleOut.AppendLine($"npmプロセスを起動しました (PID: {npmRun.Id})");
        } catch (Exception ex) {
            var errorState = await CheckDebugState();
            errorState.ErrorSummary = $"npmプロセスの起動中に例外が発生しました: {ex.Message}";
            errorState.ConsoleOut += consoleOut.ToString();

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(errorState, context.RequestAborted);
            return;
        }

        // 開始されるまで一定時間待つ
        var timeout = DateTime.Now.AddSeconds(10);
        while (true) {
            var currentState = await CheckDebugState();
            if (currentState.EstimatedPidOfNodeJs != null) {
                break;
            }
            if (DateTime.Now > timeout) {
                errorSummary.AppendLine("一定時間経過しましたがnpmプロセスが起動しませんでした。");
                break;
            }
            await Task.Delay(500);
        }

        // ログファイルの内容を読み込んでエラー情報として追加
        try {
            if (File.Exists(npmLogFile)) {
                var logContent = await File.ReadAllTextAsync(npmLogFile);
                consoleOut.AppendLine($"=== npm run dev ログファイルの内容 ===");
                consoleOut.AppendLine(logContent);

                // ログにエラーらしき内容が含まれている場合はエラーサマリーに追加
                if (logContent.Contains("ERROR") || logContent.Contains("error") ||
                    logContent.Contains("ENOENT") || logContent.Contains("Command failed")) {
                    errorSummary.AppendLine("npm run devの実行中にエラーが発生した可能性があります。詳細はログを確認してください。");
                }
            }
        } catch (Exception ex) {
            consoleOut.AppendLine($"ログファイルの読み込み中にエラーが発生しました: {ex.Message}");
        }

        // 起動し終わったのでpidを調べてクライアントに結果を返す
        var stateAfterLaunch = await CheckDebugState();
        stateAfterLaunch.ErrorSummary = errorSummary.ToString();
        stateAfterLaunch.ConsoleOut += consoleOut.ToString();
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(stateAfterLaunch, context.RequestAborted);
    }

    /// <summary>
    /// dotnet runを別プロセスで開始する。既に開始されている場合は何もしない。
    /// </summary>
    private async Task StartDotnetDebugging(HttpContext context) {
        var state = await CheckDebugState();

        // 既に起動している場合は何もしない
        if (state.EstimatedPidOfAspNetCore != null) {
            state.ConsoleOut += "dotnet runは既に起動済みです。\n";
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(state, context.RequestAborted);
            return;
        }

        var errorSummary = new StringBuilder();
        var consoleOut = new StringBuilder();

        // 一時ファイル
        var workDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "temp"));
        var dotnetCmdFile = Path.Combine(workDir, "dotnet-run.cmd");
        var dotnetLogFile = Path.Combine(workDir, "dotnet-run.log");

        Directory.CreateDirectory(workDir);

        // dotnet runをcmdを介して実行するスクリプトを作成
        var dotnetRunDir = _project.WebapiProjectRoot;
        consoleOut.AppendLine($"dotnet runの作業ディレクトリ: {dotnetRunDir}");

        ProcessExtension.RenderCmdFile(dotnetCmdFile, $$"""
            chcp 65001
            @echo off
            setlocal

            set "LOG_FILE={{dotnetLogFile}}"

            @echo. > "%LOG_FILE%"
            echo [%date% %time%] dotnet run開始 > "%LOG_FILE%"
            cd /d "{{dotnetRunDir}}"
            call dotnet run --launch-profile https >> "%LOG_FILE%" 2>&1
            echo [%date% %time%] dotnet run終了（終了コード: %errorlevel%） >> "%LOG_FILE%"
            """);

        consoleOut.AppendLine($"dotnetプロセス開始用のcmdファイルを作成しました: {dotnetCmdFile}");

        // dotnet runをcmdファイルをUseShellExecute=trueで実行
        Process? dotnetRun;
        try {
            var startInfo = new ProcessStartInfo {
                FileName = Path.GetFullPath(dotnetCmdFile),
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = dotnetRunDir,
            };

            consoleOut.AppendLine($"dotnetプロセスを起動します: {dotnetCmdFile}");
            dotnetRun = Process.Start(startInfo);

            if (dotnetRun == null) {
                throw new InvalidOperationException($"[ERROR] dotnetプロセスの起動に失敗しました。Process.Startがnullを返しました。");
            }

            consoleOut.AppendLine($"dotnetプロセスを起動しました (PID: {dotnetRun.Id})");
        } catch (Exception ex) {
            var errorState = await CheckDebugState();
            errorState.ErrorSummary = $"dotnetプロセスの起動中に例外が発生しました: {ex.Message}";
            errorState.ConsoleOut += consoleOut.ToString();

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(errorState, context.RequestAborted);
            return;
        }

        // 開始されるまで一定時間待つ
        var timeout = DateTime.Now.AddSeconds(10);
        while (true) {
            var currentState = await CheckDebugState();
            if (currentState.EstimatedPidOfAspNetCore != null) {
                break;
            }
            if (DateTime.Now > timeout) {
                errorSummary.AppendLine("一定時間経過しましたがdotnetプロセスが起動しませんでした。");
                break;
            }
            await Task.Delay(500);
        }

        // ログファイルの内容を読み込んでエラー情報として追加
        try {
            if (File.Exists(dotnetLogFile)) {
                var logContent = await File.ReadAllTextAsync(dotnetLogFile);
                consoleOut.AppendLine($"=== dotnet run ログファイルの内容 ===");
                consoleOut.AppendLine(logContent);

                // ログにエラーらしき内容が含まれている場合はエラーサマリーに追加
                if (logContent.Contains("ERROR") || logContent.Contains("error") ||
                    logContent.Contains("fail") || logContent.Contains("Exception") ||
                    logContent.Contains("Unable to")) {
                    errorSummary.AppendLine("dotnet runの実行中にエラーが発生した可能性があります。詳細はログを確認してください。");
                }
            }
        } catch (Exception ex) {
            consoleOut.AppendLine($"ログファイルの読み込み中にエラーが発生しました: {ex.Message}");
        }

        // 起動し終わったのでpidを調べてクライアントに結果を返す
        var stateAfterLaunch = await CheckDebugState();
        stateAfterLaunch.ErrorSummary = errorSummary.ToString();
        stateAfterLaunch.ConsoleOut += consoleOut.ToString();
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(stateAfterLaunch, context.RequestAborted);
    }

    /// <summary>
    /// taskkillでnpmデバッグを止める
    /// </summary>
    private async Task StopNpmDebugging(HttpContext context) {
        var errorSummary = new StringBuilder();
        var consoleOut = new StringBuilder();
        var stateBeforeKill = await CheckDebugState();

        // Node.js のプロセスを止める
        if (stateBeforeKill.EstimatedPidOfNodeJs != null) {
            // 安全のためkill対象が node.exe であることを確認
            if (stateBeforeKill.NodeJsProcessName != "node.exe") {
                errorSummary.AppendLine($"[ERROR] デバッグ対象のプロセスが node.exe ではありません。kill対象: {stateBeforeKill.NodeJsProcessName}");

            } else {
                var exitCode = await ProcessExtension.ExecuteProcessAsync(startInfo => {
                    startInfo.FileName = "taskkill";
                    startInfo.ArgumentList.Add("/PID");
                    startInfo.ArgumentList.Add(stateBeforeKill.EstimatedPidOfNodeJs.Value.ToString()!);
                    startInfo.ArgumentList.Add("/T");
                    startInfo.ArgumentList.Add("/F");
                }, (std, line) => {
                    consoleOut.AppendLine($"[{std}] {line}");
                });
                consoleOut.AppendLine($"node.exeのtaskkillの終了コード: {exitCode}");
                if (exitCode != 0) {
                    errorSummary.AppendLine($"node.exeのtaskkillの終了コードが0ではありません。終了コード: {exitCode}");
                }
            }
        } else {
            consoleOut.AppendLine("npm デバッグプロセスが見つかりませんでした。");
        }

        var stateAfterKill = await CheckDebugState();
        stateAfterKill.ErrorSummary = errorSummary.ToString();
        stateAfterKill.ConsoleOut += consoleOut.ToString();

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(stateAfterKill, context.RequestAborted);
    }

    /// <summary>
    /// taskkillでdotnetデバッグを止める
    /// </summary>
    private async Task StopDotnetDebugging(HttpContext context) {
        var errorSummary = new StringBuilder();
        var consoleOut = new StringBuilder();
        var stateBeforeKill = await CheckDebugState();

        // ASP.NET Core のプロセスを止める
        if (stateBeforeKill.EstimatedPidOfAspNetCore != null) {
            // 念のため "～～.WebApi.exe" という名前のプロセスであることを確認
            if (stateBeforeKill.AspNetCoreProcessName?.EndsWith(".WebApi.exe") != true) {
                errorSummary.AppendLine($"[ERROR] デバッグ対象のプロセスが WebApi.exe ではありません。kill対象: {stateBeforeKill.AspNetCoreProcessName}");
            } else {

                var exitCode = await ProcessExtension.ExecuteProcessAsync(startInfo => {
                    startInfo.FileName = "taskkill";
                    startInfo.ArgumentList.Add("/PID");
                    startInfo.ArgumentList.Add(stateBeforeKill.EstimatedPidOfAspNetCore.Value.ToString()!);
                    startInfo.ArgumentList.Add("/T");
                    startInfo.ArgumentList.Add("/F");
                }, (std, line) => {
                    consoleOut.AppendLine($"[{std}] {line}");
                });
                consoleOut.AppendLine($"WebApi.exeのtaskkillの終了コード: {exitCode}");
                if (exitCode != 0) {
                    errorSummary.AppendLine($"WebApi.exeのtaskkillの終了コードが0ではありません。終了コード: {exitCode}");
                }
            }
        } else {
            consoleOut.AppendLine("dotnet デバッグプロセスが見つかりませんでした。");
        }

        var stateAfterKill = await CheckDebugState();
        stateAfterKill.ErrorSummary = errorSummary.ToString();
        stateAfterKill.ConsoleOut += consoleOut.ToString();

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(stateAfterKill, context.RequestAborted);
    }

    /// <summary>
    /// 現在実行中の NijoApplicationBuilder のデバッグ状態を、httpポート番号を基準に調べる。
    /// </summary>
    private static async Task<DebugProcessState> CheckDebugState() {
        var consoleOutputBuilder = new StringBuilder();

        // Helper action to log to consoleOutputBuilder
        Action<string> logToConsole = (message) => {
            consoleOutputBuilder.AppendLine(message);
        };

        // Helper action for ProcessExtension.ExecuteProcessAsync
        // This will append process output to consoleOutputBuilder and also to a separate list if needed.
        Func<string, List<string>, Action<ProcessExtension.E_STD, string>> createLogHandler =
            (processName, outputList) => {
                return (std, line) => {
                    var formattedLine = $"[{processName}-{std}] {line}";
                    consoleOutputBuilder.AppendLine(formattedLine);
                    if (std == ProcessExtension.E_STD.StdOut && line != null) { // line can be null if stream closed
                        outputList.Add(line);
                    }
                };
            };

        int? nodePid = null;
        int? aspPid = null;
        string? nodeProcessName = null;
        string? aspProcessName = null;

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
                var tasklistNodeOutput = new List<string>();
                await ProcessExtension.ExecuteProcessAsync(
                    psi => {
                        psi.FileName = "tasklist";
                        psi.ArgumentList.Add("/fi");
                        psi.ArgumentList.Add($"pid eq {nodePid.Value}");
                        psi.ArgumentList.Add("/nh");
                        psi.ArgumentList.Add("/fo");
                        psi.ArgumentList.Add("csv");
                    },
                    createLogHandler($"tasklist-node({nodePid.Value})", tasklistNodeOutput),
                    TimeSpan.FromSeconds(10)
                );
                if (tasklistNodeOutput.Count > 0 && tasklistNodeOutput[0] != null) {
                    var parts = tasklistNodeOutput[0].Split(',');
                    if (parts.Length > 0) {
                        nodeProcessName = parts[0].Trim('"');
                        logToConsole($"Node.js process name: {nodeProcessName}");
                    }
                }
            } else {
                logToConsole($"Node.js PID (port {NPM_PORT}) not found or process not listening. Skipping tasklist.");
            }

            // tasklist を使って ASP.NET Core のpidの詳細情報を得る
            if (aspPid.HasValue) {
                logToConsole($"Executing: tasklist /fi \"pid eq {aspPid.Value}\" /nh /fo csv");
                var tasklistAspOutput = new List<string>();
                await ProcessExtension.ExecuteProcessAsync(
                    psi => {
                        psi.FileName = "tasklist";
                        psi.ArgumentList.Add("/fi");
                        psi.ArgumentList.Add($"pid eq {aspPid.Value}");
                        psi.ArgumentList.Add("/nh");
                        psi.ArgumentList.Add("/fo");
                        psi.ArgumentList.Add("csv");
                    },
                    createLogHandler($"tasklist-asp({aspPid.Value})", tasklistAspOutput),
                    TimeSpan.FromSeconds(10)
                );
                if (tasklistAspOutput.Count > 0 && tasklistAspOutput[0] != null) {
                    var parts = tasklistAspOutput[0].Split(',');
                    if (parts.Length > 0) {
                        aspProcessName = parts[0].Trim('"');
                        logToConsole($"ASP.NET Core process name: {aspProcessName}");
                    }
                }
            } else {
                logToConsole($"ASP.NET Core PID (port {DOTNET_PORT}) not found or process not listening. Skipping tasklist.");
            }

        } catch (TimeoutException tex) {
            logToConsole($"A process execution timed out: {tex.Message}");
        } catch (Exception ex) {
            logToConsole($"An error occurred while gathering debug process info: {ex.ToString()}");
        }

        return new DebugProcessState {
            EstimatedPidOfNodeJs = nodePid,
            EstimatedPidOfAspNetCore = aspPid,
            NodeJsProcessName = nodeProcessName,
            AspNetCoreProcessName = aspProcessName,
            NodeJsDebugUrl = $"http://localhost:{NPM_PORT}",
            AspNetCoreDebugUrl = $"https://localhost:{DOTNET_PORT}/swagger",
            ConsoleOut = consoleOutputBuilder.ToString(),
        };
    }
}

/// <summary>
/// 現在実行中のNijoApplicationBuilderのデバッグプロセスの状態。
/// このクラスのデータ構造はTypeScript側と合わせる必要あり
/// </summary>
public class DebugProcessState {
    /// <summary>
    /// サーバー側で発生した何らかのエラー
    /// </summary>
    [JsonPropertyName("errorSummary")]
    public string? ErrorSummary { get; set; }
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
    /// Node.jsのプロセス名
    /// </summary>
    [JsonPropertyName("nodeJsProcessName")]
    public string? NodeJsProcessName { get; set; }
    /// <summary>
    /// ASP.NET Coreのプロセス名
    /// </summary>
    [JsonPropertyName("aspNetCoreProcessName")]
    public string? AspNetCoreProcessName { get; set; }
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
