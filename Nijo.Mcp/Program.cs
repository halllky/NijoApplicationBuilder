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


        #region タスク
        private const string TOOL_START_TASK = "start_task";
        private const string TOOL_REPORT_TASK = "report_task";

        private const string NEXT_TASK_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.Mcp.作業指示書";
        private const string PROCESSING_TASK_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.Mcp.作業指示書\進行中";
        private const string PROCESSING_TASK_SUMMARY_FILE = @"SUMMARY.md";
        private const string ORIGINAL_TASK_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.Mcp.作業指示書\進行中\作業指示書原本";

        [McpServerTool(Name = TOOL_START_TASK), Description(
            "現在累積されている指示書のうち最も優先順位が高い指示書に記載されたタスクを開始する。")]
        public static string StartTask() {
            try {
                // 進行中フォルダが無ければ作成
                if (!Directory.Exists(PROCESSING_TASK_DIR)) Directory.CreateDirectory(PROCESSING_TASK_DIR);

                // 進行中のタスクがある場合はその内容を返す
                var nextTask = FindNextTask(withSummary: true);
                if (nextTask != null) {
                    return nextTask;
                }

                // 進行中のタスクが無い場合、次のタスクを用意する。
                // タスクフォルダ直下にあるmdファイルをファイル名の昇順で列挙し、最初にヒットしたものを次のタスクとする。
                var nextTaskMarkdown = Directory
                    .GetFiles(NEXT_TASK_DIR, "*.md")
                    .OrderBy(file => Path.GetFileName(file))
                    .FirstOrDefault();
                if (nextTaskMarkdown == null) {
                    return "現在積み残しとなっているタスクはありません。";
                }

                // 次のタスクを進行中フォルダに移動する。
                // AIエージェントに逐次指示を出すようにするため、以下のルールでファイルを分割する。
                // - mdファイル中に最初に登場する「## （数字2文字）」より前の部分は、作業概要とみなし、summary.mdに出力する。
                // - それ以降の部分は、「## （数字2文字）」ごとにファイルを分割し、それぞれを次のタスクとみなし、「（数字2文字）.md」ファイルに出力する。
                // - 原本を進行中フォルダの中の原本フォルダ内部にコピーする。
                using (var fs = File.OpenRead(nextTaskMarkdown))
                using (var reader = new StreamReader(fs)) {
                    var currentSectionNumber = (string?)null; // nullの場合は、summary.mdに出力する。
                    var currentSectionContents = new StringBuilder();
                    while (true) {
                        var line = reader.ReadLine();
                        var isFileEnd = line == null;

                        var match = Regex.Match(line ?? "", @"^## (\d{2})$");
                        if (isFileEnd || match.Success) {
                            // これまでに読み込んだ内容を「進行中」フォルダ下のmdファイルに出力する。
                            // まだ summary.md に出力していない場合は、その内容を summary.md に出力する。
                            var filename = currentSectionNumber == null
                                ? Path.Combine(PROCESSING_TASK_DIR, PROCESSING_TASK_SUMMARY_FILE)
                                : Path.Combine(PROCESSING_TASK_DIR, $"{currentSectionNumber}.md");

                            File.WriteAllText(filename, currentSectionContents.ToString());

                            if (isFileEnd) break;

                            currentSectionContents.Clear();
                            currentSectionNumber = match.Groups[1].Value;

                        } else {
                            currentSectionContents.AppendLine(line);
                        }
                    }
                }

                // 原本を進行中フォルダの中の原本フォルダ内部にコピーする。
                if (!Directory.Exists(ORIGINAL_TASK_DIR)) Directory.CreateDirectory(ORIGINAL_TASK_DIR);
                File.Move(nextTaskMarkdown, Path.Combine(ORIGINAL_TASK_DIR, Path.GetFileName(nextTaskMarkdown)), overwrite: true);

                return FindNextTask(withSummary: true) ?? throw new InvalidOperationException("次のタスクが見つかりません。");

            } catch (Exception ex) {
                return ex.ToString();
            }
        }

        [McpServerTool(Name = TOOL_REPORT_TASK), Description(
            "現在のタスクの進捗状況を報告する。")]
        public static string ReportTask(
            [Description("タスクの進捗結果。完了なら'0'を、作業未完了なら'1'を指定してください。")] string result,
            [Description("タスクの進捗状況の詳細")] string detail) {

            try {
                // 進行中フォルダが無ければ作成
                if (!Directory.Exists(PROCESSING_TASK_DIR)) Directory.CreateDirectory(PROCESSING_TASK_DIR);

                // 進行中のステップ番号を取得。
                // 進行中のステップ番号が無い場合は、どの作業についての完了報告かを特定できないので、エラーとする。
                var currentStepFileName = Directory
                    .GetFiles(PROCESSING_TASK_DIR)
                    .Where(file => Regex.IsMatch(Path.GetFileName(file), @"^\d{2}\.md$"))
                    .OrderBy(file => Path.GetFileName(file))
                    .FirstOrDefault();
                if (currentStepFileName == null) {
                    return "現在進行中の作業はありません。";
                }

                // 完了フォルダを用意する。完了フォルダはタスクフォルダ直下の、作業指示書原本の名を冠したフォルダ。
                var originalTaskFullName = Directory
                    .GetFiles(ORIGINAL_TASK_DIR)
                    .FirstOrDefault()
                    ?? throw new InvalidOperationException("作業指示書原本が見つかりません。");
                var completedTaskDir = Path.Combine(NEXT_TASK_DIR, Path.GetFileNameWithoutExtension(originalTaskFullName));
                if (!Directory.Exists(completedTaskDir)) Directory.CreateDirectory(completedTaskDir);

                // 入力チェック
                if (result != "0" && result != "1") {
                    return $$"""
                        タスクの進捗結果は'0'か'1'のみ指定できます。
                        完了なら'0'を、作業未完了なら'1'を指定してください。
                        """;
                }

                // 完了の場合
                if (result == "0") {
                    // - 進行中フォルダにある最初の「（数字2文字）.md」ファイルを完了フォルダに移動する。
                    // - 進捗の詳細を完了フォルダの「（数字2文字）.COMPLETED.md」ファイルに出力する。
                    // - 「（数字2文字）.INCOMPLETE.md」ファイルは削除する。
                    var completedStepFilePath = Path.Combine(completedTaskDir, $"{Path.GetFileNameWithoutExtension(currentStepFileName)}.COMPLETED.md");
                    File.Move(currentStepFileName, completedStepFilePath, overwrite: true);
                    File.WriteAllText(completedStepFilePath, detail);
                    File.Delete(Path.Combine(PROCESSING_TASK_DIR, $"{Path.GetFileNameWithoutExtension(currentStepFileName)}.INCOMPLETE.md"));

                    // 次のステップを探す
                    var nextTask = FindNextTask(withSummary: false);
                    if (nextTask != null) {
                        return nextTask;
                    }

                    // 全ての作業が完了したら作業指示書原本と summary.md を完了フォルダに移動させて作業完了
                    var summaryFilePath = Path.Combine(PROCESSING_TASK_DIR, PROCESSING_TASK_SUMMARY_FILE);
                    var completedSummaryFilePath = Path.Combine(completedTaskDir, PROCESSING_TASK_SUMMARY_FILE);
                    File.Move(summaryFilePath, completedSummaryFilePath, overwrite: true);
                    File.Move(originalTaskFullName, Path.Combine(completedTaskDir, Path.GetFileName(originalTaskFullName)), overwrite: true);

                    return "タスクは完了です。";
                }

                // 作業未完了の場合
                else {
                    // - 進行中フォルダの「（数字2文字）.md」ファイルはそのまま。
                    // - 進捗の詳細を進行中フォルダの「（数字2文字）.INCOMPLETE.md」ファイルに出力する。
                    var incompleteStepFilePath = Path.Combine(PROCESSING_TASK_DIR, $"{Path.GetFileNameWithoutExtension(currentStepFileName)}.INCOMPLETE.md");
                    File.WriteAllText(incompleteStepFilePath, detail);

                    return "タスクの遂行を中断します。";
                }

            } catch (Exception ex) {
                return ex.ToString();
            }
        }

        /// <summary>
        /// 進行中フォルダにファイルがある場合、以下を結合した文字列を返す。無い場合はnullを返す。
        /// - 作業の目的、概要（進行中フォルダにあるファイルのうち、SUMMARY.md の内容）
        /// - 次のタスク（進行中フォルダにある「（数字2文字）.md」という名称規則に合致するファイルのうち、ファイル名昇順で最初のもの）
        /// </summary>
        private static string? FindNextTask(bool withSummary) {
            // 次のタスクを取得
            var nextTask = Directory
                .GetFiles(PROCESSING_TASK_DIR)
                .Where(file => Regex.IsMatch(Path.GetFileName(file), @"^\d{2}\.md$"))
                .OrderBy(file => Path.GetFileName(file))
                .FirstOrDefault();

            // 次のタスクが無い
            if (nextTask == null) {
                return null;
            }

            // 次のタスクの内容を取得
            var nextTaskContents = File.ReadAllText(nextTask);

            // 「（数字2文字）.INCOMPLETE.md」ファイルがある場合は、その内容を追加する
            var incompleteStepFilePath = Path.Combine(PROCESSING_TASK_DIR, $"{Path.GetFileNameWithoutExtension(nextTask)}.INCOMPLETE.md");
            if (File.Exists(incompleteStepFilePath)) {
                nextTaskContents += $$"""
                    -------
                    なお、このタスクは以前別のエージェントが遂行しましたが、完了していません。
                    その際の遂行結果は以下のように報告されています:

                    > {{File.ReadAllText(incompleteStepFilePath)}}
                    """;
            }

            var summaryFilePath = Path.Combine(PROCESSING_TASK_DIR, PROCESSING_TASK_SUMMARY_FILE);
            var summary = withSummary && File.Exists(summaryFilePath)
                ? File.ReadAllText(summaryFilePath)
                : string.Empty;

            return $$"""
                {{(string.IsNullOrEmpty(summary) ? $$"""

                """ : $$"""
                {{summary}}
                -------

                次のタスク:

                """)}}
                {{nextTaskContents}}

                -------
                作業が完了した場合、または作業に行き詰った場合、 "{{TOOL_REPORT_TASK}}" ツールを呼び出してください。
                """;
        }
        #endregion タスク


        //#region 自動テストで作成されたプロジェクト
        //private const string TOOL_START_IMPLEMENTATION_SESSION = "start_implementation_session";
        //private const string TOOL_CONTINUE_IMPLEMENTATION_SESSION = "continue_implementation_session";
        //private const string NEXT_ACTION_FILE = "SESSION_NEXT_ACTION";

        //[McpServerTool(Name = TOOL_START_IMPLEMENTATION_SESSION), Description(
        //    "Nijo.IntegrationTest の DataPatterns フォルダにあるXMLと対応する OverridedApplicationService などの実装を作成するセッションを開始する。")]
        //public static async Task<string> StartImplementationSession() {
        //    using (var workDirectory = WorkDirectory.Prepare()) {
        //        var nextActionFile = Path.Combine(workDirectory.DirectoryPath, NEXT_ACTION_FILE);
        //        if (File.Exists(nextActionFile)) File.Delete(nextActionFile);
        //    }
        //    return await NextImplementationSession();
        //}

        //[McpServerTool(Name = TOOL_CONTINUE_IMPLEMENTATION_SESSION), Description(
        //    $"{TOOL_START_IMPLEMENTATION_SESSION} ツール内の指示により呼び出されるツール。")]
        //public static async Task<string> NextImplementationSession() {
        //    string? nextActionFile = null;
        //    try {
        //        using var workDirectory = WorkDirectory.Prepare();

        //        if (!await 既存デバッグプロセス中止(workDirectory)) {
        //            return workDirectory.WithMainLogContents("既存デバッグプロセス中止に失敗しました。");
        //        }

        //        // このファイルに状態を記録し、AIエージェントに対話的に指示を送る
        //        nextActionFile = Path.Combine(workDirectory.DirectoryPath, NEXT_ACTION_FILE);
        //        var currentPatternFile = Path.Combine(workDirectory.DirectoryPath, "SESSION_CURRENT_PATTERN");

        //        const string NEXT_IMPL_REACT = "::Reactの画面を実装させる::";
        //        const string NEXT_TRY_COMPLIE = "::コンパイルエラーが出なくなるまで修正を繰り返させる::";
        //        const string NEXT_FORWARD = "::次のデータパターンXMLに進む::";

        //        // セッション開始（DataPatternの最初のファイルでセットアップを行なう）
        //        if (!File.Exists(nextActionFile)) {
        //            var nextPatternXmlFullPath = GetNextDataPatternXmlFullPath(null) ?? throw new InvalidOperationException("DataPatternsフォルダにxmlが1個も無い");

        //            File.WriteAllText(nextActionFile, NEXT_IMPL_REACT);
        //            File.WriteAllText(currentPatternFile, nextPatternXmlFullPath);

        //            return await SetupNextPatternAndEchoImplementAppSrv($$"""
        //                下記スキーマ定義それぞれについて、自動生成されたソースを利用したアプリケーションの実装を開始します。
        //                {{string.Join(Environment.NewLine, Directory.GetFiles(DATA_PATTERN_DIR).Select(filename => $$"""
        //                * {{Path.GetFileName(filename)}}
        //                """))}}

        //                以下の指示に従いソースコードの実装を進めてください。

        //                ---------------------------
        //                """, workDirectory, nextPatternXmlFullPath, deleteAll: true);
        //        }

        //        // 1つ前のセッションの指示を読み取る
        //        var nextAction = File.ReadAllText(nextActionFile);
        //        var currentPatternXmlFullPath = File.ReadAllText(currentPatternFile);

        //        switch (nextAction) {
        //            // Reactの実装を促す
        //            case NEXT_IMPL_REACT:
        //                File.WriteAllText(nextActionFile, NEXT_TRY_COMPLIE);
        //                return $$"""
        //                    下記TypeScriptファイル内の関数（React Router のルーティング定義）と、
        //                    当該ルーティングで定義される画面のソースコードを、下記スキーマ定義のデータ構造に合うように実装してください。

        //                    * 実装対象: {{Path.Combine(DATA_PATTERN_REVEALED_DIR, "react", "src", "pages", "index.tsx")}}
        //                    * スキーマ定義: {{Path.Combine(DATA_PATTERN_REVEALED_DIR, "nijo.xml")}}

        //                    実装例は以下を参考にしてください。

        //                    * "{{Path.Combine(DATA_PATTERN_REVEALED_DIR, "react", "src", "pages", "index.tsx")}}" 内部でコメントアウトされたソース
        //                    * "{{Path.Combine(DATA_PATTERN_REVEALED_DIR, "react", "src", "debug-rooms")}}" フォルダ内のサンプル
        //                    * "{{Path.Combine(DATA_PATTERN_REVEALED_DIR, "react", "src", "pages")}}" 直下にある「画面実装例」フォルダの中身（TypeScriptコンパイラに認識させないために拡張子を ".tsx.SAMPLE" にしています）

        //                    実装完了後、 "{{TOOL_CONTINUE_IMPLEMENTATION_SESSION}}" ツールを呼び出してください。
        //                    """;

        //            // コンパイルエラーが出なくなるまで繰り返させる
        //            case NEXT_TRY_COMPLIE:
        //                File.WriteAllText(nextActionFile, NEXT_FORWARD);
        //                return $$"""
        //                    "{{TOOL_CHECK_COMPILE_ERROR}}" ツールを "{{Path.Combine(DATA_PATTERN_REVEALED_DIR, "nijo.xml")}}" で呼び出し、
        //                    あなたがこれまでに実装した C#, TypeScript のソースでコンパイルエラーが出ないかどうかの確認を行なってください。
        //                    ツール実行後、以下いずれかの対応を行なってください。

        //                    * エラーが無かった場合
        //                      * "{{TOOL_CONTINUE_IMPLEMENTATION_SESSION}}" ツールを呼び出してください。
        //                    * エラーがあった場合
        //                      * あなたが実装したソースコードが原因であれば、ソースコードを修正し、再度 "{{TOOL_CHECK_COMPILE_ERROR}}" ツールを呼んでください。
        //                      * 自動生成されたソースコード中に問題があるなど、あなたが実装したソースコードに原因が無い場合、 "{{TOOL_CONTINUE_IMPLEMENTATION_SESSION}}" ツールを呼んでください。
        //                    """;

        //            // AIエージェントが実装した成果を保存し、次のパターンへ進む
        //            case NEXT_FORWARD:

        //                // スナップショットフォルダを決定
        //                var snapshotDir = Path.Combine(DATA_PATTERN_IMPLEMENTORS_DIR, Path.GetFileNameWithoutExtension(currentPatternXmlFullPath));
        //                AIエージェントの実装成果をスナップショットフォルダに保存する(currentPatternXmlFullPath, snapshotDir);

        //                // コンパイラーの結果をログ出力しておく
        //                await コンパイルエラーチェック(workDirectory, DATA_PATTERN_REVEALED_DIR);
        //                File.Copy(
        //                    workDirectory.MainLogFullPath,
        //                    Path.Combine(snapshotDir, "build.log"));

        //                var next = GetNextDataPatternXmlFullPath(currentPatternXmlFullPath);
        //                if (next != null) {
        //                    File.WriteAllText(nextActionFile, NEXT_IMPL_REACT);
        //                    File.WriteAllText(currentPatternFile, next);

        //                    return await SetupNextPatternAndEchoImplementAppSrv($$"""
        //                        スキーマ定義 "{{Path.GetFileName(currentPatternXmlFullPath)}}" の実装を完了しました。続いて "{{Path.GetFileName(next)}}" の実装に移ります。
        //                        """, workDirectory, next, deleteAll: false);

        //                } else {
        //                    return $$"""
        //                        タスクは完了です。
        //                        """;
        //                }

        //            default:
        //                throw new InvalidOperationException($"不正な状態: {nextAction}");
        //        }

        //        // ファイルコピー => ソースコード自動生成かけなおし => アプリケーションサービスクラスの実装を指示
        //        static async Task<string> SetupNextPatternAndEchoImplementAppSrv(
        //            string messagePrefix,
        //            WorkDirectory workDirectory,
        //            string nextPatternXmlFullPath,
        //            bool deleteAll) {
        //            if (!await アプリケーションテンプレートを自動テストで作成されたプロジェクトにコピーする(workDirectory, nextPatternXmlFullPath, deleteAll)) {
        //                return workDirectory.WithMainLogContents("アプリケーションテンプレートの「自動テストで作成されたプロジェクト」へのコピーに失敗しました。");
        //            }
        //            if (!await ソースコード自動生成かけなおし(workDirectory, DATA_PATTERN_REVEALED_DIR)) {
        //                return workDirectory.WithMainLogContents("ソースコードの自動生成に失敗しました。");
        //            }
        //            return $$"""
        //                {{messagePrefix}}

        //                下記C#ファイル内のクラス "OverridedApplicationService" は "AutoGeneratedApplicationService" を継承します。
        //                基底クラスではいくつかのabstractメソッドが宣言されているため、具象クラス側でそれらをオーバライドして実装する必要があります。
        //                各種abstractメソッドをオーバライドし、下記スキーマ定義のデータ構造に合うように実装してください。
        //                実装例は当該C#ファイル内のコメントを参照してください。

        //                * 実装対象: {{Path.Combine(DATA_PATTERN_REVEALED_DIR, "Core", "OverridedApplicationService.cs")}}
        //                * スキーマ定義: {{Path.Combine(DATA_PATTERN_REVEALED_DIR, "nijo.xml")}}

        //                実装完了後、 "{{TOOL_CONTINUE_IMPLEMENTATION_SESSION}}" ツールを呼び出してください。
        //                """;
        //        }

        //    } catch (Exception ex) {
        //        if (nextActionFile != null && File.Exists(nextActionFile)) File.Delete(nextActionFile);
        //        return ex.ToString();
        //    }
        //}
        //#endregion 自動テストで作成されたプロジェクト
    }
}
