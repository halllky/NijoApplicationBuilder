using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nijo.Util.DotnetEx;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// デモ101(WebApi + client)の常駐プロセスを起動・停止・再起動し、
/// 出力をSignalR経由で全クライアントへストリーミングする。
/// </summary>
public class Demo101ProcessManager : IDisposable {

    private const int MAX_LOG_LINES = 500;

    private readonly DemoModeOptions _options;
    private readonly IHubContext<DemoHub, IDemoHubClient> _hub;
    private readonly ILogger<Demo101ProcessManager> _logger;

    private readonly LongRunningProcess _webApi = new();
    private readonly LongRunningProcess _client = new();
    private readonly object _logLock = new();
    private readonly List<(string Stream, string Line)> _recentLogs = new();

    public Demo101ProcessManager(DemoModeOptions options, IHubContext<DemoHub, IDemoHubClient> hub, ILogger<Demo101ProcessManager> logger) {
        _options = options;
        _hub = hub;
        _logger = logger;

        _webApi.OutputReceived += (std, line) => OnOutput("webapi", std, line);
        _client.OutputReceived += (std, line) => OnOutput("client", std, line);
        _webApi.Exited += code => _logger.LogWarning("demo101 WebApi process exited (code={code})", code);
        _client.Exited += code => _logger.LogWarning("demo101 client process exited (code={code})", code);
    }

    public string Status { get; private set; } = "stopped";

    public IReadOnlyList<(string Stream, string Line)> RecentLogs {
        get {
            lock (_logLock) {
                return _recentLogs.ToList();
            }
        }
    }

    public async Task StartAsync() {
        await SetStatusAsync("starting");

        var webApiDir = Path.Combine(_options.WorkspaceRoot, "WebApi");
        _webApi.Start(psi => {
            psi.FileName = "dotnet";
            psi.ArgumentList.Add("watch");
            psi.ArgumentList.Add("--launch-profile");
            psi.ArgumentList.Add("http");
            psi.WorkingDirectory = webApiDir;
        });

        var clientDir = Path.Combine(_options.WorkspaceRoot, "client");
        _client.Start(psi => {
            psi.FileName = "npm";
            psi.ArgumentList.Add("run");
            psi.ArgumentList.Add("dev");
            psi.WorkingDirectory = clientDir;
            psi.Environment["VITE_DEMO_BASE"] = "/demo/";
            psi.Environment["VITE_API_BASE_URL"] = "/demo-api/";
        });

        _ = Task.Run(WaitUntilHealthyAsync);
    }

    public Task RestartAsync() {
        Stop();
        return StartAsync();
    }

    public void Stop() {
        _webApi.Stop();
        _client.Stop();
        _ = SetStatusAsync("stopped");
    }

    private async Task WaitUntilHealthyAsync() {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        var deadline = DateTime.UtcNow.AddMinutes(3);

        // ユーザーが閲覧するのはviteが返す画面なので、WebApiとviteの両方が
        // 応答してはじめて "running" とする(WebApiだけ見ていると、viteが
        // ポート競合等で起動失敗していても稼働中と表示されてしまう)。
        while (DateTime.UtcNow < deadline) {
            if (await RespondsAsync(http, _options.WebApiUrl) && await RespondsAsync(http, _options.ViteUrl)) {
                await SetStatusAsync("running");
                return;
            }
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        await SetStatusAsync("error");
    }

    private static async Task<bool> RespondsAsync(HttpClient http, string url) {
        try {
            var response = await http.GetAsync(url);
            return response.IsSuccessStatusCode || (int)response.StatusCode < 500;
        } catch {
            // まだ起動していない
            return false;
        }
    }

    private async Task SetStatusAsync(string status) {
        Status = status;
        await _hub.Clients.All.DemoAppStatusChanged(status);
    }

    private void OnOutput(string stream, ProcessExtension.E_STD std, string line) {
        lock (_logLock) {
            _recentLogs.Add((stream, line));
            if (_recentLogs.Count > MAX_LOG_LINES) {
                _recentLogs.RemoveAt(0);
            }
        }
        _ = _hub.Clients.All.ProcessOutput(stream, line);
    }

    public void Dispose() {
        Stop();
    }
}
