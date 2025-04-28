using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Nijo.Mcp;

/// <summary>
/// ワークディレクトリ。
/// ログファイル等のパス情報を持つ
/// </summary>
public class WorkDirectory : IDisposable {

    /// <summary>
    /// ワークディレクトリを準備する。
    /// </summary>
    /// <param name="workDirectory">ワークディレクトリ</param>
    /// <param name="errorMessage">エラーメッセージ</param>
    /// <returns>ワークディレクトリが準備できたかどうか</returns>
    public static WorkDirectory Prepare() {
        var directoryPath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location!)!, // net9.0
            "..", // Debug
            "..", // bin
            "..", // Nijo.Mpc
            "..", // NijoApplicationBuilder
            "Nijo.Mpc.WorkDirectory"));
        var mainLog = Path.Combine(directoryPath, "output.log");

        // 既存のログファイルがあれば削除
        if (File.Exists(mainLog)) File.Delete(mainLog);

        // ワークフォルダがなければ作成
        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

        var workDirectory = new WorkDirectory(directoryPath, mainLog);

        // Git管理対象外
        File.WriteAllText(Path.Combine(workDirectory.DirectoryPath, ".gitignore"), "*");

        return workDirectory;
    }

    private WorkDirectory(string fullpath, string mainLog) {
        DirectoryPath = fullpath;
        _mainLogFileStream = new FileStream(mainLog, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        _mainLogWriter = new StreamWriter(_mainLogFileStream, encoding: Encoding.UTF8) {
            AutoFlush = true,
        };
        _lock = new Lock();
    }
    private readonly FileStream _mainLogFileStream;
    private readonly StreamWriter _mainLogWriter;
    private readonly Lock _lock;

    /// <summary>
    /// ワークディレクトリパス
    /// </summary>
    public string DirectoryPath { get; }
    /// <summary>
    /// デバッグプロセスのログ。
    /// デバッグプロセスはMCPツール本体とは別のライフサイクルで動くためファイルも別。
    /// </summary>
    public string DebugLogFile => Path.Combine(DirectoryPath, "output_debug.log");
    /// <summary>
    /// npmの標準出力ログ
    /// </summary>
    public string NpmLogStdOut => Path.Combine(DirectoryPath, "output_npm_stdout.log");
    /// <summary>
    /// npmの標準エラーログ
    /// </summary>
    public string NpmLogStdErr => Path.Combine(DirectoryPath, "output_npm_stderr.log");
    /// <summary>
    /// dotnetの標準出力ログ
    /// </summary>
    public string DotnetLogStdOut => Path.Combine(DirectoryPath, "output_dotnet_stdout.log");
    /// <summary>
    /// dotnetの標準エラーログ
    /// </summary>
    public string DotnetLogStdErr => Path.Combine(DirectoryPath, "output_dotnet_stderr.log");

    /// <summary>
    /// デバッグ中止ファイル。
    /// `nijo run` したときにキャンセル用のファイルを指定できるので、
    /// そこにファイルを出力し、完了まで一定時間待つ。
    /// </summary>
    public string NijoExeCancelFile => Path.Combine(DirectoryPath, "CANCEL_DEBUG_PROCESS.txt");

    /// <summary>
    /// メインログに大まかなセクションタイトルを追加
    /// </summary>
    public void WriteSectionTitle(string title) {
        WriteToMainLog($$"""

            ****************************************
            {{title}}
            """);
    }

    /// <summary>
    /// メインログにテキストを追加する
    /// </summary>
    public void WriteToMainLog(string text) {
        lock (_lock) {
            var counter = 0; // 失敗しても数回はリトライ
            while (counter <= 3) {
                try {
                    _mainLogWriter.WriteLine(text);
                    return;
                } catch {
                    counter++;
                    Thread.Sleep(500);
                }
            }
        }
        throw new InvalidOperationException($"ログ出力に失敗しました。");
    }

    /// <summary>
    /// 引数のメッセージのあとにメインログの内容を付加した文字列を返します。
    /// </summary>
    /// <param name="summary">概要を表す文。</param>
    /// <returns>引数のメッセージのあとにメインログの内容を付加した文字列。</returns>
    public string WithMainLogContents(string summary) {
        lock (_lock) {
            // ファイルの先頭に移動しないとファイル内容を読みだせない
            _mainLogFileStream.Seek(0, SeekOrigin.Begin);

            // StreamReaderが破棄されたあともストリームを閉じないようにするためleaveOpen
            using var reader = new StreamReader(_mainLogFileStream, leaveOpen: true);

            return $$"""
                {{summary}}
                ---
                {{reader.ReadToEnd()}}
                """;
        }
    }

    void IDisposable.Dispose() {
        _mainLogWriter.Dispose();
        _mainLogFileStream.Dispose();
    }
}
