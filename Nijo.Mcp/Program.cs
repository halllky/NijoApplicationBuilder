using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;


var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions => {
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
var host = builder.Build();
await host.RunAsync();


namespace Nijo.Mcp {
    /// <summary>
    /// AIエージェントがNijoApplicationBuilderを用いたアプリケーションのデバッグや開発を遂行しやすくするための補助ツール。
    /// Model Context Protocol を使用している。
    ///
    /// エラーハンドリング: 処理中にで例外が送出された場合、戻り値のstringにその詳細含めすべて返す。
    /// ロギング: 行なわない。
    /// </summary>
    [McpServerToolType]
    public static class NijoMcpTools {

        private const string JOB_NAME = "mijo-mcp-debug-task";

        [McpServerTool(Name = "start_debugging"), Description("ソースコード自動生成された方のアプリケーションのデバッグを開始する。既に開始されている場合はリビルドして再開する。")]
        public static string StartDebugging([Description("nijo.xmlのファイルの絶対パス")] string nijoXmlFileFullPath) {
            try {

                if (string.IsNullOrEmpty(nijoXmlFileFullPath)) {
                    return "nijo.xmlのファイルの絶対パスを指定してください。";
                }

                var nijoXmlDir = Path.GetDirectoryName(nijoXmlFileFullPath);
                var reactDir = Path.Combine(nijoXmlDir!, "react");
                var webApiDir = Path.Combine(nijoXmlDir!, "WebApi");

                if (!File.Exists(nijoXmlFileFullPath)) {
                    return $"指定されたnijo.xmlファイルが見つかりません: {nijoXmlFileFullPath}";
                }
                if (!Directory.Exists(reactDir)) {
                    return $"reactフォルダが見つかりません: {reactDir}";
                }
                if (!Directory.Exists(webApiDir)) {
                    return $"WebApiフォルダが見つかりません: {webApiDir}";
                }

                // 既存のプロセスを終了
                JobObjectHelper.TryKillJobByName(JOB_NAME);

                var output = new StringBuilder();
                var errorDetected = false;
                var errorDetectionTime = DateTime.MinValue;

                // npm run dev プロセスを開始
                var npmProcess = new Process();
                npmProcess.StartInfo.WorkingDirectory = reactDir;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    npmProcess.StartInfo.FileName = "powershell";
                    npmProcess.StartInfo.Arguments = "/c \"npm run dev\"";
                } else {
                    npmProcess.StartInfo.FileName = "npm";
                    npmProcess.StartInfo.Arguments = "run dev";
                }
                npmProcess.StartInfo.RedirectStandardOutput = true;
                npmProcess.StartInfo.RedirectStandardError = true;
                npmProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                npmProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                npmProcess.OutputDataReceived += (sender, e) => {
                    if (e.Data != null) {
                        output.AppendLine($"[npm] {e.Data}");
                    }
                };
                npmProcess.ErrorDataReceived += (sender, e) => {
                    if (e.Data != null) {
                        output.AppendLine($"[npm error] {e.Data}");
                        if (!errorDetected) {
                            errorDetected = true;
                            errorDetectionTime = DateTime.Now;
                        }
                    }
                };

                npmProcess.Start();
                npmProcess.BeginOutputReadLine();
                npmProcess.BeginErrorReadLine();

                // dotnet run プロセスを開始
                var dotnetProcess = new Process();
                dotnetProcess.StartInfo.WorkingDirectory = webApiDir;
                dotnetProcess.StartInfo.FileName = "dotnet";
                dotnetProcess.StartInfo.Arguments = "run --launch-profile https";
                dotnetProcess.StartInfo.RedirectStandardOutput = true;
                dotnetProcess.StartInfo.RedirectStandardError = true;
                dotnetProcess.StartInfo.StandardOutputEncoding = Console.OutputEncoding;
                dotnetProcess.StartInfo.StandardErrorEncoding = Console.OutputEncoding;

                dotnetProcess.OutputDataReceived += (sender, e) => {
                    if (e.Data != null) {
                        output.AppendLine($"[dotnet] {e.Data}");
                    }
                };
                dotnetProcess.ErrorDataReceived += (sender, e) => {
                    if (e.Data != null) {
                        output.AppendLine($"[dotnet error] {e.Data}");
                        if (!errorDetected) {
                            errorDetected = true;
                            errorDetectionTime = DateTime.Now;
                        }
                    }
                };

                dotnetProcess.Start();
                dotnetProcess.BeginOutputReadLine();
                dotnetProcess.BeginErrorReadLine();

                // プロセスをジョブに割り当て
                var jobHandle = JobObjectHelper.CreateAndAssignJob(JOB_NAME, npmProcess);
                JobObjectHelper.CreateAndAssignJob(JOB_NAME, dotnetProcess);

                // プロセスの準備完了を待機
                var ready = false;
                var timeout = DateTime.Now.AddMinutes(3);
                while (!ready && DateTime.Now < timeout) {
                    // エラーが検出されてから5秒経過したら中断
                    if (errorDetected && DateTime.Now > errorDetectionTime.AddSeconds(5)) {
                        JobObjectHelper.TryKillJobByName(JOB_NAME);
                        return $"エラーが検出されたため、デバッグを中断しました。\n{output}";
                    }

                    // Viteでは標準出力にこの文字が現れたら準備完了
                    if (output.ToString().Contains("➜") && output.ToString().Contains("Now listening on:")) {
                        ready = true;
                    }
                    Thread.Sleep(100);
                }

                if (!ready) {
                    JobObjectHelper.TryKillJobByName(JOB_NAME);
                    return $"タイムアウト: プロセスの準備が完了しませんでした。\n{output}";
                }

                return $"デバッグを開始しました。\n{output}";

            } catch (Exception ex) {
                return $"エラーが発生しました: {ex}";
            }
        }

        [McpServerTool(Name = "stop_debugging"), Description("ソースコード自動生成された方のアプリケーションのデバッグを終了する。")]
        public static string StopDebugging() {
            try {
                var result = JobObjectHelper.TryKillJobByName(JOB_NAME);
                return result
                    ? "デバッグを終了しました。"
                    : "デバッグプロセスが見つかりませんでした。";

            } catch (Exception ex) {
                return $"エラーが発生しました: {ex}";
            }
        }


        // *************** 以下、McpServerTool の実装例 ******************

        //[McpServerTool, Description("Echoes the message back to the client.")]
        //public static string Echo(string message) => $"hello {message}";

        //[McpServerTool(Name = "SummarizeContentFromUrl"), Description("Summarizes content downloaded from a specific URI")]
        //public static async Task<string> SummarizeDownloadedContent(
        //IMcpServer thisServer,
        //HttpClient httpClient,
        //[Description("The url from which to download the content to summarize")] string url,
        //CancellationToken cancellationToken) {
        //    string content = await httpClient.GetStringAsync(url);

        //    ChatMessage[] messages =
        //    [
        //        new(ChatRole.User, "Briefly summarize the following downloaded content:"),
        //        new(ChatRole.User, content),
        //    ];

        //    ChatOptions options = new() {
        //        MaxOutputTokens = 256,
        //        Temperature = 0.3f,
        //    };

        //    return $"Summary: {await thisServer.AsSamplingChatClient().GetResponseAsync(messages, options, cancellationToken)}";
        //}

        //[McpServerPromptType]
        //public static class MyPrompts {
        //    [McpServerPrompt, Description("Creates a prompt to summarize the provided message.")]
        //    public static ChatMessage Summarize([Description("The content to summarize")] string content) =>
        //        new(ChatRole.User, $"Please summarize this content into a single sentence: {content}");
        //}
    }
}
