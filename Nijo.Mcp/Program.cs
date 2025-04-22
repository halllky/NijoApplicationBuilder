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
using System.Net.Http;

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

        private const string NIJO_PROJ = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo"; // とりあえずハードコード
        private const string JOB_NAME = "mijo-mcp-debug-task";
        private const string DOTNET_URL = "https://localhost:7098/swagger";
        private const string NPM_URL = "http://localhost:5173";
        private const string WORK_DIR = "temp_work";
        private const string NPM_LOG_FILE = "npm_output.log";
        private const string DOTNET_LOG_FILE = "dotnet_output.log";

        // ログフォルダを準備する
        private static void PrepareLogDirectory() {
            // カレントディレクトリにlogsフォルダがなければ作成
            if (!Directory.Exists(WORK_DIR)) {
                Directory.CreateDirectory(WORK_DIR);
            }

            // Git管理対象外
            File.WriteAllText(Path.Combine(WORK_DIR, ".gitignore"), "*");

            // 既存のログファイルがあれば削除
            string npmLogPath = Path.Combine(WORK_DIR, NPM_LOG_FILE);
            string dotnetLogPath = Path.Combine(WORK_DIR, DOTNET_LOG_FILE);

            if (File.Exists(npmLogPath)) {
                File.Delete(npmLogPath);
            }

            if (File.Exists(dotnetLogPath)) {
                File.Delete(dotnetLogPath);
            }
        }

        // ログファイルの内容を読み取る
        private static string ReadLogFile(string fileName) {
            string logPath = Path.Combine(WORK_DIR, fileName);
            if (!File.Exists(logPath)) {
                return "ログファイルが見つかりません。";
            }

            try {
                return File.ReadAllText(logPath);
            } catch (Exception ex) {
                return $"ログファイルの読み取りに失敗しました: {ex.Message}";
            }
        }

        [McpServerTool(Name = "start_debugging"), Description("ソースコード自動生成された方のアプリケーションのデバッグを開始する。既に開始されている場合はリビルドして再開する。")]
        public static async Task<string> StartDebugging([Description("nijo.xmlのファイルの絶対パス")] string nijoXmlFileFullPath) {
            try {
                // npm run devの出力が化けるので強制的にutf8指定
                Console.InputEncoding = Encoding.UTF8;
                Console.OutputEncoding = Encoding.UTF8;

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

                // ログディレクトリを準備
                PrepareLogDirectory();

                // 既存のプロセスを終了 - これによって以前実行していたプロセスが終了する
                JobObjectHelper.TryKillJobByName(JOB_NAME);

                // ------------------------------------
                var mainOutput = new StringBuilder();
                var npmOutput = new StringBuilder();
                var dotnetOutput = new StringBuilder();
                var errorDetected = false;
                var errorDetectionTime = DateTime.MinValue;

                // ソースコードの自動生成のかけなおし
                mainOutput.AppendLine("[nijo-mcp] Nijoプロジェクトのビルドを開始します...");

                // Nijoプロジェクトのビルド
                var buildProcess = new Process();
                buildProcess.StartInfo.FileName = "dotnet";
                buildProcess.StartInfo.Arguments = $"build {NIJO_PROJ} -c Debug";
                buildProcess.StartInfo.RedirectStandardOutput = true;
                buildProcess.StartInfo.RedirectStandardError = true;
                buildProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                buildProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                var buildOutput = new StringBuilder();
                buildProcess.OutputDataReceived += (sender, e) => {
                    if (e.Data != null) {
                        buildOutput.AppendLine(e.Data);
                    }
                };
                buildProcess.ErrorDataReceived += (sender, e) => {
                    if (e.Data != null) {
                        buildOutput.AppendLine(e.Data);
                        if (!errorDetected) {
                            errorDetected = true;
                            errorDetectionTime = DateTime.Now;
                        }
                    }
                };

                buildProcess.Start();
                buildProcess.BeginOutputReadLine();
                buildProcess.BeginErrorReadLine();
                buildProcess.WaitForExit();

                mainOutput.AppendLine($"[nijo-mcp] Nijoプロジェクトのビルド結果:\n{buildOutput}");

                // ビルドが成功した場合のみ、ソースコード自動生成を実行
                if (buildProcess.ExitCode == 0) {
                    mainOutput.AppendLine("[nijo-mcp] ソースコード自動生成を開始します...");

                    // ソースコード自動生成
                    var generateProcess = new Process();
                    generateProcess.StartInfo.FileName = "nijo";
                    generateProcess.StartInfo.Arguments = $"generate {nijoXmlDir}";
                    generateProcess.StartInfo.RedirectStandardOutput = true;
                    generateProcess.StartInfo.RedirectStandardError = true;
                    generateProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    generateProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                    var generateOutput = new StringBuilder();
                    generateProcess.OutputDataReceived += (sender, e) => {
                        if (e.Data != null) {
                            generateOutput.AppendLine(e.Data);
                        }
                    };
                    generateProcess.ErrorDataReceived += (sender, e) => {
                        if (e.Data != null) {
                            generateOutput.AppendLine(e.Data);
                            if (!errorDetected) {
                                errorDetected = true;
                                errorDetectionTime = DateTime.Now;
                            }
                        }
                    };

                    generateProcess.Start();
                    generateProcess.BeginOutputReadLine();
                    generateProcess.BeginErrorReadLine();
                    generateProcess.WaitForExit();

                    mainOutput.AppendLine($"[nijo-mcp] ソースコード自動生成結果:\n{generateOutput}");

                    if (generateProcess.ExitCode != 0) {
                        errorDetected = true;
                        errorDetectionTime = DateTime.Now;
                    }
                } else {
                    errorDetected = true;
                    errorDetectionTime = DateTime.Now;
                }

                // npmログファイルのフルパス
                string npmLogPath = Path.Combine(Directory.GetCurrentDirectory(), WORK_DIR, NPM_LOG_FILE);
                // dotnetログファイルのフルパス
                string dotnetLogPath = Path.Combine(Directory.GetCurrentDirectory(), WORK_DIR, DOTNET_LOG_FILE);

                // npm run dev プロセスを開始
                mainOutput.AppendLine($"[nijo-mcp] npmプロセスを開始します: npm run dev (出力は {npmLogPath} に保存されます)");
                try {
                    // バッチファイルを作成して実行する（Windows）
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                        string npmBatchPath = Path.Combine(Directory.GetCurrentDirectory(), WORK_DIR, "run_npm.bat");
                        File.WriteAllText(npmBatchPath, $$"""
                            @echo off 
                            chcp 65001 
                             
                            cd /d "{{reactDir}}" 
                            npm run dev > "{{npmLogPath}}" 2>&1 
                            """);

                        var npmProcess = new Process();
                        npmProcess.StartInfo.FileName = "cmd.exe";
                        npmProcess.StartInfo.Arguments = $"/c start /min cmd /c {npmBatchPath}";
                        npmProcess.StartInfo.UseShellExecute = true;
                        npmProcess.StartInfo.CreateNoWindow = true;
                        npmProcess.Start();
                        mainOutput.AppendLine($"[nijo-mcp] npmプロセスを開始しました");
                    } else {
                        // Linuxの場合
                        var npmProcess = new Process();
                        npmProcess.StartInfo.FileName = "bash";
                        npmProcess.StartInfo.Arguments = $"-c \"cd '{reactDir}' && npm run dev > '{npmLogPath}' 2>&1 &\"";
                        npmProcess.StartInfo.UseShellExecute = true;
                        npmProcess.StartInfo.CreateNoWindow = true;
                        npmProcess.Start();
                        mainOutput.AppendLine($"[nijo-mcp] npmプロセスを開始しました");
                    }
                } catch (Exception ex) {
                    mainOutput.AppendLine($"[nijo-mcp] npmプロセスの起動に失敗しました: {ex.Message}");
                    errorDetected = true;
                    errorDetectionTime = DateTime.Now;
                }

                // dotnet run プロセスを開始
                mainOutput.AppendLine($"[nijo-mcp] dotnetプロセスを開始します: dotnet run --launch-profile https (出力は {dotnetLogPath} に保存されます)");
                try {
                    // バッチファイルを作成して実行する（Windows）
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                        string dotnetBatchPath = Path.Combine(Directory.GetCurrentDirectory(), WORK_DIR, "run_dotnet.bat");
                        File.WriteAllText(dotnetBatchPath, $$"""
                            @echo off 
                            chcp 65001 
                             
                            cd /d "{{webApiDir}}" 
                            dotnet run --launch-profile https > "{{dotnetLogPath}}" 2>&1 
                            """);

                        var dotnetProcess = new Process();
                        dotnetProcess.StartInfo.FileName = "cmd.exe";
                        dotnetProcess.StartInfo.Arguments = $"/c start /min cmd /c {dotnetBatchPath}";
                        dotnetProcess.StartInfo.UseShellExecute = true;
                        dotnetProcess.StartInfo.CreateNoWindow = true;
                        dotnetProcess.Start();
                        mainOutput.AppendLine($"[nijo-mcp] dotnetプロセスを開始しました");
                    } else {
                        // Linuxの場合
                        var dotnetProcess = new Process();
                        dotnetProcess.StartInfo.FileName = "bash";
                        dotnetProcess.StartInfo.Arguments = $"-c \"cd '{webApiDir}' && dotnet run --launch-profile https > '{dotnetLogPath}' 2>&1 &\"";
                        dotnetProcess.StartInfo.UseShellExecute = true;
                        dotnetProcess.StartInfo.CreateNoWindow = true;
                        dotnetProcess.Start();
                        mainOutput.AppendLine($"[nijo-mcp] dotnetプロセスを開始しました");
                    }
                } catch (Exception ex) {
                    mainOutput.AppendLine($"[nijo-mcp] dotnetプロセスの起動に失敗しました: {ex.Message}");
                    errorDetected = true;
                    errorDetectionTime = DateTime.Now;
                }

                // 少し待ってからアクセス確認を開始
                await Task.Delay(5000);

                // サービスの準備完了を待機
                var ready = false;
                var timeout = DateTime.Now.AddMinutes(1);
                var dotnetReady = false;
                var npmReady = false;

                // Webのポートにリクエストを投げて結果が返ってくるかどうかで確認する
                using var _httpClient = new HttpClient() {
                    Timeout = TimeSpan.FromSeconds(1)
                };
                async Task<bool> IsServiceReadyAsync(string url) {
                    try {
                        var response = await _httpClient.GetAsync(url);
                        return response.IsSuccessStatusCode;
                    } catch {
                        return false;
                    }
                }

                // 待機中に一定間隔でログファイルを確認
                var lastNpmLogCheck = DateTime.MinValue;
                var lastDotnetLogCheck = DateTime.MinValue;
                var lastNpmLogSize = 0L;
                var lastDotnetLogSize = 0L;

                while (!ready && DateTime.Now < timeout) {
                    // ログファイルを定期的にチェック（3秒ごと）
                    if ((DateTime.Now - lastNpmLogCheck).TotalSeconds >= 3) {
                        try {
                            if (File.Exists(npmLogPath)) {
                                FileInfo npmLogInfo = new FileInfo(npmLogPath);
                                if (npmLogInfo.Length > lastNpmLogSize) {
                                    string newContent = "";
                                    using (FileStream fs = new FileStream(npmLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                    using (StreamReader sr = new StreamReader(fs, Encoding.UTF8)) {
                                        if (lastNpmLogSize > 0) {
                                            sr.BaseStream.Seek(lastNpmLogSize, SeekOrigin.Begin);
                                        }
                                        newContent = sr.ReadToEnd();
                                    }
                                    lastNpmLogSize = npmLogInfo.Length;
                                    mainOutput.AppendLine($"[npm-log] {newContent.TrimEnd()}");

                                    // エラーチェック
                                    if (newContent.Contains("ERROR") || newContent.Contains("Error:")) {
                                        errorDetected = true;
                                        errorDetectionTime = DateTime.Now;
                                    }
                                }
                            }
                            lastNpmLogCheck = DateTime.Now;
                        } catch (Exception ex) {
                            mainOutput.AppendLine($"[nijo-mcp] npmログファイルの読み取りに失敗しました: {ex.Message}");
                        }
                    }

                    if ((DateTime.Now - lastDotnetLogCheck).TotalSeconds >= 3) {
                        try {
                            if (File.Exists(dotnetLogPath)) {
                                FileInfo dotnetLogInfo = new FileInfo(dotnetLogPath);
                                if (dotnetLogInfo.Length > lastDotnetLogSize) {
                                    string newContent = "";
                                    using (FileStream fs = new FileStream(dotnetLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                    using (StreamReader sr = new StreamReader(fs, Encoding.UTF8)) {
                                        if (lastDotnetLogSize > 0) {
                                            sr.BaseStream.Seek(lastDotnetLogSize, SeekOrigin.Begin);
                                        }
                                        newContent = sr.ReadToEnd();
                                    }
                                    lastDotnetLogSize = dotnetLogInfo.Length;
                                    mainOutput.AppendLine($"[dotnet-log] {newContent.TrimEnd()}");

                                    // エラーチェック
                                    if (newContent.Contains("ERROR") || newContent.Contains("Error:") || newContent.Contains("Exception:")) {
                                        errorDetected = true;
                                        errorDetectionTime = DateTime.Now;
                                    }
                                }
                            }
                            lastDotnetLogCheck = DateTime.Now;
                        } catch (Exception ex) {
                            mainOutput.AppendLine($"[nijo-mcp] dotnetログファイルの読み取りに失敗しました: {ex.Message}");
                        }
                    }

                    // エラーが検出されてから5秒経過したら中断
                    if (errorDetected && DateTime.Now > errorDetectionTime.AddSeconds(5)) {
                        return $"エラーが検出されたため、デバッグを中断しました。\n" +
                               $"メイン処理の出力:\n{mainOutput}\n" +
                               $"NPMログファイル: {npmLogPath}\n" +
                               $"WebAPIログファイル: {dotnetLogPath}";
                    }

                    if (!dotnetReady) {
                        dotnetReady = await IsServiceReadyAsync(DOTNET_URL);
                        if (dotnetReady) {
                            mainOutput.AppendLine("[nijo-mcp] WebAPIの準備が完了しました。");
                        }
                    }

                    if (!npmReady) {
                        npmReady = await IsServiceReadyAsync(NPM_URL);
                        if (npmReady) {
                            mainOutput.AppendLine("[nijo-mcp] フロントエンドの準備が完了しました。");
                        }
                    }

                    ready = dotnetReady && npmReady;

                    if (!ready) {
                        await Task.Delay(500); // 0.5秒待機
                    }
                }

                if (!ready) {
                    var status = new StringBuilder("タイムアウト: ");
                    if (!dotnetReady) status.Append("WebAPIの準備が完了しませんでした。");
                    if (!npmReady) status.Append("フロントエンドの準備が完了しませんでした。");
                    return $"{status}\n" +
                           $"メイン処理の出力:\n{mainOutput}\n" +
                           $"NPMログファイル: {npmLogPath}\n" +
                           $"WebAPIログファイル: {dotnetLogPath}";
                }

                return $"デバッグを開始しました。以下のURLにアクセスしてください。\n" +
                       $"{NPM_URL}\n" +
                       $"メイン処理の出力:\n{mainOutput}\n" +
                       $"ログファイルは以下の場所に保存されています：\n" +
                       $"NPMログ: {npmLogPath}\n" +
                       $"WebAPIログ: {dotnetLogPath}";

            } catch (Exception ex) {
                return $"エラーが発生しました: {ex}";
            }
        }

        [McpServerTool(Name = "stop_debugging"), Description("ソースコード自動生成された方のアプリケーションのデバッグを終了する。")]
        public static string StopDebugging() {
            try {
                // npm run devの出力が化けるので強制的にutf8指定
                Console.InputEncoding = Encoding.UTF8;
                Console.OutputEncoding = Encoding.UTF8;

                var result = JobObjectHelper.TryKillJobByName(JOB_NAME);

                // Windows標準の方法でプロセスを探して終了する
                try {
                    // npm関連のプロセスを終了
                    Process[] npmProcesses = Process.GetProcessesByName("node");
                    foreach (var proc in npmProcesses) {
                        try {
                            proc.Kill();
                        } catch { }
                    }

                    // dotnet関連のプロセスを終了
                    Process[] dotnetProcesses = Process.GetProcessesByName("dotnet");
                    foreach (var proc in dotnetProcesses) {
                        try {
                            // プロセス情報からWebApiのホスト判定する
                            if (proc.MainWindowTitle.Contains("WebApi") ||
                                (proc.MainModule != null && proc.MainModule.FileName.Contains("WebApi"))) {
                                proc.Kill();
                            }
                        } catch { }
                    }

                    // cmdプロセスも終了（バッチファイル実行の場合）
                    Process[] cmdProcesses = Process.GetProcessesByName("cmd");
                    foreach (var proc in cmdProcesses) {
                        try {
                            // タイトルに "run_npm.bat" や "run_dotnet.bat" が含まれていたら終了
                            if (proc.MainWindowTitle.Contains("run_npm.bat") || proc.MainWindowTitle.Contains("run_dotnet.bat")) {
                                proc.Kill();
                            }
                        } catch { }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"プロセス終了時のエラー: {ex.Message}");
                }

                // バッチファイルを削除
                try {
                    string npmBatchPath = Path.Combine(Directory.GetCurrentDirectory(), WORK_DIR, "run_npm.bat");
                    string dotnetBatchPath = Path.Combine(Directory.GetCurrentDirectory(), WORK_DIR, "run_dotnet.bat");

                    if (File.Exists(npmBatchPath)) {
                        File.Delete(npmBatchPath);
                    }

                    if (File.Exists(dotnetBatchPath)) {
                        File.Delete(dotnetBatchPath);
                    }
                } catch { }

                return result
                    ? "デバッグを終了しました。"
                    : "デバッグプロセスが見つかりませんでした。";

            } catch (Exception ex) {
                return $"エラーが発生しました: {ex}";
            }
        }

        [McpServerTool(Name = "get_npm_log"), Description("npmプロセスのログを取得する。")]
        public static string GetNpmLog() {
            return ReadLogFile(NPM_LOG_FILE);
        }

        [McpServerTool(Name = "get_dotnet_log"), Description("dotnetプロセスのログを取得する。")]
        public static string GetDotnetLog() {
            return ReadLogFile(DOTNET_LOG_FILE);
        }
    }
}
