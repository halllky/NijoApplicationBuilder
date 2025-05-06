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


        #region NUnit
        private const string TOOL_HOW_TO_RUN_TEST = "how_to_run_test";
        [McpServerTool(Name = TOOL_HOW_TO_RUN_TEST), Description(
            $"nijo.slnに含まれるすべてのユニットテストを列挙し、 {TOOL_RUN_TEST} ツールのフィルターの使用例を示します。")]
        public static async Task<string> HowToRunTest() {
            try {
                using var workDirectory = WorkDirectory.Prepare();
                workDirectory.WriteToMainLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [NijoMcpTools.HowToRunTest] HowToRunTest method called.");

                // cmdファイルを作成してテスト一覧を取得
                var cmdFilePath = Path.Combine(workDirectory.DirectoryPath, "list_tests.cmd");
                var testListOutputFile = Path.Combine(workDirectory.DirectoryPath, "test_list_output.txt");
                RenderCmdFile(cmdFilePath, $$"""
                    chcp 65001
                    @echo off
                    dotnet test "{{NIJO_SLN}}" --list-tests > "{{testListOutputFile}}" 2>&1
                    exit /b %errorlevel%
                    """);

                var exitCode = await ExecuteProcess("cmd-list-tests", startInfo => {
                    startInfo.FileName = "cmd.exe";
                    startInfo.ArgumentList.Add("/c");
                    startInfo.ArgumentList.Add(cmdFilePath);
                }, workDirectory, TimeSpan.FromMinutes(5));

                // テスト一覧ファイルが存在するか確認
                if (!File.Exists(testListOutputFile)) {
                    return workDirectory.WithMainLogContents("テスト一覧結果ファイルが見つかりません。");
                }
                // テスト一覧ファイルの内容を読み取る
                var testListOutput = File.ReadAllText(testListOutputFile);


                if (exitCode != 0) {
                    return $"ユニットテストの列挙に失敗しました。\n\n{testListOutput}";
                }

                // ログの内容から "--list-tests" の実際の出力部分を抽出（より堅牢な方法が必要な場合あり）。
                // この文字が見つからない場合はログの内容をそのまま表示する。
                const string LIST_TESTS_MARKER = "The following Tests are available:";
                var listTestsIndex = testListOutput.IndexOf(LIST_TESTS_MARKER);
                var actualTestList = listTestsIndex >= 0
                    ? testListOutput.Substring(listTestsIndex)
                    : testListOutput;

                return $$"""
                    ユニットテストの列挙とフィルターの使用例:

                    {{actualTestList}}

                    --- run_test フィルターの使用例 (NUnit) ---

                    フィルター式は `<Property><Operator><Value>[|&<Expression>]` の形式です。
                    演算子:
                      `=`   : 完全一致
                      `!=`  : 完全一致ではない
                      `~`   : 含む
                      `!~`  : 含まない

                    プロパティ (NUnitの場合):
                      - FullyQualifiedName: 名前空間を含む完全なテスト名 (例: NUnitNamespace.UnitTest1.TestMethod1)
                      - Name: テストメソッド名 (例: TestMethod1)
                      - TestCategory: [Category("...")] 属性 (例: CategoryA)
                      - Priority: [Priority(X)] 属性 (例: 2)

                    使用例:
                      - 特定のテストメソッドを実行: `run_test --testFilter "FullyQualifiedName=NUnitNamespace.UnitTest1.TestMethod1"`
                      - 名前に "TestMethod" を含むテストを実行: `run_test --testFilter "Name~TestMethod"`
                      - 特定のクラス内のすべてのテストを実行: `run_test --testFilter "FullyQualifiedName~NUnitNamespace.UnitTest1"`
                      - 特定のテストを除外: `run_test --testFilter "FullyQualifiedName!=NUnitNamespace.UnitTest1.TestMethod1"`
                      - 特定のカテゴリのテストを実行: `run_test --testFilter "TestCategory=CategoryA"`
                      - 複数の条件 (OR): `run_test --testFilter "TestCategory=CategoryA|Priority=1"`
                      - 複数の条件 (AND): `run_test --testFilter "FullyQualifiedName~UnitTest1&TestCategory=CategoryA"`
                      - 複雑な条件: `run_test --testFilter "(FullyQualifiedName~UnitTest1&TestCategory=CategoryA)|Priority=1"`

                    詳細: https://learn.microsoft.com/ja-jp/dotnet/core/testing/selective-unit-tests?pivots=nunit
                    """;

            } catch (Exception ex) {
                return ex.ToString();
            }
        }

        private const string TOOL_RUN_TEST = "run_test";
        [McpServerTool(Name = TOOL_RUN_TEST), Description(
            $"指定されたユニットテストを実行し、結果を返します。フィルターの指定方法は {TOOL_HOW_TO_RUN_TEST} を実行して確認してください。")]
        public static async Task<string> RunTest([Description("実行するテストの名前またはフィルター式")] string testFilter) {
            try {
                if (string.IsNullOrEmpty(testFilter)) {
                    return "実行するテストの名前またはフィルター式を指定してください。";
                }

                using var workDirectory = WorkDirectory.Prepare();
                workDirectory.WriteToMainLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [NijoMcpTools.RunTest] RunTest method called with filter: {testFilter}");

                // cmdファイルを作成
                var cmdFilePath = Path.Combine(workDirectory.DirectoryPath, "run_test.cmd");
                var testOutputFile = Path.Combine(workDirectory.DirectoryPath, "test_output.txt");
                RenderCmdFile(cmdFilePath, $$"""
                    chcp 65001
                    @echo off
                    dotnet test "{{NIJO_SLN}}" --filter "{{testFilter}}" -v normal > "{{testOutputFile}}" 2>&1
                    exit /b %errorlevel%
                    """);

                // cmdファイルを実行
                var exitCode = await ExecuteProcess("cmd-run-test", startInfo => {
                    startInfo.FileName = "cmd.exe";
                    startInfo.ArgumentList.Add("/c");
                    startInfo.ArgumentList.Add(cmdFilePath);
                }, workDirectory, TimeSpan.FromMinutes(10));

                // テスト結果ファイルが存在するか確認
                if (!File.Exists(testOutputFile)) {
                    return workDirectory.WithMainLogContents("テスト結果ファイルが見つかりません。");
                }

                // テスト結果ファイルの内容を読み取る
                var testOutput = File.ReadAllText(testOutputFile);

                if (exitCode != 0) {
                    return $"ユニットテストの実行に失敗しました。\n\n{testOutput}";
                }

                return $"ユニットテストの実行が完了しました。\n\n{testOutput}";
            } catch (Exception ex) {
                return ex.ToString();
            }
        }
        #endregion NUnit


        #region タスク
        private const string TOOL_START_TASK = "start_task";
        private const string TOOL_REPORT_TASK = "report_task";

        private const string NEXT_TASK_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\タスク";
        private const string PROCESSING_TASK_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\タスク\進行中";
        private const string PROCESSING_TASK_SUMMARY_FILE = @"SUMMARY.md";
        private const string COMPLETED_TASK_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\タスク\完了";

        [McpServerTool(Name = TOOL_START_TASK), Description(
            "現在累積されている指示書のうち最も優先順位が高い指示書に記載されたタスクを開始する。")]
        public static string StartTask() {
            try {
                // 進行中フォルダ、完了フォルダが無ければ作成
                if (!Directory.Exists(PROCESSING_TASK_DIR)) Directory.CreateDirectory(PROCESSING_TASK_DIR);
                if (!Directory.Exists(COMPLETED_TASK_DIR)) Directory.CreateDirectory(COMPLETED_TASK_DIR);

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
                using var fs = File.OpenRead(nextTaskMarkdown);
                using var reader = new StreamReader(fs);
                var currentSectionNumber = (string?)null; // nullの場合は、summary.mdに出力する。
                var currentSectionContents = new StringBuilder();
                string? line;
                while ((line = reader.ReadLine()) != null) {
                    var match = Regex.Match(line, @"^## (\d{2})$");
                    if (match.Success) {
                        // これまでに読み込んだ内容を「進行中」フォルダ下のmdファイルに出力する。
                        // まだ summary.md に出力していない場合は、その内容を summary.md に出力する。
                        var filename = currentSectionNumber == null
                            ? Path.Combine(PROCESSING_TASK_DIR, PROCESSING_TASK_SUMMARY_FILE)
                            : Path.Combine(PROCESSING_TASK_DIR, $"{currentSectionNumber}.md");

                        File.WriteAllText(filename, currentSectionContents.ToString());
                        currentSectionContents.Clear();
                        currentSectionNumber = match.Groups[1].Value;

                    } else {
                        currentSectionContents.AppendLine(line);
                    }
                }

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
                // 進行中フォルダ、完了フォルダが無ければ作成
                if (!Directory.Exists(PROCESSING_TASK_DIR)) Directory.CreateDirectory(PROCESSING_TASK_DIR);
                if (!Directory.Exists(COMPLETED_TASK_DIR)) Directory.CreateDirectory(COMPLETED_TASK_DIR);

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
                    var completedStepFilePath = Path.Combine(COMPLETED_TASK_DIR, $"{Path.GetFileNameWithoutExtension(currentStepFileName)}.COMPLETED.md");
                    File.Move(currentStepFileName, completedStepFilePath, overwrite: true);
                    File.WriteAllText(completedStepFilePath, detail);
                    File.Delete(Path.Combine(PROCESSING_TASK_DIR, $"{Path.GetFileNameWithoutExtension(currentStepFileName)}.INCOMPLETE.md"));

                    // 次のステップを探す。無ければタスク完了
                    var nextTask = FindNextTask(withSummary: false);
                    if (nextTask != null) {
                        return nextTask;
                    }

                    // summary.md を完了フォルダに移動させて作業完了
                    var summaryFilePath = Path.Combine(PROCESSING_TASK_DIR, PROCESSING_TASK_SUMMARY_FILE);
                    var completedSummaryFilePath = Path.Combine(COMPLETED_TASK_DIR, PROCESSING_TASK_SUMMARY_FILE);
                    File.Move(summaryFilePath, completedSummaryFilePath, overwrite: true);

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
