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
    public static partial class NijoMcpTools {
        private const string NIJO_PROJ = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo"; // とりあえずハードコード
        private const string DOTNET_URL = "https://localhost:7098";
        private const string NPM_URL = "http://localhost:5173";


        [McpServerTool(Name = "generate_code"), Description("ソースコードの自動生成処理の最新化、自動生成処理のかけなおし、コンパイルエラーチェックを行います。")]
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

                await 既存デバッグプロセス中止(workDirectory);

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

        [McpServerTool(Name = "start_debugging"), Description("ソースコード自動生成された方のアプリケーションのデバッグを開始する。既に開始されている場合はリビルドして再開する。")]
        public static async Task<string> StartDebugging([Description("nijo.xmlのファイルの絶対パス")] string nijoXmlFileFullPath) {
            try {
                if (string.IsNullOrEmpty(nijoXmlFileFullPath)) {
                    return "nijo.xmlのファイルの絶対パスを指定してください。";
                }
                var nijoXmlDir = Path.GetDirectoryName(nijoXmlFileFullPath)!;

                using var workDirectory = WorkDirectory.Prepare();

                await 既存デバッグプロセス中止(workDirectory);

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
                    {{await デバッグ中サイト情報取得(TimeSpan.FromSeconds(5))}}
                    """;
            } catch (Exception ex) {
                return ex.ToString();
            }
        }

        [McpServerTool(Name = "stop_debugging"), Description("ソースコード自動生成された方のアプリケーションのデバッグを終了する。")]
        public static async Task<string> StopDebugging() {
            try {
                using var workDirectory = WorkDirectory.Prepare();

                await 既存デバッグプロセス中止(workDirectory);

                return "デバッグを停止しました。";

            } catch (Exception ex) {
                return ex.ToString();
            }
        }

        [McpServerTool(Name = "get_debug_info"), Description(
            "ソースコード自動生成された方のアプリケーションのAPサーバーに問い合わせ、接続先DBなどの情報を取得する。" +
            "このツールを使用するためには、予め start_debugging でアプリケーションが実行開始されている必要がある。")]
        public static async Task<string> GeDebugInfo() {
            try {
                return await デバッグ中サイト情報取得(TimeSpan.FromSeconds(5));

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
    }
}
