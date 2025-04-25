using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using System.Reflection;

//Console.WriteLine(await Nijo.Mcp.NijoMcpTools.StartDebugging(@"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.ApplicationTemplate.Ver1\nijo.xml"));
//Console.WriteLine(Nijo.Mcp.NijoMcpTools.StopDebugging());

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
        private const string DOTNET_URL = "https://localhost:7098/swagger";
        private const string NPM_URL = "http://localhost:5173";
        private const string WORK_DIR = "temp_work";
        private const string MAIN_LOG_FILE = "output.log";
        private const string NPM_LOG_FILE = "output_npm.log";
        private const string DOTNET_LOG_FILE = "output_dotnet.log";

        [McpServerTool(Name = "start_debugging"), Description("ソースコード自動生成された方のアプリケーションのデバッグを開始する。既に開始されている場合はリビルドして再開する。")]
        public static async Task<string> StartDebugging([Description("nijo.xmlのファイルの絶対パス")] string nijoXmlFileFullPath) {
            // ログディレクトリを準備
            PrepareLogDirectory();

            // メインログ
            var workDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location!)!, WORK_DIR);
            var mainOutput = new StringBuilder();
            string mainLogPath = Path.Combine(workDirectory, MAIN_LOG_FILE);
            using var mainLogFs = new FileStream(mainLogPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var sw = new StreamWriter(mainLogFs, encoding: Encoding.UTF8);
            sw.AutoFlush = true;
            void WriteToMainLog(string text) {
                mainOutput.AppendLine(text);
                sw.WriteLine(text);
            }

            try {

                if (string.IsNullOrEmpty(nijoXmlFileFullPath)) {
                    return ToMcpResutJson(new { Error = "nijo.xmlのファイルの絶対パスを指定してください。" });
                }

                var nijoXmlDir = Path.GetDirectoryName(nijoXmlFileFullPath);
                var reactDir = Path.Combine(nijoXmlDir!, "react");
                var webApiDir = Path.Combine(nijoXmlDir!, "WebApi");

                if (!File.Exists(nijoXmlFileFullPath)) {
                    return ToMcpResutJson(new { Error = $"指定されたnijo.xmlファイルが見つかりません: {nijoXmlFileFullPath}" });
                }
                if (!Directory.Exists(reactDir)) {
                    return ToMcpResutJson(new { Error = $"reactフォルダが見つかりません: {reactDir}" });
                }
                if (!Directory.Exists(webApiDir)) {
                    return ToMcpResutJson(new { Error = $"WebApiフォルダが見つかりません: {webApiDir}" });
                }

                // 既存のプロセスを終了 - これによって以前実行していたプロセスが終了する
                WriteToMainLog(StopDebugging());

                // ------------------------------------
                var errorDetected = false;
                var errorDetectionTime = DateTime.MinValue;

                // ソースコードの自動生成のかけなおし
                WriteToMainLog("[nijo-mcp] Nijoプロジェクトのビルドを開始します...");

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

                WriteToMainLog($"[nijo-mcp] Nijoプロジェクトのビルド結果:\n{buildOutput}");

                // ビルドが成功した場合のみ、ソースコード自動生成を実行
                if (buildProcess.ExitCode == 0) {
                    WriteToMainLog("[nijo-mcp] ソースコード自動生成を開始します...");

                    // ソースコード自動生成
                    var generateProcess = new Process();
                    generateProcess.StartInfo.FileName = "nijo";
                    generateProcess.StartInfo.Arguments = $"generate {nijoXmlDir}";
                    generateProcess.StartInfo.RedirectStandardOutput = true;
                    generateProcess.StartInfo.RedirectStandardError = true;
                    generateProcess.StartInfo.StandardOutputEncoding = Console.OutputEncoding;
                    generateProcess.StartInfo.StandardErrorEncoding = Console.OutputEncoding;

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

                    WriteToMainLog($"[nijo-mcp] ソースコード自動生成結果:\n{generateOutput}");

                    if (generateProcess.ExitCode != 0) {
                        errorDetected = true;
                        errorDetectionTime = DateTime.Now;
                    }
                } else {
                    errorDetected = true;
                    errorDetectionTime = DateTime.Now;
                }

                // npmログファイル
                string npmLogPath = Path.Combine(workDirectory, NPM_LOG_FILE);
                using var npmLogFileStream = new FileStream(npmLogPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var npmLogWriter = new StreamWriter(npmLogFileStream, Encoding.UTF8) { AutoFlush = true };

                // dotnetログファイル
                string dotnetLogPath = Path.Combine(workDirectory, DOTNET_LOG_FILE);
                using var dotnetLogFileStream = new FileStream(dotnetLogPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var dotnetLogWriter = new StreamWriter(dotnetLogFileStream, Encoding.UTF8) { AutoFlush = true };

                // npm run dev プロセスを開始
                WriteToMainLog($"[nijo-mcp] npmプロセスを開始します: npm run dev (出力は {npmLogPath} に保存されます)");
                Process? npmProcess = null;
                try {
                    npmProcess = new Process();
                    npmProcess.StartInfo.FileName = "cmd";
                    npmProcess.StartInfo.Arguments = "/c \"npm.cmd run dev\"";
                    npmProcess.StartInfo.WorkingDirectory = reactDir;
                    npmProcess.StartInfo.UseShellExecute = false;
                    npmProcess.StartInfo.RedirectStandardOutput = true;
                    npmProcess.StartInfo.RedirectStandardError = true;
                    npmProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    npmProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                    npmProcess.StartInfo.CreateNoWindow = true;
                    npmProcess.StartInfo.EnvironmentVariables["NO_COLOR"] = "true"; // ログに色変更指示のANSIエスケープシーケンスが出力されるのを防ぐ

                    // ログファイルへの出力
                    npmProcess.OutputDataReceived += (sender, e) => {
                        if (e.Data != null) {
                            npmLogWriter.WriteLine(e.Data);
                        }
                    };
                    npmProcess.ErrorDataReceived += (sender, e) => {
                        if (e.Data != null) {
                            npmLogWriter.WriteLine("ERROR: " + e.Data);
                            if (e.Data.Contains("ERROR") || e.Data.Contains("Error:")) {
                                errorDetected = true;
                                errorDetectionTime = DateTime.Now;
                            }
                        }
                    };

                    npmProcess.Start();
                    npmProcess.BeginOutputReadLine();
                    npmProcess.BeginErrorReadLine();

                    WriteToMainLog($"[nijo-mcp] npmプロセスを開始しました");
                } catch (Exception ex) {
                    WriteToMainLog($"[nijo-mcp] npmプロセスの起動に失敗しました: {ex.Message}");
                    errorDetected = true;
                    errorDetectionTime = DateTime.Now;
                }

                // dotnet run プロセスを開始
                WriteToMainLog($"[nijo-mcp] dotnetプロセスを開始します: dotnet run --launch-profile https (出力は {dotnetLogPath} に保存されます)");
                Process? dotnetProcess = null;
                try {
                    dotnetProcess = new Process();
                    dotnetProcess.StartInfo.FileName = "dotnet";
                    dotnetProcess.StartInfo.Arguments = "run --launch-profile https";
                    dotnetProcess.StartInfo.WorkingDirectory = webApiDir;
                    dotnetProcess.StartInfo.UseShellExecute = false;
                    dotnetProcess.StartInfo.RedirectStandardOutput = true;
                    dotnetProcess.StartInfo.RedirectStandardError = true;
                    dotnetProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    dotnetProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                    dotnetProcess.StartInfo.CreateNoWindow = true;

                    // ログファイルへの出力
                    dotnetProcess.OutputDataReceived += (sender, e) => {
                        if (e.Data != null) {
                            dotnetLogWriter.WriteLine(e.Data);
                        }
                    };
                    dotnetProcess.ErrorDataReceived += (sender, e) => {
                        if (e.Data != null) {
                            dotnetLogWriter.WriteLine("ERROR: " + e.Data);
                            if (e.Data.Contains("ERROR") || e.Data.Contains("Error:") || e.Data.Contains("Exception:")) {
                                errorDetected = true;
                                errorDetectionTime = DateTime.Now;
                            }
                        }
                    };

                    dotnetProcess.Start();
                    dotnetProcess.BeginOutputReadLine();
                    dotnetProcess.BeginErrorReadLine();

                    WriteToMainLog($"[nijo-mcp] dotnetプロセスを開始しました");
                } catch (Exception ex) {
                    WriteToMainLog($"[nijo-mcp] dotnetプロセスの起動に失敗しました: {ex.Message}");
                    errorDetected = true;
                    errorDetectionTime = DateTime.Now;
                }

                // 少し待ってからアクセス確認を開始
                await Task.Delay(2000);

                // サービスの準備完了を待機
                var ready = false;
                var timeout = DateTime.Now.AddSeconds(10);
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
                    // ログファイルを定期的にチェック（0.5秒ごと）
                    if ((DateTime.Now - lastNpmLogCheck).TotalSeconds >= 0.5d) {
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
                                    WriteToMainLog($"[npm-log] {newContent.TrimEnd()}");

                                    // エラーチェック
                                    if (newContent.Contains("ERROR") || newContent.Contains("Error:")) {
                                        errorDetected = true;
                                        errorDetectionTime = DateTime.Now;
                                    }
                                }
                            }
                            lastNpmLogCheck = DateTime.Now;
                        } catch (Exception ex) {
                            WriteToMainLog($"[nijo-mcp] npmログファイルの読み取りに失敗しました: {ex.Message}");
                        }
                    }

                    if ((DateTime.Now - lastDotnetLogCheck).TotalSeconds >= 0.5d) {
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
                                    WriteToMainLog($"[dotnet-log] {newContent.TrimEnd()}");

                                    // エラーチェック
                                    if (newContent.Contains("ERROR") || newContent.Contains("Error:") || newContent.Contains("Exception:")) {
                                        errorDetected = true;
                                        errorDetectionTime = DateTime.Now;
                                    }
                                }
                            }
                            lastDotnetLogCheck = DateTime.Now;
                        } catch (Exception ex) {
                            WriteToMainLog($"[nijo-mcp] dotnetログファイルの読み取りに失敗しました: {ex.Message}");
                        }
                    }

                    // エラーが検出されてから一定時間経過したら中断
                    if (errorDetected && DateTime.Now > errorDetectionTime.AddSeconds(3)) {
                        // プロセスを終了
                        WriteToMainLog(StopDebugging());

                        return ToMcpResutJson(new {
                            Result = "エラーが検出されたため、デバッグを中断しました。",
                            NpmLogFile = npmLogPath,
                            DotnetLogFile = dotnetLogPath,
                            Details = mainOutput.ToString(),
                        });
                    }

                    if (!dotnetReady) {
                        dotnetReady = await IsServiceReadyAsync(DOTNET_URL);
                        if (dotnetReady) {
                            WriteToMainLog("[nijo-mcp] WebAPIの準備が完了しました。");
                        }
                    }

                    if (!npmReady) {
                        npmReady = await IsServiceReadyAsync(NPM_URL);
                        if (npmReady) {
                            WriteToMainLog("[nijo-mcp] フロントエンドの準備が完了しました。");
                        }
                    }

                    ready = dotnetReady && npmReady;

                    if (!ready) {
                        await Task.Delay(500); // 0.5秒待機
                    }
                }

                if (!ready) {
                    // プロセスを終了
                    WriteToMainLog(StopDebugging());

                    var status = new StringBuilder("タイムアウト: ");
                    if (!dotnetReady) status.Append("WebAPIの準備が完了しませんでした。");
                    if (!npmReady) status.Append("フロントエンドの準備が完了しませんでした。");

                    return ToMcpResutJson(new {
                        Result = status.ToString(),
                        NpmLogFile = npmLogPath,
                        DotnetLogFile = dotnetLogPath,
                        Details = mainOutput.ToString(),
                    });
                }

                return ToMcpResutJson(new {
                    Success = true,
                    Result = "デバッグを開始しました。",
                    LaunchedApplicationUrl = NPM_URL,
                    NpmLogFile = npmLogPath,
                    DotnetLogFile = dotnetLogPath,
                    Details = mainOutput.ToString(),
                });

            } catch (Exception ex) {
                // エラー発生時はプロセスを終了
                WriteToMainLog(StopDebugging());

                WriteToMainLog($"Error: {ex}");

                return ToMcpResutJson(new {
                    Result = "エラーが発生しました。",
                    Exception = ex.ToString(),
                });
            }
        }

        [McpServerTool(Name = "stop_debugging"), Description("ソースコード自動生成された方のアプリケーションのデバッグを終了する。")]
        public static string StopDebugging() {
            try {
                var result = new StringBuilder();

                // 特定のポートを使用しているプロセスを見つけて終了させる
                try {
                    // netstatコマンドを実行して、特定のポートを使っているプロセスのPIDを取得
                    var dotnetPid = FindProcessIdByPort(7098); // DOTNETのポート番号
                    var npmPid = FindProcessIdByPort(5173);    // NPMのポート番号

                    if (dotnetPid.HasValue) {
                        var killResultDotnet = KillProcessWithChildren(dotnetPid.Value);
                        result.AppendLine($"WebAPIプロセス(PID:{dotnetPid})の終了結果: {killResultDotnet}");
                    } else {
                        result.AppendLine("WebAPIプロセスが見つかりませんでした。");
                    }

                    if (npmPid.HasValue) {
                        var killResultNpm = KillProcessWithChildren(npmPid.Value);
                        result.AppendLine($"フロントエンドプロセス(PID:{npmPid})の終了結果: {killResultNpm}");
                    } else {
                        result.AppendLine("フロントエンドプロセスが見つかりませんでした。");
                    }
                } catch (Exception ex) {
                    return $$"""

                        ******* 既存プロセス停止処理 START ******
                        ポート検索処理でエラーが発生しました: {{ex}}
                        ******* 既存プロセス停止処理 END ******

                        """;
                }

                return $$"""

                    ******* 既存プロセス停止処理 START ******
                    {{result}}
                    ******* 既存プロセス停止処理 END ******

                    """;

            } catch (Exception ex) {
                return $$"""

                    ******* 既存プロセス停止処理 START ******
                    エラーが発生しました: {{ex}}
                    ******* 既存プロセス停止処理 END ******

                    """;
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

        // ログフォルダを準備する
        private static void PrepareLogDirectory() {
            var workDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location!)!, WORK_DIR);

            // カレントディレクトリにlogsフォルダがなければ作成
            if (!Directory.Exists(workDirectory)) Directory.CreateDirectory(workDirectory);

            // Git管理対象外
            File.WriteAllText(Path.Combine(workDirectory, ".gitignore"), "*");

            // 既存のログファイルがあれば削除
            string mainLogPath = Path.Combine(workDirectory, MAIN_LOG_FILE);
            string npmLogPath = Path.Combine(workDirectory, NPM_LOG_FILE);
            string dotnetLogPath = Path.Combine(workDirectory, DOTNET_LOG_FILE);
            if (File.Exists(mainLogPath)) File.Delete(mainLogPath);
            if (File.Exists(npmLogPath)) File.Delete(npmLogPath);
            if (File.Exists(dotnetLogPath)) File.Delete(dotnetLogPath);
        }

        // ログファイルの内容を読み取る
        private static string ReadLogFile(string fileName) {
            string logPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location!)!, WORK_DIR, fileName);
            if (!File.Exists(logPath)) {
                return "ログファイルが見つかりません。";
            }

            try {
                return File.ReadAllText(logPath);
            } catch (Exception ex) {
                return $"ログファイルの読み取りに失敗しました: {ex.Message}";
            }
        }

        // ポート番号からプロセスIDを取得するメソッド
        private static int? FindProcessIdByPort(int port) {
            // netstatコマンドを実行して、特定のポートを使用しているプロセスのPIDを取得
            var process = new Process();
            process.StartInfo.FileName = "netstat";
            process.StartInfo.Arguments = "-ano";  // すべてのコネクション情報とPIDを表示
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // 出力から該当するポートを使用しているプロセスのPIDを検索
            var lines = output.Split('\n');
            foreach (var line in lines) {
                // TCPコネクションをフィルタリング
                if (line.Contains("TCP") && line.Contains($":{port} ")) {
                    // 行の形式: "TCP    127.0.0.1:7098     0.0.0.0:0     LISTENING    12345"
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5) {
                        int pid;
                        if (int.TryParse(parts[parts.Length - 1], out pid)) {
                            return pid;
                        }
                    }
                }
            }

            return null;  // 見つからなかった場合
        }

        // 指定したPIDのプロセスとその子プロセスを終了するメソッド
        private static string KillProcessWithChildren(int pid) {
            var result = new StringBuilder();

            try {
                // taskkillコマンドを使ってプロセスツリーを強制終了
                var process = new Process();
                process.StartInfo.FileName = "taskkill";
                process.StartInfo.Arguments = $"/PID {pid} /T /F";  // /T: ツリー(子プロセスを含む) /F: 強制終了
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0) {
                    result.Append($"成功: {output.Trim()}");
                } else {
                    result.Append($"エラー(コード:{process.ExitCode}): {error.Trim()}");
                }
            } catch (Exception ex) {
                result.Append($"例外が発生しました: {ex.Message}");
            }

            return result.ToString();
        }

        #region MCPの戻り値
        // MCPツールの戻り値はJSONのみ

        private static string ToMcpResultError(string error) {
            return ToMcpResutJson(new { Success = false, Error = error });
        }
        private static string ToMcpResutJson<T>(T result) {
            var jsonOptions = new JsonSerializerOptions {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            };
            return JsonSerializer.Serialize(result, jsonOptions);
        }
        #endregion MCPの戻り値
    }
}
