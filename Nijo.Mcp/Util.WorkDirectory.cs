using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

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
        // ワークフォルダがなければ作成
        var directoryPath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location!)!, // net9.0
            "..", // Debug
            "..", // bin
            "..", // Nijo.Mpc
            "..", // NijoApplicationBuilder
            "Nijo.Mpc.WorkDirectory"));
        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

        // 既存のログファイルがあれば削除
        var mainLog = Path.Combine(directoryPath, "output.log");
        if (File.Exists(mainLog)) File.Delete(mainLog);

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

    public string NpmRunPidFile => Path.Combine(DirectoryPath, "PID_NPM_RUN");
    public string NpmRunLogFile => Path.Combine(DirectoryPath, "output_npm-run.log");
    public string NpmRunCmdFile => Path.Combine(DirectoryPath, "run-npm-dev.cmd");
    public string DotnetRunPidFile => Path.Combine(DirectoryPath, "PID_DOTNET_RUN");
    public string DotnetRunLogFile => Path.Combine(DirectoryPath, "output_dotnet-run.log");
    public string DotnetRunCmdFile => Path.Combine(DirectoryPath, "run-dotnet.cmd");

    private readonly Lock _npmLock = new();
    private readonly Lock _dotnetLock = new();
    private bool _isFirstNpmLog = true;
    private bool _isFirstDotnetLog = true;

    public void WriteToNpmRunLog(string text) {
        lock (_npmLock) {
            // このセッションにおける最初の書き込み時は前回のセッションのファイルを削除
            if (_isFirstNpmLog) {
                if (File.Exists(NpmRunLogFile)) File.Delete(NpmRunLogFile);
                _isFirstNpmLog = false;
            }

            using var fs = new FileStream(NpmRunLogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(fs, Encoding.UTF8);
            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {text}");
        }
    }
    public void WriteToDotnetRunLog(string text) {
        lock (_dotnetLock) {
            // このセッションにおける最初の書き込み時は前回のセッションのファイルを削除
            if (_isFirstDotnetLog) {
                if (File.Exists(DotnetRunLogFile)) File.Delete(DotnetRunLogFile);
                _isFirstDotnetLog = false;
            }

            using var fs = new FileStream(DotnetRunLogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(fs, Encoding.UTF8);
            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {text}");
        }
    }
    public int? ReadNpmRunPidFile() {
        if (!File.Exists(NpmRunPidFile)) return null;
        return int.Parse(File.ReadAllText(NpmRunPidFile));
    }
    public int? ReadDotnetRunPidFile() {
        if (!File.Exists(DotnetRunPidFile)) return null;
        return int.Parse(File.ReadAllText(DotnetRunPidFile));
    }
    public void DeleteNpmRunPidFile() {
        lock (_npmLock) {
            if (File.Exists(NpmRunPidFile)) File.Delete(NpmRunPidFile);
        }
    }
    public void DeleteDotnetRunPidFile() {
        lock (_dotnetLock) {
            if (File.Exists(DotnetRunPidFile)) File.Delete(DotnetRunPidFile);
        }
    }

    /// <summary>
    /// メインログに大まかなセクションタイトルを追加
    /// </summary>
    public void WriteSectionTitle(string title) {
        lock (_lock) {
            WriteLineWithRetry(_mainLogWriter, $$"""

                ****************************************
                {{title}}
                """);
        }
    }

    /// <summary>
    /// メインログにテキストを追加する
    /// </summary>
    public void WriteToMainLog(string text) {
        lock (_lock) {
            WriteLineWithRetry(_mainLogWriter, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {text}");
        }
    }
    private static void WriteLineWithRetry(StreamWriter writer, string text) {
        var counter = 0; // 失敗しても数回はリトライ
        while (counter <= 3) {
            try {
                writer.WriteLine(text);
                return;
            } catch {
                counter++;
                Thread.Sleep(500);
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
