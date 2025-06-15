namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// 稼働中のサイトのデバッグ情報取得APIを叩いてその結果を返す。
    /// </summary>
    /// <param name="timeout">タイムアウト時間</param>
    /// <returns>デバッグ情報取得APIの結果。取得できなかった場合はその旨</returns>
    public static async Task<string> デバッグ中サイト情報取得(WorkDirectory workDirectory, TimeSpan timeout) {
        var status = await ChecAliveDebugServer(workDirectory, TimeSpan.FromSeconds(5));
        if (!status.DotnetIsReady || !status.NpmIsReady) {
            return $$"""
                デバッグプロセスまたはその一部は現在実行されていません。
                - ASP.NET Core プロセス: {{(status.DotnetIsReady ? "実行中です。" : "実行されていません。")}}
                - Node.js プロセス     : {{(status.NpmIsReady ? "実行中です。" : "実行されていません。")}}
                """;
        }

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

        return $$"""
            デバッグプロセスは実行中です。
            - ASP.NET Core プロセス: {{DOTNET_URL}}
            - Node.js プロセス     : {{NPM_URL}}

            {{await response.Content.ReadAsStringAsync()}}
            """;
    }
}
