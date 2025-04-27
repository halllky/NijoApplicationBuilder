using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Nijo.Mcp;

/// <summary>
/// ワークディレクトリ。
/// ログファイル等のパス情報を持つ
/// </summary>
public class WorkDirectory {

    /// <summary>
    /// ワークディレクトリを準備する。
    /// </summary>
    /// <param name="workDirectory">ワークディレクトリ</param>
    /// <param name="errorMessage">エラーメッセージ</param>
    /// <returns>ワークディレクトリが準備できたかどうか</returns>
    public static bool TryPrepare(
        [NotNullWhen(true)] out WorkDirectory? workDirectory,
        [NotNullWhen(false)] out string? errorMessage) {

        try {
            var fullpath = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location!)!, // net9.0
                "..", // Debug
                "..", // bin
                "..", // Nijo.Mpc
                "..", // NijoApplicationBuilder
                "Nijo.Mpc.WorkDirectory"));

            workDirectory = new WorkDirectory(fullpath);

            // ワークフォルダがなければ作成
            if (!Directory.Exists(workDirectory.FullPath)) Directory.CreateDirectory(workDirectory.FullPath);

            // Git管理対象外
            File.WriteAllText(Path.Combine(workDirectory.FullPath, ".gitignore"), "*");

            // 既存のログファイルがあれば削除 (ファイル名を追加)
            if (File.Exists(workDirectory.MainLogFile)) File.Delete(workDirectory.MainLogFile);

            errorMessage = null;
            return true;

        } catch (Exception ex) {
            errorMessage = $$"""
                ワークディレクトリの準備に失敗しました。
                ---
                {{ex}}
                """;
            workDirectory = null;
            return false;
        }
    }

    private WorkDirectory(string fullpath) {
        FullPath = fullpath;
    }
    /// <summary>
    /// ワークディレクトリパス
    /// </summary>
    public string FullPath { get; }
    /// <summary>
    /// ログ
    /// </summary>
    public string MainLogFile => Path.Combine(FullPath, "output.log");
    /// <summary>
    /// デバッグプロセスのログ。
    /// デバッグプロセスはMCPツール本体とは別のライフサイクルで動くためファイルも別。
    /// </summary>
    public string DebugLogFile => Path.Combine(FullPath, "output_debug.log");
    /// <summary>
    /// npmの標準出力ログ
    /// </summary>
    public string NpmLogStdOut => Path.Combine(FullPath, "output_npm_stdout.log");
    /// <summary>
    /// npmの標準エラーログ
    /// </summary>
    public string NpmLogStdErr => Path.Combine(FullPath, "output_npm_stderr.log");
    /// <summary>
    /// dotnetの標準出力ログ
    /// </summary>
    public string DotnetLogStdOut => Path.Combine(FullPath, "output_dotnet_stdout.log");
    /// <summary>
    /// dotnetの標準エラーログ
    /// </summary>
    public string DotnetLogStdErr => Path.Combine(FullPath, "output_dotnet_stderr.log");

    /// <summary>
    /// デバッグ中止ファイル。
    /// `nijo run` したときにキャンセル用のファイルを指定できるので、
    /// そこにファイルを出力し、完了まで一定時間待つ。
    /// </summary>
    public string NijoExeCancelFile => Path.Combine(FullPath, "CANCEL_DEBUG_PROCESS.txt");

    /// <summary>
    /// メインログにテキストを追加する
    /// </summary>
    public void AppendToMainLog(string text) {
        var counter = 0; // 失敗しても数回はリトライ
        while (counter <= 3) {
            try {
                using var writer = new StreamWriter(MainLogFile, append: true, encoding: Encoding.UTF8);
                writer.WriteLine(text);
                return;
            } catch {
                counter++;
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
        return $$"""
            {{summary}}
            ---
            {{File.ReadAllText(MainLogFile)}}
            """;
    }
}
