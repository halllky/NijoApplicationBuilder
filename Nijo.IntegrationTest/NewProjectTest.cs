using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Threading;
using System.Text;

namespace Nijo.IntegrationTest;

[Category("新規プロジェクト作成")]
public class NewProjectTest {
    private const string TEST_PROJECT_DIR = "プロジェクト新規作成処理のテストで作成されたプロジェクト";
    private readonly ILogger _logger = new ConsoleLogger();
    private string _testProjectDir = string.Empty;
    private string _workspaceRoot = string.Empty;
    private string _repoRoot = string.Empty;
    private const int COMMAND_TIMEOUT_SECONDS = 300;

    [SetUp]
    public void SetUp() {
        // テストプロジェクトのソリューションディレクトリを取得
        _workspaceRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", ".."));
        // リポジトリのルートディレクトリを取得（release.batがある場所）
        _repoRoot = Path.GetFullPath(Path.Combine(_workspaceRoot, ".."));
        // テストプロジェクトの出力先ディレクトリを設定
        _testProjectDir = Path.Combine(_repoRoot, TEST_PROJECT_DIR);

        // 前回実行時のごみが残っている可能性があるので、出力先ディレクトリが存在する場合は削除
        if (Directory.Exists(_testProjectDir)) {
            _logger.LogInformation("既存の出力先ディレクトリを削除します: {directory}", _testProjectDir);
            Directory.Delete(_testProjectDir, recursive: true);
        }
    }

    [TearDown]
    public void TearDown() {
        // 作成後のプロジェクトを目検で調査するケースがあるので削除しない
    }

    [Test(Description = "新規プロジェクト作成のテスト")]
    public async Task TestNewProject() {
        // release.batを実行する
        string releaseBatPath = Path.Combine(_repoRoot, "release.bat");
        Assert.That(File.Exists(releaseBatPath), Is.True, $"release.batが見つかりません: {releaseBatPath}");

        _logger.LogInformation("release.batを実行します: {releaseBatPath}", releaseBatPath);
        // バージョン番号「1.0.0」を標準入力に渡す
        bool releaseSuccess = await ExecuteProcess("cmd.exe", $"/c \"{releaseBatPath}\" TEST", _repoRoot, new UTF8Encoding(false, false));
        Assert.That(releaseSuccess, Is.True, "release.batの実行に失敗しました。");

        // リリースされたnijo.exeのパスを特定する
        string nijoExePath = Path.Combine(_repoRoot, "Nijo", "bin", "Release", "net9.0", "publish-win", "nijo.exe");
        Assert.That(File.Exists(nijoExePath), Is.True, $"nijo.exeが見つかりません: {nijoExePath}");

        // nijo newコマンドを実行してプロジェクトを作成する（常にnpm ciをスキップ）
        _logger.LogInformation("nijo newコマンドを実行します: {nijoExePath}", nijoExePath);
        bool nijoNewSuccess = await ExecuteProcess(nijoExePath, $"new \"{_testProjectDir}\" --skip-npm-ci", _repoRoot);
        Assert.That(nijoNewSuccess, Is.True, "nijo newコマンドの実行に失敗しました。");

        // プロジェクトが要件を満たしているか確認
        Assert.That(Directory.Exists(_testProjectDir), Is.True, "プロジェクトディレクトリが作成されていません。");
        Assert.That(File.Exists(Path.Combine(_testProjectDir, "nijo.xml")), Is.True, "nijo.xml が作成されていません。");

        // npm ciは常にスキップするため、node_modulesのチェックはしない
    }

    private async Task<bool> ExecuteProcess(string fileName, string arguments, string workingDirectory, Encoding? encoding = null) {
        var processStartInfo = new ProcessStartInfo {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,  // 標準入力をリダイレクト
            StandardOutputEncoding = encoding,
            StandardErrorEncoding = encoding,
            StandardInputEncoding = encoding,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = processStartInfo };
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(COMMAND_TIMEOUT_SECONDS));

        process.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                _logger.LogInformation(e.Data);
            }
        };
        process.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                _logger.LogError(e.Data);
            }
        };

        try {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try {
                // CancellationTokenを直接WaitForExitAsyncに渡す
                await process.WaitForExitAsync(timeoutCts.Token);
                return process.ExitCode == 0;
            } catch (OperationCanceledException) {
                // タイムアウト発生時
                _logger.LogError("コマンド実行がタイムアウトしました（{timeout}秒）: {fileName} {arguments}", COMMAND_TIMEOUT_SECONDS, fileName, arguments);
                try {
                    if (!process.HasExited) {
                        process.Kill(entireProcessTree: true);
                        _logger.LogInformation("プロセスを強制終了しました。");
                    }
                } catch (Exception ex) {
                    _logger.LogError("プロセス強制終了中にエラーが発生しました: {message}", ex.Message);
                }
                return false;
            }
        } catch (Exception ex) {
            _logger.LogError("プロセス実行中にエラーが発生しました: {message}", ex.Message);
            return false;
        }
    }
}
