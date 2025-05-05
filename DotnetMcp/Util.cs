
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DotnetMcp;

partial class DotnetMcpTools {

    /// <summary>
    /// DotnetMcpのリクエスト1回分のセットアップ
    /// </summary>
    private static bool TrySetup([NotNullWhen(true)] out DotnetMcpSessionContext? context, [NotNullWhen(false)] out string? error) {

        try {
            var thisExeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            // 実行時設定ファイルの読み込み
            const string DOTNET_MCP_SECTION = "DotnetMcp";
            var appSettingsJson = File.ReadAllText(Path.Combine(thisExeDir, "appsettings.json"));
            var appSettings = JsonSerializer.Deserialize<JsonObject>(appSettingsJson) ?? throw new InvalidOperationException("appsettings.json のデータが不正です。");

            // ワークディレクトリのセットアップ
            var workDirectory = Path.GetFullPath(Path.Combine(
                thisExeDir,
                appSettings[DOTNET_MCP_SECTION]![nameof(DotnetMcpSessionContext.WorkDirectory)]?.ToString() ?? throw new InvalidOperationException("appsettings.json の DotnetMcp セクションに WorkDirectory が設定されていません。")));
            if (!Directory.Exists(workDirectory)) {
                Directory.CreateDirectory(workDirectory);
                File.WriteAllText(Path.Combine(workDirectory, ".gitignore"), "*"); // git管理対象外
            }

            // 結果返却
            context = new DotnetMcpSessionContext {
                SolutionFileFullPath = appSettings[DOTNET_MCP_SECTION]![nameof(DotnetMcpSessionContext.SolutionFileFullPath)]?.ToString() ?? throw new InvalidOperationException("appsettings.json の DotnetMcp セクションに SolutionFileFullPath が設定されていません。"),
                WorkDirectory = workDirectory,
            };
            error = null;
            return true;

        } catch (Exception ex) {
            error = $"MCPツールのセットアップでエラーが発生しました: {ex}";
            context = null;
            return false;
        }
    }
}

/// <summary>
/// リクエスト1回分のコンテキスト情報
/// </summary>
public class DotnetMcpSessionContext {
    /// <summary>
    /// 処理対象のソリューションの絶対パス
    /// </summary>
    public string SolutionFileFullPath { get; set; } = string.Empty;
    /// <summary>
    /// 不具合発生時の調査用のログなどの一時ファイルが出力されるフォルダ
    /// </summary>
    public string WorkDirectory { get; set; } = string.Empty;

    /// <summary>
    /// ログファイルに情報を追記します。ログファイルはセッションを越えて常に1つ。
    /// アーカイブ化などは特に考えていません。
    /// </summary>
    public void WriteLog(string text) {
        var logFile = Path.Combine(WorkDirectory, "log.txt");
        File.AppendAllText(logFile, text);
    }
}
