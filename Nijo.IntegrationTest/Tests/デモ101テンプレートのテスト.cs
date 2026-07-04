using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;

namespace Nijo.IntegrationTest.Tests;

/// <summary>
/// <para>
/// /demo/101_販売管理システム から新規プロジェクトテンプレート（demo101-template）を作る
/// /Task/デモ101テンプレート作成.sh は、デモ101への機能追加によって不具合が出やすい箇所である。
/// このテストは、そのスクリプトを実行した結果に対してソースコード自動生成をかけなおし、
/// dotnet build / npm run tsc が通ることを確認する。
/// </para>
/// <para>
/// デモ101側の構造が変わり、スクリプトの更新が必要になった場合はこのテストが失敗するので、
/// /Task/Demo101TemplateBuilder/Demo101TemplatePruner.cs を追随させること。
/// </para>
/// </summary>
public class デモ101テンプレートのテスト {

    [Test]
    [Category("時間がかかる")]
    public async Task デモ101テンプレート作成スクリプトの実行結果に対してビルドが通るか() {
        var repoRoot = FindRepoRoot();
        var scriptPath = Path.Combine(repoRoot, "Task", "デモ101テンプレート作成.sh");
        Assert.That(File.Exists(scriptPath), Is.True, $"シェルスクリプトが見つかりません: {scriptPath}");

        var logger = new ConsoleAndContextLogger();

        // 1. シェルスクリプトをそのまま呼ぶ
        var scriptResult = await RunCommandAsync("bash", $"\"{scriptPath}\"", repoRoot, logger);
        Assert.That(scriptResult, Is.True, "デモ101テンプレート作成.sh の実行に失敗しました。");

        var zipPath = Path.Combine(repoRoot, "temp_release", "demo101-template.zip");
        Assert.That(File.Exists(zipPath), Is.True, $"テンプレートzipが生成されていません: {zipPath}");

        // 2. zip化する前のフォルダ相当（zipの展開結果）を用意する
        var extractedRoot = Path.Combine(NijoTestUtil.BaseDirectory, $"{nameof(デモ101テンプレートのテスト)}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(extractedRoot);
        using (var archive = ZipFile.OpenRead(zipPath)) {
            archive.ExtractToDirectory(extractedRoot);
        }

        try {
            // 3. GenerateCode を実行し、コード再生成をかけなおす
            Assert.That(GeneratedProject.TryOpen(extractedRoot, out var project, out var openError), Is.True, openError);

            var xDocument = System.Xml.Linq.XDocument.Load(project!.SchemaXmlPath);
            var rule = SchemaParsing.SchemaParseRule.Default();
            var parseContext = new SchemaParsing.SchemaParseContext(xDocument, rule, CodeGenerating.GeneratedProjectOptions.Parse(xDocument, true));
            var renderingOptions = new CodeGenerating.CodeRenderingOptions { AllowNotImplemented = false };

            Assert.That(project.ValidateSchema(parseContext, logger), Is.True, "デモ101テンプレートのnijo.xmlにスキーマ定義エラーがあります。");
            Assert.That(project.GenerateCode(parseContext, renderingOptions, logger), Is.True, "デモ101テンプレートに対するソースコード自動生成に失敗しました。");

            // 4-a. dotnet build
            var slnPath = Directory.GetFiles(extractedRoot, "*.sln").SingleOrDefault();
            Assert.That(slnPath, Is.Not.Null, "テンプレート直下に .sln ファイルが見つかりません。");

            var dotnetBuildOk = await RunCommandAsync("dotnet", $"build \"{slnPath}\"", extractedRoot, logger);
            Assert.That(dotnetBuildOk, Is.True, "テンプレートに対する dotnet build に失敗しました。");

            // 4-b. npm run tsc
            var clientDir = Path.Combine(extractedRoot, "client");
            Assert.That(Directory.Exists(clientDir), Is.True, $"client フォルダが見つかりません: {clientDir}");

            var npmInstallOk = await RunCommandAsync("npm", "install", clientDir, logger);
            Assert.That(npmInstallOk, Is.True, "テンプレートの client に対する npm install に失敗しました。");

            var npmTscOk = await RunCommandAsync("npm", "run tsc", clientDir, logger);
            Assert.That(npmTscOk, Is.True, "テンプレートの client に対する npm run tsc に失敗しました。");

        } finally {
            // 後片付け（テスト失敗時は調査できるよう残す）
            if (TestContext.CurrentContext.Result.Outcome.Status != NUnit.Framework.Interfaces.TestStatus.Failed) {
                try { Directory.Delete(extractedRoot, recursive: true); } catch { /* ベストエフォート */ }
            }
            try { Directory.Delete(Path.Combine(repoRoot, "temp_release", "demo101-template-work"), recursive: true); } catch { /* 存在しなければ無視 */ }
            try { File.Delete(zipPath); } catch { /* 存在しなければ無視 */ }
        }
    }

    private static string FindRepoRoot() {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "nijo.sln"))) {
            dir = dir.Parent;
        }
        if (dir == null) throw new InvalidOperationException("リポジトリルート（nijo.sln）が見つかりませんでした。");
        return dir.FullName;
    }

    private static async Task<bool> RunCommandAsync(string fileName, string arguments, string workingDirectory, ConsoleAndContextLogger logger) {
        var psi = new ProcessStartInfo {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"プロセスを開始できませんでした: {fileName} {arguments}");
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        logger.LogInfo($"COMMAND: {fileName} {arguments} (cwd={workingDirectory})\nEXIT_CODE: {process.ExitCode}\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");

        return process.ExitCode == 0;
    }

    /// <summary>
    /// <see cref="GeneratedProject.ValidateSchema"/> / <see cref="GeneratedProject.GenerateCode"/> が要求する
    /// <see cref="Microsoft.Extensions.Logging.ILogger"/> の簡易実装。
    /// </summary>
    private class ConsoleAndContextLogger : Microsoft.Extensions.Logging.ILogger {
        public void LogInfo(string message) => TestContext.Progress.WriteLine(message);

        IDisposable? Microsoft.Extensions.Logging.ILogger.BeginScope<TState>(TState state) => null;
        bool Microsoft.Extensions.Logging.ILogger.IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        void Microsoft.Extensions.Logging.ILogger.Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            TestContext.Progress.WriteLine($"[{logLevel}] {formatter(state, exception)}");
            if (exception != null) TestContext.Progress.WriteLine(exception.ToString());
        }
    }
}
