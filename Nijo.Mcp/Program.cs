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

//using var wd = Nijo.Mcp.WorkDirectory.Prepare();
//await Nijo.Mcp.NijoMcpTools.アプリケーションテンプレートを自動テストで作成されたプロジェクトにコピーする(wd, @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns\000_単純な集約.xml");

// -----------------------------------

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
    public static partial class NijoMcpTools {
        private const string NIJO_PROJ = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo"; // とりあえずハードコード
        private const string DOTNET_URL = "https://localhost:7098";
        private const string NPM_URL = "http://localhost:5173";
        private const string NIJO_SLN = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\nijo.sln";
        private const string APPLICATION_TEMPLATE_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.ApplicationTemplate.Ver1";
        private const string DATA_PATTERN_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns";
        private const string DATA_PATTERN_REVEALED_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\自動テストで作成されたプロジェクト";
        private const string DATA_PATTERN_IMPLEMENTORS_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest.DataPatternsImplementors";


        #region アプリケーションテンプレート
        private const string TOOL_CHECK_COMPILE_ERROR = "check_compile_error";
        [McpServerTool(Name = TOOL_CHECK_COMPILE_ERROR), Description(
            "ソースコードの自動生成処理の最新化、自動生成処理のかけなおし、コンパイルエラーチェックを行います。")]
        public static async Task<string> GenerateCode([Description("nijo.xmlのファイルの絶対パス")] string nijoXmlFileFullPath) {
            try {
                if (string.IsNullOrEmpty(nijoXmlFileFullPath)) {
                    return "nijo.xmlのファイルの絶対パスを指定してください。";
                }
                var nijoXmlDir = Path.GetDirectoryName(nijoXmlFileFullPath)!;
                var reactDir = Path.Combine(nijoXmlDir!, "react");

                if (!File.Exists(nijoXmlFileFullPath)) {
                    return $"指定されたnijo.xmlファイルが見つかりません: {nijoXmlFileFullPath}";
                }
                if (!Directory.Exists(reactDir)) {
                    return $"reactフォルダが見つかりません: {reactDir}";
                }

                using var workDirectory = WorkDirectory.Prepare();

                if (!await 既存デバッグプロセス中止(workDirectory)) {
                    return workDirectory.WithMainLogContents("既存デバッグプロセス中止に失敗しました。");
                }
                if (!await ソースコード自動生成かけなおし(workDirectory, nijoXmlDir)) {
                    return workDirectory.WithMainLogContents("ソースコードの自動生成に失敗しました。");
                }
                if (!await コンパイルエラーチェック(workDirectory, nijoXmlDir)) {
                    return workDirectory.WithMainLogContents("コンパイルエラーが発生しました。");
                }

                return "ソースコードの自動生成を完了しました。コンパイルエラーもありません。";
            } catch (Exception ex) {
                return ex.ToString();
            }
        }

        [McpServerTool(Name = "start_debugging"), Description(
            "ソースコード自動生成された方のアプリケーションのデバッグを開始する。既に開始されている場合はリビルドして再開する。")]
        public static async Task<string> StartDebugging([Description("nijo.xmlのファイルの絶対パス")] string nijoXmlFileFullPath) {
            try {
                if (string.IsNullOrEmpty(nijoXmlFileFullPath)) {
                    return "nijo.xmlのファイルの絶対パスを指定してください。";
                }
                var nijoXmlDir = Path.GetDirectoryName(nijoXmlFileFullPath)!;

                using var workDirectory = WorkDirectory.Prepare();

                if (!await 既存デバッグプロセス中止(workDirectory)) {
                    return workDirectory.WithMainLogContents("既存デバッグプロセス中止に失敗しました。");
                }
                if (!await ソースコード自動生成かけなおし(workDirectory, nijoXmlDir)) {
                    return workDirectory.WithMainLogContents("ソースコードの自動生成に失敗しました。");
                }
                if (!await コンパイルエラーチェック(workDirectory, nijoXmlDir)) {
                    return workDirectory.WithMainLogContents("コンパイルエラーが発生しました。");
                }
                if (!await デバッグ開始(workDirectory, nijoXmlDir)) {
                    return workDirectory.WithMainLogContents("デバッグ開始に失敗しました。");
                }

                return $$"""
                    デバッグを開始しました。
                    ---
                    {{await デバッグ中サイト情報取得(workDirectory, TimeSpan.FromSeconds(5))}}
                    """;
            } catch (Exception ex) {
                return ex.ToString();
            }
        }

        [McpServerTool(Name = "stop_debugging"), Description(
            "ソースコード自動生成された方のアプリケーションのデバッグを終了する。")]
        public static async Task<string> StopDebugging() {
            using var workDirectory = WorkDirectory.Prepare();
            // MCP サーバー自身の PID をログに出力
            var mcpProcessId = Environment.ProcessId;
            workDirectory.WriteToMainLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [NijoMcpTools.StopDebugging] MCP Process ID: {mcpProcessId}");
            workDirectory.WriteToMainLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [NijoMcpTools.StopDebugging] StopDebugging method called. Calling 既存デバッグプロセス中止...");

            // 既存デバッグプロセス中止 を呼び出し、結果を待つ
            bool success = await 既存デバッグプロセス中止(workDirectory);

            // await の完了直後に結果を返す
            if (success) {
                workDirectory.WriteToMainLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [NijoMcpTools.StopDebugging] 既存デバッグプロセス中止 succeeded. Returning success message.");
                return "デバッグを停止しました。";
            } else {
                workDirectory.WriteToMainLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [NijoMcpTools.StopDebugging] 既存デバッグプロセス中止 failed. Returning failure message.");
                // 失敗した場合でも、ログの内容を含めて返す
                return workDirectory.WithMainLogContents("既存デバッグプロセス中止に失敗しました。");
            }
            // この return 文の後には何も実行されない
        }

        [McpServerTool(Name = "get_debug_info"), Description(
            "ソースコード自動生成された方のアプリケーションのAPサーバーに問い合わせ、プロセスが実行中か否かや、接続先DBがどこかなどの情報を取得する。")]
        public static async Task<string> GeDebugInfo() {
            try {
                using var workDirectory = WorkDirectory.Prepare();
                return await デバッグ中サイト情報取得(workDirectory, TimeSpan.FromSeconds(5));

            } catch (Exception ex) {
                return ex.ToString();
            }
        }

        [McpServerTool(Name = "get_debug_info_er_diagram"), Description(
            "ソースコード自動生成された方のアプリケーションのAPサーバーに問い合わせ、テーブル定義のER図をmermaid形式で返す。" +
            "このツールを使用するためには、予め start_debugging でアプリケーションが実行開始されている必要がある。")]
        public static async Task<string> GeDebugInfoErDiagram() {
            try {
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

            } catch (Exception ex) {
                return ex.ToString();
            }
        }

        [McpServerTool(Name = "reset_debug_database"), Description(
            "ソースコード自動生成された方のアプリケーションのデータベースをリセットし、ダミーデータを投入します。" +
            "このツールを使用するためには、予め start_debugging でアプリケーションが実行開始されている必要があります。")]
        public static async Task<string> ResetDebugDatabase() {
            try {
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

            } catch (Exception ex) {
                return ex.ToString();
            }
        }

        [McpServerTool(Name = "execute_debug_sql"), Description(
            "指定されたSQLクエリを実行し、結果を返します。SELECT文のみ実行可能です。" +
            "このツールを使用するためには、予め start_debugging でアプリケーションが実行開始されている必要がある。")]
        public static async Task<string> ExecuteDebugSql([Description("実行するSQLクエリ")] string sql) {
            try {
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
            } catch (Exception ex) {
                return ex.ToString();
            }
        }
        #endregion アプリケーションテンプレート


        #region NUnit
        [McpServerTool(Name = "list_tests"), Description(
            "nijo.slnに含まれるすべてのユニットテストを列挙して返します。")]
        public static async Task<string> ListTests() {
            try {
                using var workDirectory = WorkDirectory.Prepare();
                workDirectory.WriteToMainLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [NijoMcpTools.ListTests] ListTests method called.");

                var exitCode = await ExecuteProcess(
                    "dotnet-test-list",
                    startInfo => {
                        startInfo.FileName = "dotnet";
                        startInfo.ArgumentList.Add("test");
                        startInfo.ArgumentList.Add(NIJO_SLN);
                        startInfo.ArgumentList.Add("--list-tests");
                    },
                    workDirectory,
                    TimeSpan.FromMinutes(5)
                );

                if (exitCode != 0) {
                    return workDirectory.WithMainLogContents("ユニットテストの列挙に失敗しました。");
                }

                return workDirectory.WithMainLogContents("ユニットテストの列挙が完了しました（詳細ログ）。");
            } catch (Exception ex) {
                return ex.ToString();
            }
        }

        [McpServerTool(Name = "run_test"), Description(
            "指定されたユニットテストを実行し、結果を返します。")]
        public static async Task<string> RunTest([Description("実行するテストの名前またはフィルター式")] string testFilter) {
            try {
                if (string.IsNullOrEmpty(testFilter)) {
                    return "実行するテストの名前またはフィルター式を指定してください。";
                }

                using var workDirectory = WorkDirectory.Prepare();
                workDirectory.WriteToMainLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [NijoMcpTools.RunTest] RunTest method called with filter: {testFilter}");

                var exitCode = await ExecuteProcess(
                    "dotnet-test-run",
                    startInfo => {
                        startInfo.FileName = "dotnet";
                        startInfo.ArgumentList.Add("test");
                        startInfo.ArgumentList.Add(NIJO_SLN);
                        startInfo.ArgumentList.Add("--filter");
                        startInfo.ArgumentList.Add(testFilter);
                        startInfo.ArgumentList.Add("-v");
                        startInfo.ArgumentList.Add("normal");
                    },
                    workDirectory,
                    TimeSpan.FromMinutes(10)
                );

                if (exitCode != 0) {
                    return workDirectory.WithMainLogContents("ユニットテストの実行に失敗しました。");
                }

                return workDirectory.WithMainLogContents("ユニットテストの実行が完了しました。");
            } catch (Exception ex) {
                return ex.ToString();
            }
        }
        #endregion NUnit


        #region 自動テストで作成されたプロジェクト
        private const string TOOL_START_IMPLEMENTATION_SESSION = "start_implementation_session";
        private const string TOOL_CONTINUE_IMPLEMENTATION_SESSION = "continue_implementation_session";
        private const string NEXT_ACTION_FILE = "SESSION_NEXT_ACTION";

        [McpServerTool(Name = TOOL_START_IMPLEMENTATION_SESSION), Description(
            "Nijo.IntegrationTest の DataPatterns フォルダにあるXMLと対応する OverridedApplicationService などの実装を作成するセッションを開始する。")]
        public static async Task<string> StartImplementationSession() {
            using (var workDirectory = WorkDirectory.Prepare()) {
                var nextActionFile = Path.Combine(workDirectory.DirectoryPath, NEXT_ACTION_FILE);
                if (File.Exists(nextActionFile)) File.Delete(nextActionFile);
            }
            return await NextImplementationSession();
        }

        [McpServerTool(Name = TOOL_CONTINUE_IMPLEMENTATION_SESSION), Description(
            $"{TOOL_START_IMPLEMENTATION_SESSION} ツール内の指示により呼び出されるツール。")]
        public static async Task<string> NextImplementationSession() {
            string? nextActionFile = null;
            try {
                using var workDirectory = WorkDirectory.Prepare();

                if (!await 既存デバッグプロセス中止(workDirectory)) {
                    return workDirectory.WithMainLogContents("既存デバッグプロセス中止に失敗しました。");
                }

                // このファイルに状態を記録し、AIエージェントに対話的に指示を送る
                nextActionFile = Path.Combine(workDirectory.DirectoryPath, NEXT_ACTION_FILE);
                var currentPatternFile = Path.Combine(workDirectory.DirectoryPath, "SESSION_CURRENT_PATTERN");

                const string NEXT_IMPL_REACT = "::Reactの画面を実装させる::";
                const string NEXT_TRY_COMPLIE = "::コンパイルエラーが出なくなるまで修正を繰り返させる::";
                const string NEXT_FORWARD = "::次のデータパターンXMLに進む::";

                // セッション開始（DataPatternの最初のファイルでセットアップを行なう）
                if (!File.Exists(nextActionFile)) {
                    var nextPatternXmlFullPath = GetNextDataPatternXmlFullPath(null) ?? throw new InvalidOperationException("DataPatternsフォルダにxmlが1個も無い");

                    File.WriteAllText(nextActionFile, NEXT_IMPL_REACT);
                    File.WriteAllText(currentPatternFile, nextPatternXmlFullPath);

                    return await SetupNextPatternAndEchoImplementAppSrv(workDirectory, nextPatternXmlFullPath, deleteAll: true);
                }

                // 1つ前のセッションの指示を読み取る
                var nextAction = File.ReadAllText(nextActionFile);
                var currentPattern = File.ReadAllText(currentPatternFile);

                switch (nextAction) {
                    // Reactの実装を促す
                    case NEXT_IMPL_REACT:
                        File.WriteAllText(nextActionFile, NEXT_TRY_COMPLIE);
                        return $$"""
                            下記TypeScriptファイル内の関数（React Router のルーティング定義）と、
                            当該ルーティングで定義される画面のソースコードを、下記スキーマ定義のデータ構造に合うように実装してください。

                            * 実装対象: {{Path.Combine(DATA_PATTERN_REVEALED_DIR, "react", "src", "pages", "index.tsx")}}
                            * スキーマ定義: {{Path.Combine(DATA_PATTERN_REVEALED_DIR, "nijo.xml")}}

                            実装例は当該TypeScriptファイル内のコメントや、
                            "{{Path.Combine(DATA_PATTERN_REVEALED_DIR, "react", "src", "debug-rooms")}}" フォルダ内のサンプルを参照してください。

                            実装完了後、 "{{TOOL_CONTINUE_IMPLEMENTATION_SESSION}}" ツールを呼び出してください。
                            """;

                    // コンパイルエラーが出なくなるまで繰り返させる
                    case NEXT_TRY_COMPLIE:
                        File.WriteAllText(nextActionFile, NEXT_FORWARD);
                        return $$"""
                            "{{TOOL_CHECK_COMPILE_ERROR}}" ツールを "{{Path.Combine(DATA_PATTERN_REVEALED_DIR, "nijo.xml")}}" で呼び出し、
                            あなたがこれまでに実装した C#, TypeScript のソースでコンパイルエラーが出ないかどうかの確認を行なってください。
                            ツール実行後、以下いずれかの対応を行なってください。

                            * エラーが無かった場合
                              * "{{TOOL_CONTINUE_IMPLEMENTATION_SESSION}}" ツールを呼び出してください。
                            * エラーがあった場合
                              * あなたが実装したソースコードが原因であれば、ソースコードを修正し、再度 "{{TOOL_CHECK_COMPILE_ERROR}}" ツールを呼んでください。
                              * 自動生成されたソースコード中に問題があるなど、あなたが実装したソースコードに原因が無い場合、 "{{TOOL_CONTINUE_IMPLEMENTATION_SESSION}}" ツールを呼んでください。
                            """;

                    // AIエージェントが実装した成果を保存し、次のパターンへ進む
                    case NEXT_FORWARD:
                        AIエージェントの実装成果をスナップショットフォルダに保存する(currentPattern);
                        var next = GetNextDataPatternXmlFullPath(currentPattern);
                        if (next != null) {
                            File.WriteAllText(nextActionFile, NEXT_IMPL_REACT);
                            File.WriteAllText(currentPatternFile, next);

                            return await SetupNextPatternAndEchoImplementAppSrv(workDirectory, next, deleteAll: false);

                        } else {
                            return $$"""
                                タスクは完了です。
                                """;
                        }

                    default:
                        throw new InvalidOperationException($"不正な状態: {nextAction}");
                }

                // ファイルコピー => ソースコード自動生成かけなおし => アプリケーションサービスクラスの実装を指示
                static async Task<string> SetupNextPatternAndEchoImplementAppSrv(WorkDirectory workDirectory, string nextPatternXmlFullPath, bool deleteAll) {
                    if (!await アプリケーションテンプレートを自動テストで作成されたプロジェクトにコピーする(workDirectory, nextPatternXmlFullPath, deleteAll)) {
                        return workDirectory.WithMainLogContents("アプリケーションテンプレートの「自動テストで作成されたプロジェクト」へのコピーに失敗しました。");
                    }
                    if (!await ソースコード自動生成かけなおし(workDirectory, DATA_PATTERN_REVEALED_DIR)) {
                        return workDirectory.WithMainLogContents("ソースコードの自動生成に失敗しました。");
                    }
                    return $$"""
                        データパターン "{{Path.GetFileName(nextPatternXmlFullPath)}}" の定義で「自動テストで作成されたプロジェクト」の自動生成コードを洗い替えました。

                        下記C#ファイル内のクラスで定義可能な abstract メソッドを、下記スキーマ定義のデータ構造に合うように実装してください。
                        実装例は当該C#ファイル内のコメントを参照してください。

                        * 実装対象: {{Path.Combine(DATA_PATTERN_REVEALED_DIR, "Core", "OverridedApplicationService.cs")}}
                        * スキーマ定義: {{Path.Combine(DATA_PATTERN_REVEALED_DIR, "nijo.xml")}}

                        実装完了後、 "{{TOOL_CONTINUE_IMPLEMENTATION_SESSION}}" ツールを呼び出してください。
                        """;
                }

            } catch (Exception ex) {
                if (nextActionFile != null && File.Exists(nextActionFile)) File.Delete(nextActionFile);
                return ex.ToString();
            }
        }
        #endregion 自動テストで作成されたプロジェクト
    }
}
