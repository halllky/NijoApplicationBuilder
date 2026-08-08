namespace Nijo.WebService.DemoMode;

/// <summary>
/// 共有デモサイトモードの設定。
/// --demo-mode で serve した場合のみ生成され、DIに登録される。
/// </summary>
public class DemoModeOptions {

    /// <summary>
    /// デモ用に固定するプロジェクトのルートディレクトリ絶対パス。
    /// このモードでは pj クエリパラメータは無視され、常にこのパスが使われる。
    /// </summary>
    public required string WorkspaceRoot { get; init; }

    /// <summary>
    /// この分数の間だれも操作しない場合、環境をリセットする。
    /// </summary>
    public int IdleResetMinutes { get; init; } = 30;

    /// <summary>
    /// デモ101 の React クライアント(vite)が動くURL
    /// </summary>
    public string ViteUrl { get; init; } = "http://localhost:5173";

    /// <summary>
    /// デモ101 の WebApi が動くURL
    /// </summary>
    public string WebApiUrl { get; init; } = "http://localhost:5290";
}
