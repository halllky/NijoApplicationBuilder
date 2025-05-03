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
        private const string NIJO_SLN = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\nijo.sln";
        private const string APPLICATION_TEMPLATE_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.ApplicationTemplate.Ver1";
        private const string DATA_PATTERN_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatterns";
        private const string DATA_PATTERN_REVEALED_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\自動テストで作成されたプロジェクト";
        private const string DATA_PATTERN_IMPLEMENTORS_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.IntegrationTest\DataPatternsImplementors";


        #region アプリケーションテンプレート
        [McpServerTool(Name = "generate_code"), Description(
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
        [McpServerTool(Name = "reveal_data_pattern"), Description(
            "Nijo.IntegrationTest の DataPatterns フォルダにあるXMLで「自動テストで作成されたプロジェクト」フォルダにアプリケーションを顕現させる。 ")]
        public static async Task<string> RevealDataPattern(
            [Description("DataPatternsフォルダにあるXMLのファイルの絶対パス")] string dataPatternsXmlFileFullPath) {

            try {
                if (string.IsNullOrEmpty(dataPatternsXmlFileFullPath)) {
                    return "DataPatternsフォルダにあるXMLのファイルの絶対パスを指定してください。";
                }
                if (!File.Exists(dataPatternsXmlFileFullPath)) {
                    return $"指定されたDataPatternsフォルダにあるXMLのファイルが見つかりません: {dataPatternsXmlFileFullPath}";
                }

                using var workDirectory = WorkDirectory.Prepare();
                workDirectory.WriteToMainLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [NijoMcpTools.RevealDataPattern] RevealDataPattern method called with path: {dataPatternsXmlFileFullPath}");

                // 「自動テストで作成されたプロジェクト」フォルダの内容を全部クリア
                Directory.Delete(DATA_PATTERN_REVEALED_DIR, true);

                // アプリケーションテンプレートを「自動テストで作成されたプロジェクト」フォルダにコピー。
                // ただし、アプリケーションテンプレートのnijo.xmlのデータ構造に由来するソースは除く
                CopyDirectoryRecursively(APPLICATION_TEMPLATE_DIR, DATA_PATTERN_REVEALED_DIR, path => {

                    // CreateQuerySource の実装などはコピーしない
                    if (path == Path.Combine(APPLICATION_TEMPLATE_DIR, "Core", "OverridedApplicationService.cs")) return false;

                    // 画面の実装はコピーしない。ただしHome.tsxだけはコピーする
                    if (path == Path.Combine(APPLICATION_TEMPLATE_DIR, "react", "src", "pages", "Home.tsx")) return true;
                    if (path.StartsWith(Path.Combine(APPLICATION_TEMPLATE_DIR, "react", "src", "pages"))) return false;

                    return true;
                });

                // 「自動テストで作成されたプロジェクト」のnijo.xmlを指定されたXMLファイルで上書き
                var nijoXmlDestination = Path.Combine(DATA_PATTERN_REVEALED_DIR, "nijo.xml");
                File.Copy(dataPatternsXmlFileFullPath, nijoXmlDestination, true);

                // OverridedApplicationService や画面の実装はスナップショットされたファイルで上書き
                var xmlFileNameWithoutExtension = Path.GetFileNameWithoutExtension(dataPatternsXmlFileFullPath);
                var existsSnapshotFiles = Directory.Exists(Path.Combine(DATA_PATTERN_IMPLEMENTORS_DIR, xmlFileNameWithoutExtension));
                if (existsSnapshotFiles) {
                    CopyDirectoryRecursively(Path.Combine(DATA_PATTERN_IMPLEMENTORS_DIR, xmlFileNameWithoutExtension), DATA_PATTERN_REVEALED_DIR, path => true);
                } else {
                    // スナップショットが無い場合はAIエージェントによるスナップショット用ファイルの実装の助けになるようなファイルを作成する
                    var appsrvFileName = Path.Combine(DATA_PATTERN_REVEALED_DIR, "Core", "OverridedApplicationService.cs");
                    var pagesIndexFileName = Path.Combine(DATA_PATTERN_REVEALED_DIR, "react", "src", "pages", "Index.tsx");

                    await File.WriteAllTextAsync(appsrvFileName, $$"""
                        using System;
                        using System.Collections.Generic;
                        using System.Linq;
                        using System.Text;
                        using System.Threading.Tasks;

                        namespace MyApp.Core;

                        partial class OverridedApplicationService {
                            // このクラスに生成されるabstractメソッドを実装してください。

                            // 例: 従業員DataModel から 従業員QueryModel のインスタンスを生成するメソッドを実装する
                            // protected override IQueryable<従業員SearchResult> CreateQuerySource(従業員SearchCondition searchCondition, IPresentationContext<従業員SearchConditionMessages> context) {
                            //     return DbContext.従業員DbSet.Select(e => new 従業員SearchResult {
                            //         従業員ID = e.従業員ID,
                            //         氏名 = e.氏名,
                            //         氏名カナ = e.氏名カナ,
                            //         退職日 = e.退職日,
                            //         所属部署 = e.所属部署.Select(d => new 所属部署SearchResult {
                            //             年度 = d.年度,
                            //             部署_部署名 = d.部署!.部署名,
                            //             部署_部署コード = d.部署!.部署コード,
                            //         }).ToList(),
                            //         Version = (int)e.Version!,
                            //     });
                            // }
                        }
                        """, new UTF8Encoding(false, false));

                    await File.WriteAllTextAsync(pagesIndexFileName, $$"""
                        // このファイルには画面の実装を記載してください。

                        import { RouteObjectWithSideMenuSetting } from "../routes";

                        // 例:
                        // import { 従業員一覧検索 } from "./従業員/従業員一覧検索";
                        // import { 従業員詳細編集 } from "./従業員/従業員詳細編集";
                        // import { 顧客一覧検索 } from "./顧客/顧客一覧検索";
                        // import { 顧客詳細編集 } from "./顧客/顧客詳細編集";

                        /** Home以外の画面のルーティング設定 */
                        export default function (): RouteObjectWithSideMenuSetting[] {
                          return []

                          // 例:
                          // return [
                          //   { path: '顧客', element: <顧客一覧検索 />, sideMenuLabel: "顧客一覧" },
                          //   { path: '顧客/new', element: <顧客詳細編集 /> },
                          //   { path: '顧客/:顧客ID', element: <顧客詳細編集 /> },
                          //   { path: '従業員', element: <従業員一覧検索 />, sideMenuLabel: "従業員一覧" },
                          //   { path: '従業員/new', element: <従業員詳細編集 /> },
                          //   { path: '従業員/:従業員ID', element: <従業員詳細編集 /> },
                          // ]
                        }
                        """, new UTF8Encoding(false, false));
                }

                return $$"""
                    「自動テストで作成されたプロジェクト」フォルダにアプリケーションを作成しました。
                    {{nijoXmlDestination}}
                    """;

            } catch (Exception ex) {
                return ex.ToString();
            }
        }

        [McpServerTool(Name = "save_implementation_snapshot"), Description(
            "「自動テストで作成されたプロジェクト」フォルダにあるアプリケーションの実装のうち、"
            + "当該データパターンのデータ構造に由来するカスタマイズ実装を、スナップショットとして保存する。")]
        public static async Task<string> SaveImplementationSnapshot() {
            try {
                using var workDirectory = WorkDirectory.Prepare();
                workDirectory.WriteToMainLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [NijoMcpTools.SaveImplementationSnapshot] SaveImplementationSnapshot method called.");

                // 「自動テストで作成されたプロジェクト」のnijo.xmlが「DataPatterns」フォルダのどのパターンから作成されたものかを特定する。
                // 「自動テストで作成されたプロジェクト」のnijo.xmlと完全一致する「DataPatterns」フォルダのXMLファイルを探すというロジックで特定する。
                var nijoXmlFileFullPath = Path.Combine(DATA_PATTERN_REVEALED_DIR, "nijo.xml");
                if (!File.Exists(nijoXmlFileFullPath)) {
                    return $"指定されたファイルが見つかりません: {nijoXmlFileFullPath}";
                }
                var nijoXmlFileContent = await File.ReadAllTextAsync(nijoXmlFileFullPath);
                var dataPatternsXmlFiles = Directory.GetFiles(DATA_PATTERN_DIR, "*.xml");

                string? snapshotDir = null;
                foreach (var dataPatternsXmlFile in dataPatternsXmlFiles) {
                    var dataPatternsXmlFileContent = await File.ReadAllTextAsync(dataPatternsXmlFile);
                    if (nijoXmlFileContent == dataPatternsXmlFileContent) {
                        snapshotDir = Path.Combine(DATA_PATTERN_IMPLEMENTORS_DIR, Path.GetFileNameWithoutExtension(dataPatternsXmlFile));
                        break;
                    }
                }
                if (snapshotDir == null) {
                    return "指定されたnijo.xmlに対応するデータパターンが見つかりません。";
                }

                // スナップショットフォルダが存在しない場合は作成する
                if (!Directory.Exists(snapshotDir)) {
                    Directory.CreateDirectory(snapshotDir);
                }

                // 「自動テストで作成されたプロジェクト」のフォルダの内容のうち、
                // 当該データパターンのデータ構造に由来するカスタマイズ実装をスナップショットフォルダにコピーする
                CopyDirectoryRecursively(DATA_PATTERN_REVEALED_DIR, snapshotDir, path => {
                    // OverridedApplicationService.cs と画面の実装のみコピーし、他は無視
                    if (path == Path.Combine(DATA_PATTERN_REVEALED_DIR, "Core", "OverridedApplicationService.cs")) return true;
                    if (path == Path.Combine(DATA_PATTERN_REVEALED_DIR, "react", "src", "pages", "index.tsx")) return true;
                    return false;
                });

                return $$"""
                    指定されたデータパターンのデータ構造に由来するカスタマイズ実装をスナップショットフォルダにコピーしました。
                    {{snapshotDir}}
                """;
            } catch (Exception ex) {
                return ex.ToString();
            }
        }
        #endregion 自動テストで作成されたプロジェクト
    }
}
