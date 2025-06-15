using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using System.Reflection;
using System.Text.RegularExpressions;

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

        private const string SQLITE_EXE_PATH = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20250604_sqlite3\sqlite-tools-win-x64-3500000\sqlite3.exe";

        #region アプリケーションテンプレート
        private const string TOOL_CHECK_COMPILE_ERROR = "generate_code";
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

        //[McpServerTool(Name = "get_debug_info"), Description(
        //    "ソースコード自動生成された方のアプリケーションのAPサーバーに問い合わせ、プロセスが実行中か否かや、接続先DBがどこかなどの情報を取得する。")]
        //public static async Task<string> GeDebugInfo() {
        //    try {
        //        using var workDirectory = WorkDirectory.Prepare();
        //        return await デバッグ中サイト情報取得(workDirectory, TimeSpan.FromSeconds(5));

        //    } catch (Exception ex) {
        //        return ex.ToString();
        //    }
        //}

        //[McpServerTool(Name = "get_debug_info_er_diagram"), Description(
        //    "ソースコード自動生成された方のアプリケーションのAPサーバーに問い合わせ、テーブル定義のER図をmermaid形式で返す。" +
        //    "このツールを使用するためには、予め start_debugging でアプリケーションが実行開始されている必要がある。")]
        //public static async Task<string> GeDebugInfoErDiagram() {
        //    try {
        //        using var httpClient = new HttpClient() {
        //            BaseAddress = new Uri(DOTNET_URL),
        //        };
        //        var response = await httpClient.GetAsync("/api/debug-info/er-diagram");

        //        if (!response.IsSuccessStatusCode) {
        //            return $$"""
        //            アプリケーションの実行設定の問い合わせに失敗しました。
        //            start_debugging でアプリケーションが実行開始されていない可能性があります。
        //            """;
        //        }

        //        return await response.Content.ReadAsStringAsync();

        //    } catch (Exception ex) {
        //        return ex.ToString();
        //    }
        //}

        //[McpServerTool(Name = "reset_debug_database"), Description(
        //    "ソースコード自動生成された方のアプリケーションのデータベースをリセットし、ダミーデータを投入します。" +
        //    "このツールを使用するためには、予め start_debugging でアプリケーションが実行開始されている必要があります。")]
        //public static async Task<string> ResetDebugDatabase() {
        //    try {
        //        using var httpClient = new HttpClient() {
        //            BaseAddress = new Uri(DOTNET_URL),
        //        };

        //        try {
        //            var content = new StringContent("", Encoding.UTF8, "application/json"); // 空のJSONコンテンツを作成
        //            var response = await httpClient.PostAsync("/api/debug-info/destroy-and-reset-database", content); // contentを渡す
        //            var responseBody = await response.Content.ReadAsStringAsync();

        //            if (!response.IsSuccessStatusCode) {
        //                return $"データベースのリセットに失敗しました (HTTP {(int)response.StatusCode}): {responseBody}";
        //            }

        //            return $"データベースのリセットとダミーデータの投入が完了しました: {responseBody}";

        //        } catch (HttpRequestException ex) {
        //            return $"APIエンドポイントへの接続に失敗しました。start_debugging でアプリケーションが実行開始されているか確認してください。 Error: {ex.Message}";
        //        } catch (Exception ex) {
        //            return $"予期せぬエラーが発生しました: {ex.Message}";
        //        }

        //    } catch (Exception ex) {
        //        return ex.ToString();
        //    }
        //}

        //[McpServerTool(Name = "execute_debug_sql"), Description(
        //    "指定されたSQLクエリを実行し、結果を返します。SELECT文のみ実行可能です。" +
        //    "このツールを使用するためには、予め start_debugging でアプリケーションが実行開始されている必要がある。")]
        //public static async Task<string> ExecuteDebugSql([Description("実行するSQLクエリ")] string sql) {
        //    try {
        //        using var httpClient = new HttpClient() {
        //            BaseAddress = new Uri(DOTNET_URL),
        //        };

        //        try {
        //            var requestBody = JsonSerializer.Serialize(new { sql });
        //            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        //            var response = await httpClient.PostAsync("/api/debug-info/execute-sql", content);

        //            var responseBody = await response.Content.ReadAsStringAsync();

        //            if (!response.IsSuccessStatusCode) {
        //                return $"SQLの実行に失敗しました (HTTP {(int)response.StatusCode}): {responseBody}";
        //            }

        //            // 成功時はJSONをそのまま返す
        //            return responseBody;

        //        } catch (HttpRequestException ex) {
        //            return $"APIエンドポイントへの接続に失敗しました。start_debugging でアプリケーションが実行開始されているか確認してください。 Error: {ex.Message}";
        //        } catch (Exception ex) {
        //            return $"予期せぬエラーが発生しました: {ex.Message}";
        //        }
        //    } catch (Exception ex) {
        //        return ex.ToString();
        //    }
        //}
        #endregion アプリケーションテンプレート


        #region スキーマ情報取得
        private const string TOOL_GET_SQLITE_TABLE_SCHEMA = "get_sqlite_table_schema";
        [McpServerTool(Name = TOOL_GET_SQLITE_TABLE_SCHEMA), Description(
            "指定されたSQLiteデータベースファイル内のテーブルスキーマ情報を取得します。")]
        public static string GetSQLiteTableSchema(
            [Description("スキーマ情報を取得するSQLiteデータベースファイルの絶対パス。")] string sqliteDbFilePath,
            [Description("スキーマ情報を取得するテーブルの名前。")] string tableName) {
            try {
                if (string.IsNullOrEmpty(sqliteDbFilePath)) {
                    return "SQLiteデータベースファイルのパスを指定してください。";
                }
                if (string.IsNullOrEmpty(tableName)) {
                    return "テーブル名を指定してください。";
                }
                if (!File.Exists(sqliteDbFilePath)) {
                    return $"指定されたSQLiteデータベースファイルが見つかりません: {sqliteDbFilePath}";
                }

                var sqlSafeTableName = tableName.Replace("'", "''"); // SQLインジェクション対策のためシングルクートをエスケープ

                var startInfo = new ProcessStartInfo {
                    FileName = SQLITE_EXE_PATH,
                    Arguments = $"-header -csv \"{sqliteDbFilePath}\" \"PRAGMA table_info('{sqlSafeTableName}');\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                };

                using var process = new Process { StartInfo = startInfo };
                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (sender, args) => {
                    if (args.Data != null) {
                        output.AppendLine(args.Data);
                    }
                };
                process.ErrorDataReceived += (sender, args) => {
                    if (args.Data != null) {
                        error.AppendLine(args.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (!process.WaitForExit(1000 * 10)) { // 10秒のタイムアウト
                    try {
                        process.Kill(true);
                    } catch { /* 無視 */ }
                    return "sqlite3.exe の実行がタイムアウトしました。";
                }

                if (process.ExitCode != 0) {
                    return $"sqlite3.exe の実行に失敗しました。ExitCode: {process.ExitCode}{Environment.NewLine}Error: {error.ToString().Trim()}{Environment.NewLine}Output: {output.ToString().Trim()}";
                }

                var csvOutput = output.ToString().Trim();
                if (string.IsNullOrWhiteSpace(csvOutput)) {
                    return $"テーブル '{sqlSafeTableName}' が存在しないか、スキーマ情報を取得できませんでした。sqlite3からの出力はありません。";
                }

                var lines = csvOutput.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 2) {
                    return $"スキーマ情報のパースに失敗しました。予期しないCSV形式です: {csvOutput}";
                }

                var headers = lines[0].Split(',');
                var resultList = new List<Dictionary<string, object?>>();

                for (int i = 1; i < lines.Length; i++) {
                    var values = lines[i].Split(',');
                    if (values.Length != headers.Length) continue;

                    var entry = new Dictionary<string, object?>();
                    for (int j = 0; j < headers.Length; j++) {
                        var key = headers[j].Trim();
                        var value = values[j].Trim();

                        if (key == "cid" || key == "notnull" || key == "pk") {
                            if (int.TryParse(value, out int intValue)) {
                                entry[key] = intValue;
                            } else {
                                entry[key] = value;
                            }
                        } else if (string.Equals(value, "NULL", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(value)) {
                            entry[key] = null;
                        } else {
                            entry[key] = value;
                        }
                    }
                    resultList.Add(entry);
                }

                return JsonSerializer.Serialize(resultList, new JsonSerializerOptions { WriteIndented = true });

            } catch (Exception ex) {
                return ex.ToString();
            }
        }
        #endregion スキーマ情報取得


        #region 全テーブル名取得
        private const string TOOL_GET_SQLITE_ALL_TABLE_NAMES = "get_sqlite_all_table_names";
        [McpServerTool(Name = TOOL_GET_SQLITE_ALL_TABLE_NAMES), Description(
            "指定されたSQLiteデータベースファイル内のすべてのテーブル名の一覧を取得します。")]
        public static string GetSQLiteAllTableNames(
            [Description("テーブル名一覧を取得するSQLiteデータベースファイルの絶対パス。")] string sqliteDbFilePath) {
            try {
                if (string.IsNullOrEmpty(sqliteDbFilePath)) {
                    return "SQLiteデータベースファイルのパスを指定してください。";
                }
                if (!File.Exists(sqliteDbFilePath)) {
                    return $"指定されたSQLiteデータベースファイルが見つかりません: {sqliteDbFilePath}";
                }

                var startInfo = new ProcessStartInfo {
                    FileName = SQLITE_EXE_PATH,
                    Arguments = $"\"{sqliteDbFilePath}\" \".tables\" -list", // .tables コマンドの方がシンプルで確実性が高い
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                };

                using var process = new Process { StartInfo = startInfo };
                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (sender, args) => {
                    if (args.Data != null) {
                        output.AppendLine(args.Data);
                    }
                };
                process.ErrorDataReceived += (sender, args) => {
                    if (args.Data != null) {
                        error.AppendLine(args.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (!process.WaitForExit(1000 * 10)) { // 10秒のタイムアウト
                    try {
                        process.Kill(true);
                    } catch { /* 無視 */ }
                    return "sqlite3.exe の実行がタイムアウトしました。";
                }

                if (process.ExitCode != 0) {
                    return $"sqlite3.exe の実行に失敗しました。ExitCode: {process.ExitCode}{Environment.NewLine}Error: {error.ToString().Trim()}{Environment.NewLine}Output: {output.ToString().Trim()}";
                }

                var rawOutput = output.ToString().Trim();
                if (string.IsNullOrWhiteSpace(rawOutput) && !string.IsNullOrWhiteSpace(error.ToString().Trim())) {
                    return $"sqlite3.exe の実行中にエラーが発生しました: {error.ToString().Trim()}";
                }

                // .tables の出力はスペース区切りでテーブル名が並ぶことが多いので、適切にSplitする
                // -list をつけてもsqlite3のバージョンや環境によって挙動が変わる可能性があるため、柔軟に対応
                var tableNames = rawOutput.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(name => name.Trim())
                                          .Where(name => !string.IsNullOrEmpty(name) && !name.StartsWith("sqlite_")) // sqlite_ 内部テーブルを除外
                                          .ToList();

                if (!tableNames.Any() && string.IsNullOrWhiteSpace(rawOutput) && process.ExitCode == 0 && string.IsNullOrWhiteSpace(error.ToString().Trim())) {
                    // 正常終了、出力なし、エラーなしなら空のリスト
                }

                return JsonSerializer.Serialize(tableNames, new JsonSerializerOptions { WriteIndented = true });

            } catch (Exception ex) {
                return ex.ToString();
            }
        }
        #endregion 全テーブル名取得
    }
}
