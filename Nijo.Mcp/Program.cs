using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using System.Reflection;

//Console.WriteLine(Nijo.Mcp.NijoMcpTools.GenerateCode(@"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.ApplicationTemplate.Ver1\nijo.xml"));
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
        private const string DOTNET_URL = "https://localhost:7098";
        private const string NPM_URL = "http://localhost:5173";
        private const string MAIN_LOG_FILE = "output.log";
        private const string START_CMD_FILE = "start_app.cmd";

        private static string GetWorkDirectoryFullPath() {
            return Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location!)!, // net9.0
                "..", // Debug
                "..", // bin
                "..", // Nijo.Mpc
                "..", // NijoApplicationBuilder
                "Nijo.Mpc.WorkDirectory"));
        }

        private static string RenderCmdContent(string nijoXmlFileFullPath, bool isDebug) {
            var workDirectory = GetWorkDirectoryFullPath();
            var mainLogPath = Path.Combine(workDirectory, MAIN_LOG_FILE);
            // ログファイル名を標準出力用と標準エラー用に分ける
            var npmLogPath_StdOut = Path.Combine(workDirectory, "output_npm_stdout.log");
            var npmLogPath_StdErr = Path.Combine(workDirectory, "output_npm_stderr.log");
            var dotnetLogPath_StdOut = Path.Combine(workDirectory, "output_dotnet_stdout.log");
            var dotnetLogPath_StdErr = Path.Combine(workDirectory, "output_dotnet_stderr.log");

            var nijoXmlDir = Path.GetDirectoryName(nijoXmlFileFullPath);
            var reactDir = Path.Combine(nijoXmlDir!, "react");
            var webApiDir = Path.Combine(nijoXmlDir!, "WebApi");

            return $$"""
                chcp 65001
                @rem ↑dotnetコマンド実行時に強制的に書き換えられてしまいnpmの標準入出力が化けるので先に書き換えておく

                @echo off
                setlocal
                set NO_COLOR=true

                set "PROJ_ROOT={{nijoXmlDir}}"
                set "NIJO_ROOT={{NIJO_PROJ}}"
                set "NIJO_EXE={{NIJO_PROJ}}\bin\Debug\net9.0\nijo.exe"

                {{(isDebug ? $$"""
                echo. > {{dotnetLogPath_StdOut}}
                echo. > {{dotnetLogPath_StdErr}}
                echo. > {{npmLogPath_StdOut}}
                echo. > {{npmLogPath_StdErr}}

                """ : $$"""

                """)}}
                @echo ******* {{(isDebug ? "start_debugging" : "generate_code")}} を開始します。 ******* >> "{{mainLogPath}}"
                @echo. >> "{{mainLogPath}}"
                @echo - 対象プロジェクト: %PROJ_ROOT% >> "{{mainLogPath}}"
                @echo - NijoApplicationBuilderルート: %NIJO_ROOT% >> "{{mainLogPath}}"
                @echo. >> "{{mainLogPath}}"

                @echo. >> "{{mainLogPath}}"
                @echo ************************************** >> "{{mainLogPath}}"
                @echo ******* ソースコードの自動生成プログラムの最新化を開始します。 ******* >> "{{mainLogPath}}"
                dotnet build %NIJO_ROOT% -c Debug >> "{{mainLogPath}}" 2>&1
                if not "%errorlevel%"=="0" (
                    @echo. >> "{{mainLogPath}}"
                    @echo nijo.exeのビルドでエラーが発生しました。処理を中断します。 >> "{{mainLogPath}}"
                    exit /b 1
                )

                @echo. >> "{{mainLogPath}}"
                @echo ************************************** >> "{{mainLogPath}}"
                @echo ******* ソースコードの自動生成のかけなおしを実行します。 ******* >> "{{mainLogPath}}"
                %NIJO_EXE% generate %PROJ_ROOT% >> "{{mainLogPath}}" 2>&1
                if not "%errorlevel%"=="0" (
                    @echo. >> "{{mainLogPath}}"
                    @echo ソースコードの自動生成のかけなおしでエラーが発生しました。処理を中断します。 >> "{{mainLogPath}}"
                    exit /b 1
                )

                {{(isDebug ? $$"""
                @echo. >> "{{mainLogPath}}"
                @echo ************************************** >> "{{mainLogPath}}"
                @echo ******* .NET デバッグプロセスを起動します。 ******* >> "{{mainLogPath}}"
                cd /d "{{webApiDir}}"
                powershell -Command "Start-Process -FilePath 'dotnet' -ArgumentList 'run --launch-profile https' -RedirectStandardOutput '{{dotnetLogPath_StdOut}}' -RedirectStandardError '{{dotnetLogPath_StdErr}}' -NoNewWindow"

                @echo. >> "{{mainLogPath}}"
                @echo ************************************** >> "{{mainLogPath}}"
                @echo ******* Node.js デバッグプロセスを起動します。 ******* >> "{{mainLogPath}}"
                cd /d "{{reactDir}}"
                powershell -Command "Start-Process -FilePath 'npm' -ArgumentList 'run dev' -RedirectStandardOutput '{{npmLogPath_StdOut}}' -RedirectStandardError '{{npmLogPath_StdErr}}' -NoNewWindow"

                @echo. >> "{{mainLogPath}}"
                @echo ************************************** >> "{{mainLogPath}}"
                @echo 起動指示を出しました。アプリケーションが実行されているかは各httpポートを監視して確認してください。 >> "{{mainLogPath}}"
                @echo - .NET    : {{DOTNET_URL}}/swagger >> "{{mainLogPath}}"
                @echo - Node.js : {{NPM_URL}} >> "{{mainLogPath}}"
                @echo. >> "{{mainLogPath}}"
                @echo 各プロセスの出力は以下のログを参照してください。 >> "{{mainLogPath}}"
                @echo - npm run dev の標準出力ログ: "{{npmLogPath_StdOut}}" >> "{{mainLogPath}}"
                @echo - npm run dev の標準エラーログ: "{{npmLogPath_StdErr}}" >> "{{mainLogPath}}"
                @echo - dotnet run の標準出力ログ: "{{dotnetLogPath_StdOut}}" >> "{{mainLogPath}}"
                @echo - dotnet run の標準エラーログ: "{{dotnetLogPath_StdErr}}" >> "{{mainLogPath}}"

                """ : $$"""
                @echo. >> "{{mainLogPath}}"
                @echo ************************************** >> "{{mainLogPath}}"
                @echo ******* .NET をビルドします。 ******* >> "{{mainLogPath}}"
                cd /d "{{nijoXmlDir}}"
                dotnet build >> {{mainLogPath}} 2>&1

                @echo. >> "{{mainLogPath}}"
                @echo ************************************** >> "{{mainLogPath}}"
                @echo ******* TypeScript の型検査を行ないます。 ******* >> "{{mainLogPath}}"
                endlocal
                pushd "{{reactDir}}"
                call tsc -b --noEmit >> "{{mainLogPath}}" 2>&1
                @echo. >> "{{mainLogPath}}"
                if "%errorlevel%"=="0" (
                    @echo エラーなし >> "{{mainLogPath}}"
                ) else (
                    @echo エラーが検知されました。 >> "{{mainLogPath}}"
                )
                @echo. >> "{{mainLogPath}}"
                popd

                """)}}
                @echo. >> "{{mainLogPath}}"
                @echo ************************************** >> "{{mainLogPath}}"
                @echo ******* 処理終了 ******* >> "{{mainLogPath}}"

                exit /b 0
                """.ReplaceLineEndings(" \r\n"); // cmd処理中にchcpしたときは改行コードの前にスペースが無いと上手く動かない
        }

        [McpServerTool(Name = "generate_code"), Description("ソースコードの自動生成処理の最新化、自動生成処理のかけなおし、コンパイルエラーチェックを行います。")]
        public static async Task<string> GenerateCode([Description("nijo.xmlのファイルの絶対パス")] string nijoXmlFileFullPath) {
            var mainOutput = new StringBuilder();

            try {
                // 既存のプロセスを終了 - これによって以前実行していたプロセスが終了する
                await StopDebugging(text => mainOutput.AppendLine(text), TimeSpan.FromSeconds(5));

                // ワークディレクトリを準備
                PrepareWorkDirectory();

                if (string.IsNullOrEmpty(nijoXmlFileFullPath)) {
                    return ToMcpResutJson(new { Error = "nijo.xmlのファイルの絶対パスを指定してください。" });
                }

                // 各種パス定義
                var workDirectory = GetWorkDirectoryFullPath();
                var mainLogPath = Path.Combine(workDirectory, MAIN_LOG_FILE);

                var nijoXmlDir = Path.GetDirectoryName(nijoXmlFileFullPath);
                var reactDir = Path.Combine(nijoXmlDir!, "react");

                if (!File.Exists(nijoXmlFileFullPath)) {
                    return ToMcpResutJson(new { Error = $"指定されたnijo.xmlファイルが見つかりません: {nijoXmlFileFullPath}" });
                }
                if (!Directory.Exists(reactDir)) {
                    return ToMcpResutJson(new { Error = $"reactフォルダが見つかりません: {reactDir}" });
                }
                // WebApiディレクトリの存在チェックはデバッグビルドではないので不要

                // .cmdファイルを作成
                string cmdFilePath = Path.Combine(workDirectory, START_CMD_FILE); // start_app.cmdを流用
                string cmdContent = RenderCmdContent(nijoXmlFileFullPath, false); // isDebug = false を指定
                File.WriteAllText(cmdFilePath, cmdContent, new UTF8Encoding(false, false));
                mainOutput.AppendLine($"[nijo-mcp] 実行スクリプトを作成しました: {cmdFilePath}");

                // .cmdファイルを実行
                mainOutput.AppendLine($"[nijo-mcp] スクリプトを実行します (出力は {mainLogPath} に保存されます)");
                Process? cmdProcess = null;
                try {
                    cmdProcess = new Process();
                    cmdProcess.StartInfo.FileName = "cmd";
                    cmdProcess.StartInfo.Arguments = $"/c \"{cmdFilePath}\"";
                    cmdProcess.StartInfo.UseShellExecute = false;
                    cmdProcess.StartInfo.CreateNoWindow = true;
                    cmdProcess.StartInfo.RedirectStandardOutput = true; // 標準出力をリダイレクト
                    cmdProcess.StartInfo.RedirectStandardError = true;  // 標準エラーをリダイレクト

                    var processOutput = new StringBuilder();
                    cmdProcess.OutputDataReceived += (sender, e) => { if (e.Data != null) processOutput.AppendLine(e.Data); };
                    cmdProcess.ErrorDataReceived += (sender, e) => { if (e.Data != null) processOutput.AppendLine(e.Data); };

                    cmdProcess.Start();
                    cmdProcess.BeginOutputReadLine();
                    cmdProcess.BeginErrorReadLine();
                    cmdProcess.WaitForExit();

                    mainOutput.AppendLine("[nijo-mcp] スクリプト実行出力:");
                    mainOutput.AppendLine(processOutput.ToString());

                    if (cmdProcess.ExitCode != 0) {
                        return ToMcpResutJson(new {
                            Success = false,
                            Result = $"[nijo-mcp] スクリプトの実行に失敗しました（終了コード: {cmdProcess.ExitCode}）",
                            ExecutionLog = mainOutput.ToString(),
                            LogFile = mainLogPath,
                        });
                    }

                } catch (Exception ex) {
                    return ToMcpResutJson(new {
                        Success = false,
                        Result = $"[nijo-mcp] スクリプトの実行中にエラーが発生しました（{ex.Message}）",
                        ExecutionLog = mainOutput.ToString(),
                        Exception = ex.ToString(),
                        LogFile = mainLogPath,
                    });
                }

                // 成功した場合
                return $$"""
                    ソースコードの自動生成を完了しました。

                    ---
                    【ビルド結果ログ】
                    {{File.ReadAllText(mainLogPath, new UTF8Encoding(false, false))}}
                    """;

            } catch (Exception ex) {
                mainOutput.AppendLine($"Error: {ex}");
                return ToMcpResutJson(new {
                    Success = false,
                    Result = "エラーが発生しました。",
                    Exception = ex.ToString(),
                    Details = mainOutput.ToString(),
                });
            }
        }

        [McpServerTool(Name = "start_debugging"), Description("ソースコード自動生成された方のアプリケーションのデバッグを開始する。既に開始されている場合はリビルドして再開する。")]
        public static async Task<string> StartDebugging([Description("nijo.xmlのファイルの絶対パス")] string nijoXmlFileFullPath) {

            // メインログへのC#からの書き込み
            var workDirectory = GetWorkDirectoryFullPath();
            var mainLogPath = Path.Combine(workDirectory, MAIN_LOG_FILE);
            void AppendLineToMainLog(string text) {
                var counter = 0; // 失敗しても数回はリトライ
                while (counter <= 3) {
                    try {
                        using var writer = new StreamWriter(mainLogPath, append: true, encoding: Encoding.UTF8);
                        writer.WriteLine(text);
                        return;
                    } catch {
                        counter++;
                    }
                }
                throw new InvalidOperationException($"ログ出力に失敗しました。");
            }

            try {
                // 既存のプロセスを終了 - これによって以前実行していたプロセスが終了する
                await StopDebugging(AppendLineToMainLog, TimeSpan.FromSeconds(5));

                // ワークディレクトリを準備
                PrepareWorkDirectory();

                if (string.IsNullOrEmpty(nijoXmlFileFullPath)) {
                    return ToMcpResutJson(new { Error = "nijo.xmlのファイルの絶対パスを指定してください。" });
                }

                // 各種パス定義
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

                // ------------------------------------


                // .cmdファイルを作成
                string startCmdPath = Path.Combine(workDirectory, START_CMD_FILE);
                string cmdContent = RenderCmdContent(nijoXmlFileFullPath, true);
                File.WriteAllText(startCmdPath, cmdContent, new UTF8Encoding(false, false));
                AppendLineToMainLog($"[nijo-mcp] 起動スクリプトを作成しました: {startCmdPath}");

                // .cmdファイルを実行
                AppendLineToMainLog($"[nijo-mcp] アプリケーションプロセスを開始します (出力は {mainLogPath} に保存されます)");
                Process? startProcess = null;
                try {
                    startProcess = new Process();
                    startProcess.StartInfo.FileName = "cmd";
                    startProcess.StartInfo.Arguments = $"/c \"{startCmdPath}\"";
                    startProcess.StartInfo.UseShellExecute = false;
                    startProcess.StartInfo.CreateNoWindow = true;

                    startProcess.Start();
                    startProcess.WaitForExit();

                    if (startProcess.ExitCode != 0) throw new InvalidOperationException($"終了コード: {startProcess.ExitCode}");

                } catch (Exception ex) {
                    return ToMcpResutJson(new {
                        Result = $"[nijo-mcp] 起動スクリプトの実行に失敗しました（{ex.Message}）。詳細はログを確認してください。",
                        LogFile = mainLogPath,
                        Exception = ex.ToString(),
                    });
                }

                AppendLineToMainLog("[nijo-mcp] 起動スクリプトの実行を完了しました。各Webサーバーが立ち上がっているかの確認を行ないます。");

                // 少し待ってからアクセス確認を開始
                await Task.Delay(1000);

                // サービスの準備完了を待機
                var ready = false;
                var dotnetReady = false;
                var npmReady = false;

                // Webのポートにリクエストを投げて結果が返ってくるかどうかで確認する
                using var _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(1) };
                async Task<bool> IsServiceReadyAsync(string url) {
                    try {
                        var response = await _httpClient.GetAsync(url);
                        return response.IsSuccessStatusCode;
                    } catch {
                        return false;
                    }
                }

                var timeout = DateTime.Now.AddSeconds(10);
                while (!ready && DateTime.Now < timeout) {
                    if (!dotnetReady) {
                        dotnetReady = await IsServiceReadyAsync($"{DOTNET_URL}/swagger");
                        if (dotnetReady) {
                            AppendLineToMainLog("[nijo-mcp] ASP.NET Core サーバーの起動を確認しました。");
                        } else {
                            AppendLineToMainLog("[nijo-mcp] ASP.NET Core サーバーはまだ起動されていません。");
                        }
                    }

                    if (!npmReady) {
                        npmReady = await IsServiceReadyAsync(NPM_URL);
                        if (npmReady) {
                            AppendLineToMainLog("[nijo-mcp] Node.js サーバーの起動を確認しました。");
                        } else {
                            AppendLineToMainLog("[nijo-mcp] Node.js サーバーはまだ起動されていません。");
                        }
                    }

                    ready = dotnetReady && npmReady;

                    if (!ready) {
                        AppendLineToMainLog("[nijo-mcp] 待機中...");
                        await Task.Delay(500); // 0.5秒待機
                    }
                }

                // Node.js と .NET のいずれかがタイムアウトまで起動しなかった場合、プロセスを終了
                if (!ready) {
                    AppendLineToMainLog("[nijo-mcp] Node.js と .NET のいずれかが起動していません。起動されている方のプロセスを中断します。");

                    await StopDebugging(AppendLineToMainLog, TimeSpan.FromSeconds(5));

                    var status = new StringBuilder("タイムアウト: ");
                    if (!dotnetReady) status.Append("WebAPIの準備が完了しませんでした。");
                    if (!npmReady) status.Append("フロントエンドの準備が完了しませんでした。");
                    status.Append("詳細はログを確認してください。");

                    return ToMcpResutJson(new {
                        Result = status.ToString(),
                        LogFile = mainLogPath,
                    });
                }

                // Node.js と .NET の両方の起動が確認できた
                return ToMcpResutJson(new {
                    Success = true,
                    Result = "デバッグを開始しました。",
                    LaunchedApplicationUrl = NPM_URL,
                });

            } catch (Exception ex) {
                // エラー発生時はプロセスを終了
                await StopDebugging(AppendLineToMainLog, TimeSpan.FromSeconds(5));

                AppendLineToMainLog($"Error: {ex}");

                return ToMcpResutJson(new {
                    Result = "エラーが発生しました。詳細はログを確認してください。",
                    LogFile = mainLogPath,
                    Exception = ex.ToString(),
                });
            }
        }

        [McpServerTool(Name = "stop_debugging"), Description("ソースコード自動生成された方のアプリケーションのデバッグを終了する。")]
        public static async Task<string> StopDebugging() {
            try {
                var result = new StringBuilder();
                await StopDebugging(text => result.AppendLine(text), TimeSpan.FromSeconds(5));

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

        [McpServerTool(Name = "get_debug_info"), Description(
            "ソースコード自動生成された方のアプリケーションのAPサーバーに問い合わせ、接続先DBなどの情報を取得する。" +
            "このツールを使用するためには、予め start_debugging でアプリケーションが実行開始されている必要がある。")]
        public static async Task<string> GeDebugInfo() {
            using var httpClient = new HttpClient() {
                BaseAddress = new Uri(DOTNET_URL),
            };
            var response = await httpClient.GetAsync("/api/debug-info");

            if (!response.IsSuccessStatusCode) {
                return $$"""
                    アプリケーションの実行設定の問い合わせに失敗しました。
                    start_debugging でアプリケーションが実行開始されていない可能性があります。
                    """;
            }

            return await response.Content.ReadAsStringAsync();
        }

        [McpServerTool(Name = "get_debug_info_er_diagram"), Description(
            "ソースコード自動生成された方のアプリケーションのAPサーバーに問い合わせ、テーブル定義のER図をmermaid形式で返す。" +
            "このツールを使用するためには、予め start_debugging でアプリケーションが実行開始されている必要がある。")]
        public static async Task<string> GeDebugInfoErDiagram() {
            using var httpClient = new HttpClient() {
                BaseAddress = new Uri(DOTNET_URL),
            };
            var response = await httpClient.GetAsync("/api/debug-info/er-diagram");

            if (!response.IsSuccessStatusCode) {
                return $$"""
                    アプリケーションの実行設定の問い合わせに失敗しました。
                    start_debugging でアプリケーションが実行開始されていない可能性があります。
                    """;
            }

            return await response.Content.ReadAsStringAsync();
        }

        [McpServerTool(Name = "reset_debug_database"), Description(
            "ソースコード自動生成された方のアプリケーションのデータベースをリセットし、ダミーデータを投入します。" +
            "このツールを使用するためには、予め start_debugging でアプリケーションが実行開始されている必要があります。")]
        public static async Task<string> ResetDebugDatabase() {
            using var httpClient = new HttpClient() {
                BaseAddress = new Uri(DOTNET_URL),
            };

            try {
                var content = new StringContent("", Encoding.UTF8, "application/json"); // 空のJSONコンテンツを作成
                var response = await httpClient.PostAsync("/api/debug-info/destroy-and-reset-database", content); // contentを渡す
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) {
                    return $"データベースのリセットに失敗しました (HTTP {(int)response.StatusCode}): {responseBody}";
                }

                return $"データベースのリセットとダミーデータの投入が完了しました: {responseBody}";

            } catch (HttpRequestException ex) {
                return $"APIエンドポイントへの接続に失敗しました。start_debugging でアプリケーションが実行開始されているか確認してください。 Error: {ex.Message}";
            } catch (Exception ex) {
                return $"予期せぬエラーが発生しました: {ex.Message}";
            }
        }

        [McpServerTool(Name = "execute_debug_sql"), Description(
            "指定されたSQLクエリを実行し、結果を返します。SELECT文のみ実行可能です。" +
            "このツールを使用するためには、予め start_debugging でアプリケーションが実行開始されている必要がある。")]
        public static async Task<string> ExecuteDebugSql([Description("実行するSQLクエリ")] string sql) {
            using var httpClient = new HttpClient() {
                BaseAddress = new Uri(DOTNET_URL),
            };

            try {
                var requestBody = JsonSerializer.Serialize(new { sql });
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("/api/debug-info/execute-sql", content);

                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) {
                    return $"SQLの実行に失敗しました (HTTP {(int)response.StatusCode}): {responseBody}";
                }

                // 成功時はJSONをそのまま返す
                return responseBody;

            } catch (HttpRequestException ex) {
                return $"APIエンドポイントへの接続に失敗しました。start_debugging でアプリケーションが実行開始されているか確認してください。 Error: {ex.Message}";
            } catch (Exception ex) {
                return $"予期せぬエラーが発生しました: {ex.Message}";
            }
        }

        // ログフォルダを準備する
        private static void PrepareWorkDirectory() {
            var workDirectory = GetWorkDirectoryFullPath();

            // ワークフォルダがなければ作成
            if (!Directory.Exists(workDirectory)) Directory.CreateDirectory(workDirectory);

            // Git管理対象外
            File.WriteAllText(Path.Combine(workDirectory, ".gitignore"), "*");

            // 既存のログファイルがあれば削除 (ファイル名を追加)
            string mainLogPath = Path.Combine(workDirectory, MAIN_LOG_FILE);
            string npmLogPath_StdOut = Path.Combine(workDirectory, "output_npm_stdout.log");
            string npmLogPath_StdErr = Path.Combine(workDirectory, "output_npm_stderr.log");
            string dotnetLogPath_StdOut = Path.Combine(workDirectory, "output_dotnet_stdout.log");
            string dotnetLogPath_StdErr = Path.Combine(workDirectory, "output_dotnet_stderr.log");
            string startCmdPath = Path.Combine(workDirectory, START_CMD_FILE);
            if (File.Exists(mainLogPath)) File.Delete(mainLogPath);
            if (File.Exists(npmLogPath_StdOut)) File.Delete(npmLogPath_StdOut);
            if (File.Exists(npmLogPath_StdErr)) File.Delete(npmLogPath_StdErr);
            if (File.Exists(dotnetLogPath_StdOut)) File.Delete(dotnetLogPath_StdOut);
            if (File.Exists(dotnetLogPath_StdErr)) File.Delete(dotnetLogPath_StdErr);
            if (File.Exists(startCmdPath)) File.Delete(startCmdPath);
        }

        //
        private static async Task StopDebugging(Action<string> outLog, TimeSpan timeout) {
            // ASP.NET Core
            try {
                outLog("[stop_debugging] ASP.NET Core サーバーのポート検索開始");

                var dotnetPid = FindProcessIdByPort(7098, timeout); // DOTNETのポート番号

                if (dotnetPid.HasValue) {
                    outLog($"[stop_debugging] ASP.NET Coreプロセス(PID:{dotnetPid})を終了します。");

                    await KillProcessWithChildren(dotnetPid.Value, timeout, outLog);

                    outLog($"[stop_debugging] ASP.NET Coreプロセス(PID:{dotnetPid})を終了しました。");
                } else {
                    outLog("[stop_debugging] ASP.NET Coreプロセスが見つかりませんでした。");
                }
            } catch (Exception ex) {
                outLog($"[stop_debugging] ASP.NET Core サーバーの停止でエラーが発生しました: {ex.Message}");
            }

            // Node.js
            try {
                outLog("[stop_debugging] Node.js サーバーのポート検索開始");

                var npmPid = FindProcessIdByPort(5173, timeout);    // NPMのポート番号

                if (npmPid.HasValue) {
                    outLog($"[stop_debugging] Node.js フロントエンドプロセス(PID:{npmPid})を終了します。");

                    await KillProcessWithChildren(npmPid.Value, timeout, outLog);

                    outLog($"[stop_debugging] フロントエンドプロセス(PID:{npmPid})を終了しました。");
                } else {
                    outLog("[stop_debugging] フロントエンドプロセスが見つかりませんでした。");
                }
            } catch (Exception ex) {
                outLog($"[stop_debugging] Node.js サーバーの停止でエラーが発生しました: {ex.Message}");
            }
        }

        // ポート番号からプロセスIDを取得するメソッド
        private static int? FindProcessIdByPort(int port, TimeSpan timeout) {
            // netstatコマンドを実行して、特定のポートを使用しているプロセスのPIDを取得
            var process = new Process();
            process.StartInfo.FileName = "netstat";
            process.StartInfo.Arguments = "-ano";  // すべてのコネクション情報とPIDを表示
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(timeout);

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
        private static Task KillProcessWithChildren(int pid, TimeSpan timeout, Action<string> outLog) {
            try {
                // taskkillではなくwmicを使用してプロセスを終了
                var process = new Process();
                process.StartInfo.FileName = "wmic";
                process.StartInfo.Arguments = $"process where ProcessId={pid} delete";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                var maybeFinish = false;
                process.OutputDataReceived += (sender, args) => {
                    if (args.Data == null) return;
                    outLog($"[wmic {pid} stdout] {args.Data}");
                    maybeFinish = true;
                };
                process.ErrorDataReceived += (sender, args) => {
                    if (args.Data == null) return;
                    outLog($"[wmic {pid} stderr] {args.Data}");
                    maybeFinish = true;
                };

                outLog($"[wmic {pid}] 開始");

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var timeoutLimit = DateTime.Now + timeout;
                while (true) {
                    if (DateTime.Now > timeoutLimit) {
                        outLog($"[wmic {pid}] タイムアウト");
                        process.CancelOutputRead();
                        process.CancelErrorRead();
                        process.Kill();
                        break;

                    } else if (maybeFinish) {
                        outLog($"[wmic {pid}] 終了");
                        process.CancelOutputRead();
                        process.CancelErrorRead();
                        break;

                    } else {
                        outLog($"[wmic {pid}] 完了待機中...");
                        Thread.Sleep(500);
                    }
                }
            } catch (Exception ex) {
                outLog($"[wmic {pid}] 例外が発生しました: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        // MCPツールの戻り値をJSONにする
        private static string ToMcpResutJson<T>(T result) {
            var jsonOptions = new JsonSerializerOptions {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            };
            return JsonSerializer.Serialize(result, jsonOptions);
        }
    }
}
