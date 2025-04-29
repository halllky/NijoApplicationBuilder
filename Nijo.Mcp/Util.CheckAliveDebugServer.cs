namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// HTTPポートを監視し、デバッグプロセスが稼働しているかどうかを判定する。
    /// </summary>
    public static async Task<ServiceStatus> ChecAliveDebugServer(WorkDirectory workDirectory, TimeSpan timeout) {

        // サービスの準備完了を待機
        var ready = false;
        var dotnetReady = false;
        var npmReady = false;

        // Webのポートにリクエストを投げて結果が返ってくるかどうかで確認する
        using var _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(3) };
        async Task<(bool Result, string Message)> IsServiceReadyAsync(string url) {
            try {
                var response = await _httpClient.GetAsync(url);
                return (response.IsSuccessStatusCode, $"[{url}] HTTP Response Status: {response.StatusCode}");

            } catch (Exception ex) {
                return (false, $"[{url}] HTTP Request Result: {ex.Message}");
            }
        }

        // .NET側はswaggerを見る
        const string SWAGGER_URL = $"{DOTNET_URL}/swagger";

        // タイムアウトまで繰り返し確認
        var timeoutLimit = DateTime.Now.Add(timeout);
        while (!ready && DateTime.Now < timeoutLimit) {
            if (!dotnetReady) {
                var (alive, message) = await IsServiceReadyAsync(SWAGGER_URL);
                var pid = workDirectory.ReadDotnetRunPidFile();
                if (alive) {
                    dotnetReady = true;
                    workDirectory.WriteToMainLog($"ASP.NET Core サーバー(PID: {pid})は起動されています: {message}");
                } else {
                    workDirectory.WriteToMainLog($"ASP.NET Core サーバー(PID: {pid})は起動されていません: {message}");

                }
            }

            if (!npmReady) {
                var (alive, message) = await IsServiceReadyAsync(NPM_URL);
                var pid = workDirectory.ReadNpmRunPidFile();
                if (alive) {
                    npmReady = true;
                    workDirectory.WriteToMainLog($"Node.js サーバー(PID: {pid})は起動されています: {message}");
                } else {
                    workDirectory.WriteToMainLog($"Node.js サーバー(PID: {pid})は起動されていません: {message}");
                }
            }

            ready = dotnetReady && npmReady;

            if (!ready) {
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
