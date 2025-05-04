using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Text;
using Nijo.SchemaParsing;
using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.DataModelModules;
using Nijo.Models.QueryModelModules;
using Nijo.CodeGenerating.Helpers;
using Nijo.Models;

namespace Nijo.IntegrationTest;

/// <summary>
/// DataPatternsフォルダ内のXMLファイルを使っていろいろ確認するテスト
/// </summary>
[TestFixture]
[NonParallelizable]
public class DataPatternTest {
    private const string TEST_PROJECT_DIR = "自動テストで作成されたプロジェクト";
    private readonly ILogger _logger = new ConsoleLogger();

    /// <summary>
    /// DataPatternsフォルダ内のXMLファイルパスを取得するためのTestCaseSource
    /// </summary>
    /// <returns>XMLファイルパスのリスト</returns>
    public static IEnumerable<string> GetXmlFilePaths() {
        string dataPatternDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "DataPatterns");
        dataPatternDir = Path.GetFullPath(dataPatternDir);

        var files = Directory
            .GetFiles(dataPatternDir, "*.xml")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .OrderBy(f => f)
            .AsEnumerable();

        // 環境変数からテストケースが指定されている場合は、そのファイルのみを返す
        var testCase = Environment.GetEnvironmentVariable("TEST_CASE");
        if (!string.IsNullOrEmpty(testCase)) {
            files = files.Where(f => f == testCase);
        }

        return files;
    }

    [OneTimeSetUp]
    public void Setup() {
        // テストプロジェクトディレクトリの準備
        var workspaceRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", ".."));
        var testProjectDir = Path.Combine(workspaceRoot, "..", TEST_PROJECT_DIR);
        var templateProjectDir = Path.Combine(workspaceRoot, "..", "Nijo.ApplicationTemplate.Ver1");

        // 既存のテストプロジェクトを削除
        if (Directory.Exists(testProjectDir)) {
            foreach (var file in Directory.GetFiles(testProjectDir)) {
                File.Delete(file);
            }
            foreach (var dir in Directory.GetDirectories(testProjectDir)) {
                Directory.Delete(dir, true);
            }
        }

        // テンプレートプロジェクトをコピー
        CopyDirectory(templateProjectDir, testProjectDir);

        // テンプレートプロジェクトでしか使わないソースを削除（ApplicationService）
        File.WriteAllText(Path.Combine(testProjectDir, "Core", "OverridedApplicationService.cs"), $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;

            namespace MyApp.Core;

            partial class OverridedApplicationService {
                // 実装なし
            }
            """.Replace("\r\n", "\n"), new UTF8Encoding(false, false));

        // テンプレートプロジェクトでしか使わないソースを削除（ユニットテスト）
        Directory.Delete(Path.Combine(testProjectDir, "Test", "Tests"), true);

        // テンプレートプロジェクトでしか使わないソースを削除（画面）
        foreach (var dir in Directory.GetDirectories(Path.Combine(testProjectDir, "react", "src", "pages"))) {
            Directory.Delete(dir, true);
        }
        // テンプレートプロジェクトでしか使わないソースを削除（ルーティング。pages/index.tsx）
        File.WriteAllText(Path.Combine(testProjectDir, "react", "src", "pages", "index.tsx"), $$"""
            /** 画面のルーティング設定 */
            export default function (): never[] {
              return []
            }
            """.Replace("\r\n", "\n"), new UTF8Encoding(false, false));
    }

    /// <summary>
    /// コード自動生成に使われる各種メソッドの動作確認の目検用ファイルのダンプ
    /// </summary>
    /// <param name="xmlFileName">XMLファイル名</param>
    [Test]
    [TestCaseSource(nameof(GetXmlFilePaths))]
    [Category("DataPattern")]
    public void 各種中間出力ダンプ(string fileName) {
        var workspaceRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", ".."));
        var testProjectDir = Path.Combine(workspaceRoot, "..", TEST_PROJECT_DIR);
        var dataPatternsDir = Path.Combine(workspaceRoot, "DataPatterns");
        var targetXmlPath = Path.Combine(testProjectDir, "nijo.xml");
        var sourceXmlPath = Path.Combine(dataPatternsDir, $"{fileName}.xml");

        // XMLファイルをコピー
        File.Copy(sourceXmlPath, targetXmlPath, true);

        if (!GeneratedProject.TryOpen(testProjectDir, out var project, out var error)) {
            Assert.Fail($"プロジェクトフォルダを開くのに失敗しました: {error}");
            return;
        }
        var schemaXml = XDocument.Load(project.SchemaXmlPath);
        var parseContext = new SchemaParseContext(schemaXml, SchemaParseRule.Default());

        // TryBuildSchemaメソッドを使用してApplicationSchemaのインスタンスを生成
        if (!parseContext.TryBuildSchema(schemaXml, out var appSchema, _logger)) {
            Assert.Fail("スキーマのビルドに失敗したため、ダンプを生成できませんでした。");
            return;
        }

        // ダンプ出力先ファイルを決める
        var logDirRoot = Path.Combine(workspaceRoot, "..", "Nijo.IntegrationTest.Log");
        if (!Directory.Exists(logDirRoot)) Directory.CreateDirectory(logDirRoot);

        // ダンプファイルに出力
        var logDirOfThisTest = Path.Combine(logDirRoot, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        if (!Directory.Exists(logDirOfThisTest)) Directory.CreateDirectory(logDirOfThisTest);
        using (var logWriterOfThisTest = new StreamWriter(Path.Combine(logDirOfThisTest, $"{fileName}.md"), append: false, encoding: Encoding.UTF8)) {
            var dump = appSchema.GenerateMarkdownDump();

            logWriterOfThisTest.WriteLine(dump);
        }

        Assert.Pass($"{fileName} のテストが完了しました");
    }

    [Test]
    [TestCaseSource(nameof(GetXmlFilePaths))]
    [Category("DataPattern")]
    public void 各構造体のオブジェクトパスが正しく生成されるか確認(string fileName) {
        var implementor = GetImplementor(fileName)
            ?? throw new InvalidOperationException(
                $"'{fileName}' の確認用クラスが定義されていません。" +
                $"Implementorsフォルダにこのパターンと対応するクラスが用意されているか確認してください。");

        var workspaceRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", ".."));
        var dataPatternsDir = Path.Combine(workspaceRoot, "DataPatterns");
        var sourceXmlPath = Path.Combine(dataPatternsDir, $"{fileName}.xml");
        var schemaXml = XDocument.Load(sourceXmlPath);
        var parseContext = new SchemaParseContext(schemaXml, SchemaParseRule.Default());

        // TryBuildSchemaメソッドを使用してApplicationSchemaのインスタンスを生成
        if (!parseContext.TryBuildSchema(schemaXml, out var appSchema, _logger)) {
            Assert.Fail("スキーマのビルドに失敗したため、ダンプを生成できませんでした。");
            return;
        }

        // パターンごとに期待結果と突合する
        Assert.Multiple(() => {
            // SearchResult
            var allMembers = appSchema
                .GetRootAggregates()
                .Where(root => root.Model is QueryModel
                            || root.Model is DataModel && root.GenerateDefaultQueryModel)
                .Select(root => new Variable(root.PhysicalName, new SearchResult(root)))
                .SelectMany(variable => variable.CreatePropertiesRecursively())
                .ToArray();
            implementor.AssertSearchResultMemberPath(allMembers);

            // 調査用
            TestContext.Out.WriteLine($$"""
                実際の全メンバーパス:
                {{allMembers.SelectTextTemplate(m => $$"""
                {{m.GetJoinedPathFromInstance(E_CsTs.CSharp)}}
                """)}}
                """);
        });

        Assert.Pass($"{fileName} のテストが完了しました");
    }

    /// <summary>
    /// XMLファイルごとのテスト
    /// </summary>
    /// <param name="xmlFileName">XMLファイル名</param>
    [Test]
    [TestCaseSource(nameof(GetXmlFilePaths))]
    [Category("DataPattern")]
    public async Task コンパイルエラーチェック(string fileName) {
        var workspaceRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", ".."));
        var testProjectDir = Path.Combine(workspaceRoot, "..", TEST_PROJECT_DIR);
        var dataPatternsDir = Path.Combine(workspaceRoot, "DataPatterns");
        var targetXmlPath = Path.Combine(testProjectDir, "nijo.xml");
        var sourceXmlPath = Path.Combine(dataPatternsDir, $"{fileName}.xml");

        Console.WriteLine($"テスト実行: {fileName}");

        // XMLファイルをコピー
        File.Copy(sourceXmlPath, targetXmlPath, true);

        // ソースコード自動生成を実行
        if (!GeneratedProject.TryOpen(testProjectDir, out var project, out var error)) {
            Assert.Fail($"プロジェクトフォルダを開くのに失敗しました: {error}");
            return;
        }
        var schemaXml = XDocument.Load(project.SchemaXmlPath);
        var parseContext = new SchemaParseContext(schemaXml, SchemaParseRule.Default());

        if (!project.ValidateSchema(parseContext, _logger)) {
            Assert.Fail($"スキーマ定義の検証に失敗しました。");
        }
        try {
            project.GenerateCode(parseContext, new() {
                AllowNotImplemented = true,
            }, _logger);
        } catch (Exception ex) {
            Assert.Fail($"ソースコード自動生成に失敗しました。\n{ex}");
        }

        //// OverridedApplicationServiceの実装
        //var implementor = GetImplementor(fileName);
        //if (implementor != null) {
        //    var implementation = implementor.GetImplementation(schemaXml);
        //    var servicePath = Path.Combine(testProjectDir, "Core", "OverridedApplicationService.cs");
        //    File.WriteAllText(servicePath, implementation);
        //}

        // -------------------------------
        // コンパイラーチェック実行。dotnet build と tsc -b --noEmit で判断する。
        var logDirRoot = Path.Combine(workspaceRoot, "..", "Nijo.IntegrationTest.Log");
        var csharpCmd = Path.Combine(logDirRoot, "csharp_compile_check.cmd");
        var typeScriptCmd = Path.Combine(logDirRoot, "typescript_compile_check.cmd");

        RenderCmdFile(csharpCmd, $$"""
            chcp 65001
            @echo off
            setlocal
            set NO_COLOR=true

            @echo.
            @echo *** C#コンパイルエラーチェック ***

            dotnet build -c Debug 2>&1

            exit /b %errorlevel%
            """);

        RenderCmdFile(typeScriptCmd, $$"""
            chcp 65001
            @echo off
            setlocal
            set NO_COLOR=true

            @echo.
            @echo *** TypeScriptコンパイルエラーチェック ***

            call tsc -b --noEmit 2>&1

            exit /b %errorlevel%
            """);

        var csharpExitCode = await ExecuteProcess("dotnet build", startInfo => {
            startInfo.WorkingDirectory = project.ProjectRoot;
            startInfo.FileName = "cmd";
            startInfo.Arguments = $"/c \"{csharpCmd}\"";
            startInfo.UseShellExecute = false;
        }, TimeSpan.FromSeconds(25));

        var typeScriptExitCode = await ExecuteProcess("tsc -b --noEmit", startInfo => {
            startInfo.WorkingDirectory = project.ReactProjectRoot;
            startInfo.FileName = "cmd";
            startInfo.Arguments = $"/c \"{typeScriptCmd}\"";
            startInfo.UseShellExecute = false;
        }, TimeSpan.FromSeconds(25));

        if (csharpExitCode == 0 && typeScriptExitCode == 0) {
            Assert.Pass($"{fileName} のテストが完了しました");

        } else {
            Assert.Fail($"コンパイルエラーが発生しました。");
        }

        // -------------------------------
        // ユーティリティ

        // .cmd ファイルを、文字コードや行末処理を加えたうえで出力する。
        static void RenderCmdFile(string cmdFilePath, string cmdFileContent) {
            File.WriteAllText(
                cmdFilePath,
                // cmd処理中にchcpしたときは各行の改行コードの前にスペースが無いと上手く動かないので
                cmdFileContent.ReplaceLineEndings(" \r\n"),
                new UTF8Encoding(false, false));
        }

        // プロセス実行
        static async Task<int> ExecuteProcess(string logName, Action<ProcessStartInfo> editStartInfo, TimeSpan timeout) {
            using var process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true; // 標準出力をリダイレクト
            process.StartInfo.RedirectStandardError = true;  // 標準エラーをリダイレクト
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            editStartInfo(process.StartInfo);

            process.OutputDataReceived += (sender, e) => {
                try {
                    if (e.Data != null) {
                        TestContext.Out.WriteLine($"[{logName} stdout] {e.Data}");
                    } else {
                        TestContext.Out.WriteLine($"[{logName} event] OutputDataReceived: Stream closed (Data is null).");
                    }
                } catch (InvalidOperationException ioex) {
                    // プロセス終了時などにストリームが閉じられてこの例外が発生することがあるため、無視する
                    TestContext.Out.WriteLine($"[{logName} event] Caught InvalidOperationException in OutputDataReceived (likely harmless): {ioex.Message}");
                } catch (Exception ex) {
                    TestContext.Out.WriteLine($"[{logName} event] EXCEPTION in OutputDataReceived: {ex.ToString()}");
                }
            };
            process.ErrorDataReceived += (sender, e) => {
                try {
                    if (e.Data != null) {
                        TestContext.Out.WriteLine($"[{logName} stderr] {e.Data}");
                    } else {
                        TestContext.Out.WriteLine($"[{logName} event] ErrorDataReceived: Stream closed (Data is null).");
                    }
                } catch (InvalidOperationException ioex) {
                    // プロセス終了時などにストリームが閉じられてこの例外が発生することがあるため、ログのみ出力して無視する
                    TestContext.Out.WriteLine($"[{logName} event] Caught InvalidOperationException in ErrorDataReceived (likely harmless): {ioex.Message}");
                } catch (Exception ex) {
                    TestContext.Out.WriteLine($"[{logName} event] EXCEPTION in ErrorDataReceived: {ex.ToString()}");
                }
            };

            TestContext.Out.WriteLine($"[{logName}] 開始");

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            var timeoutLimit = DateTime.Now.Add(timeout);
            while (true) {
                if (DateTime.Now > timeoutLimit) {
                    TestContext.Out.WriteLine($"[{logName}] プロセスがタイムアウトしました。");
                    await EnsureKill(process);
                    process.CancelOutputRead();
                    process.CancelErrorRead();
                    return 1;

                } else if (process.HasExited) {
                    TestContext.Out.WriteLine($"[{logName}] 終了（終了コード: {process.ExitCode}）");
                    process.CancelOutputRead();
                    process.CancelErrorRead();
                    return process.ExitCode;

                } else {
                    await Task.Delay(100);
                }
            }
        }

        // タスクキル
        static async Task<string> EnsureKill(Process process) {
            int? pid = null;
            try {
                if (process.HasExited) return "Process is already exited. taskkill is skipped.";

                pid = process.Id;
                // 対象プロセスの情報をログ出力
                Console.Error.WriteLine($"[NijoMcpTools.EnsureKill] Killing process info - PID: {process.Id}, Name: {process.ProcessName}"); // 標準エラーに出力

                var kill = new Process();
                kill.StartInfo.FileName = "taskkill";
                kill.StartInfo.ArgumentList.Add("/PID");
                kill.StartInfo.ArgumentList.Add(pid.ToString()!);
                kill.StartInfo.ArgumentList.Add("/T");
                kill.StartInfo.ArgumentList.Add("/F");
                kill.StartInfo.RedirectStandardOutput = true;
                kill.StartInfo.RedirectStandardError = true;

                kill.Start();
                bool exited = await Task.Run(() => kill.WaitForExit(TimeSpan.FromSeconds(5)));

                if (!exited) {
                    return $"taskkill timed out after 5 seconds (PID = {pid})";
                }

                if (kill.ExitCode == 0) {
                    return $"Success to task kill (PID = {pid})";
                } else {
                    return $"Exit code of TASKKILL is '{kill.ExitCode}' (PID = {pid})";
                }

            } catch (Exception ex) {
                return $"Failed to task kill (PID = {pid}): {ex.Message}";
            }
        }
    }

    static private void CopyDirectory(string sourceDir, string targetDir) {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir)) {
            var targetPath = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, targetPath, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir)) {
            var targetPath = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectory(dir, targetPath);
        }
    }

    static private ApplicationServiceImplementorBase GetImplementor(string xmlFileName) {
        // アセンブリ内のすべてのIApplicationServiceImplementor実装を取得
        var implementors = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(ApplicationServiceImplementorBase).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t => Activator.CreateInstance(t))
            .OfType<ApplicationServiceImplementorBase>();

        // 対象のXMLファイルに対応する実装を返す
        return implementors.FirstOrDefault(i => i.TargetXmlFileName == $"{xmlFileName}.xml")
            ?? throw new InvalidOperationException(
                $"{xmlFileName}.xml と対応する {nameof(ApplicationServiceImplementorBase)} クラスの実装が見つかりません。" +
                $"Implementorsフォルダにこのパターンの実装クラスがあるか確認してください。");
    }
}

public class ConsoleLogger : ILogger {
    public bool IsEnabled(LogLevel logLevel) => true;
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
        return null;
    }
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        TestContext.Out.WriteLine($"[{logLevel}] {formatter(state, exception)}");
    }
}
