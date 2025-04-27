namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// 稼働中のサイトのデバッグ情報取得APIを叩いてその結果を返す。
    /// 既にデバッグが開始されていることを前提とする。
    /// </summary>
    /// <param name="timeout">タイムアウト時間</param>
    /// <returns>デバッグ情報取得APIの結果。取得できなかった場合はその旨</returns>
    public static async Task<string> デバッグ中サイト情報取得(TimeSpan timeout) {
        using var httpClient = new HttpClient() {
            BaseAddress = new Uri(DOTNET_URL),
            Timeout = timeout,
        };
        var response = await httpClient.GetAsync("/api/debug-info");

        if (!response.IsSuccessStatusCode) {
            return $$"""
                アプリケーションの実行設定の問い合わせに失敗しました。
                start_debugging でアプリケーションが実行開始されていない可能性があります。
                """;
        }

        return await response.Content.ReadAsStringAsync();
    }
}
