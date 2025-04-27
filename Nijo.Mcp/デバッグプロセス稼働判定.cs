namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// HTTPポートを監視し、デバッグプロセスが稼働しているかどうかを判定する。
    /// </summary>
    public static async Task<ServiceStatus> デバッグプロセス稼働判定(WorkDirectory workDirectory, TimeSpan timeout) {

        // サービスの準備完了を待機
        var ready = false;
        var dotnetReady = false;
        var npmReady = false;

        // Webのポートにリクエストを投げて結果が返ってくるかどうかで確認する
        using var _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(1) };
        async Task<bool> IsServiceReadyAsync(string url) {
            try {
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            } catch {
                return false;
            }
        }

        var timeoutLimit = DateTime.Now.Add(timeout);
        while (!ready && DateTime.Now < timeoutLimit) {
            if (!dotnetReady) {
                dotnetReady = await IsServiceReadyAsync($"{DOTNET_URL}/swagger");
                if (dotnetReady) {
                    workDirectory.AppendToMainLog("[nijo-mcp] ASP.NET Core サーバーの起動を確認しました。");
                } else {
                    workDirectory.AppendToMainLog("[nijo-mcp] ASP.NET Core サーバーはまだ起動されていません。");
                }
            }

            if (!npmReady) {
                npmReady = await IsServiceReadyAsync(NPM_URL);
                if (npmReady) {
                    workDirectory.AppendToMainLog("[nijo-mcp] Node.js サーバーの起動を確認しました。");
                } else {
                    workDirectory.AppendToMainLog("[nijo-mcp] Node.js サーバーはまだ起動されていません。");
                }
            }

            ready = dotnetReady && npmReady;

            if (!ready) {
                workDirectory.AppendToMainLog("[nijo-mcp] 待機中...");
                await Task.Delay(500); // 0.5秒待機
            }
        }

        return new ServiceStatus {
            DotnetIsReady = dotnetReady,
            NpmIsReady = npmReady,
        };
    }
}

/// <summary>
/// サービスの準備完了状態
/// </summary>
public class ServiceStatus {
    /// <summary>
    /// ASP.NET Core サーバーの準備完了状態
    /// </summary>
    public bool DotnetIsReady { get; init; }
    /// <summary>
    /// Node.js サーバーの準備完了状態
    /// </summary>
    public bool NpmIsReady { get; init; }
}
