using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Text;

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

    [SetUp]
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
    }

    /// <summary>
    /// XMLファイルごとのテスト
    /// </summary>
    /// <param name="xmlFileName">XMLファイル名</param>
    [Test]
    [TestCaseSource(nameof(GetXmlFilePaths))]
    [Category("DataPattern")]
    public void TestXmlPattern(string fileName) {
        var workspaceRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", ".."));
        var testProjectDir = Path.Combine(workspaceRoot, "..", TEST_PROJECT_DIR);
        var dataPatternsDir = Path.Combine(workspaceRoot, "DataPatterns");
        var targetXmlPath = Path.Combine(testProjectDir, "nijo.xml");
        var sourceXmlPath = Path.Combine(dataPatternsDir, $"{fileName}.xml");

        Console.WriteLine($"テスト実行: {fileName}");

        // XMLファイルをコピー
        File.Copy(sourceXmlPath, targetXmlPath, true);

        // ソースコード自動生成を実行
        var project = new GeneratedProject(testProjectDir);
        if (!project.ValidateSchema(_logger)) {
            Assert.Fail($"スキーマ定義の検証に失敗しました。");
        }
        try {
            project.GenerateCode(_logger);
        } catch (Exception ex) {
            Assert.Fail($"ソースコード自動生成に失敗しました。\n{ex}");
        }

        // OverridedApplicationServiceの実装
        var schemaXml = XDocument.Load(project.SchemaXmlPath);
        var implementor = GetImplementor(fileName);
        if (implementor != null) {
            var implementation = implementor.GetImplementation(schemaXml);
            var servicePath = Path.Combine(testProjectDir, "Core", "OverridedApplicationService.cs");
            File.WriteAllText(servicePath, implementation);
        }

        // C#コンパイルチェック
        var solutionPath = Path.Combine(testProjectDir, "NIJO_APPLICATION_TEMPLATE.sln");
        using var csBuildProcess = Process.Start(new ProcessStartInfo {
            FileName = "dotnet",
            Arguments = $"build \"{solutionPath}\" -c Debug",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        })!;

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        csBuildProcess.OutputDataReceived += (sender, e) => {
            if (e.Data != null) {
                outputBuilder.AppendLine(e.Data);
                Console.WriteLine(e.Data);
            }
        };
        csBuildProcess.ErrorDataReceived += (sender, e) => {
            if (e.Data != null) {
                errorBuilder.AppendLine(e.Data);
                Console.WriteLine(e.Data);
            }
        };
        csBuildProcess.BeginOutputReadLine();
        csBuildProcess.BeginErrorReadLine();

        if (!csBuildProcess.WaitForExit(300000)) { // 5分のタイムアウト
            csBuildProcess.Kill();
            Assert.Fail("C#コンパイルがタイムアウトしました。");
        }

        if (csBuildProcess.ExitCode != 0) {
            Assert.Fail($"C#コンパイルに失敗しました。\n出力:\n{outputBuilder}\nエラー:\n{errorBuilder}");
        }

        // TypeScriptコンパイルチェック
        var tsConfigPath = Path.Combine(testProjectDir, "react", "tsconfig.json");
        using var tsBuildProcess = Process.Start(new ProcessStartInfo {
            FileName = "powershell",
            Arguments = "/c \"npm run tsc\"",
            WorkingDirectory = Path.Combine(testProjectDir, "react"),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        })!;
        tsBuildProcess.WaitForExit();
        if (tsBuildProcess.ExitCode != 0) {
            var output = tsBuildProcess.StandardOutput.ReadToEnd();
            var error = tsBuildProcess.StandardError.ReadToEnd();
            Assert.Fail($"TypeScriptコンパイルに失敗しました。\n出力:\n{output}\nエラー:\n{error}");
        }

        // 統合テストの実行
        using var testProcess = Process.Start(new ProcessStartInfo {
            FileName = Path.Combine(testProjectDir, "Test", "bin", "Debug", "net9.0", "run-data-pattern-tests-sample.bat"),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        })!;
        testProcess.WaitForExit();
        if (testProcess.ExitCode != 0) {
            var output = testProcess.StandardOutput.ReadToEnd();
            var error = testProcess.StandardError.ReadToEnd();
            Assert.Fail($"統合テストの実行に失敗しました。\n出力:\n{output}\nエラー:\n{error}");
        }

        Assert.Pass($"{fileName} のテストが完了しました");
    }

    private void CopyDirectory(string sourceDir, string targetDir) {
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

    private IApplicationServiceImplementor GetImplementor(string xmlFileName) {
        // アセンブリ内のすべてのIApplicationServiceImplementor実装を取得
        var implementors = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IApplicationServiceImplementor).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t => Activator.CreateInstance(t))
            .OfType<IApplicationServiceImplementor>();

        // 対象のXMLファイルに対応する実装を返す
        return implementors.First(i => i.TargetXmlFileName == $"{xmlFileName}.xml");
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
