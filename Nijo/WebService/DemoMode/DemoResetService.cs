using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nijo.Util.DotnetEx;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// 共有デモ環境を pristine 状態(イメージビルド時にgit commitした状態)へ戻す。
/// 手動リセットボタンと <see cref="IdleResetService"/> の両方から呼ばれる。
/// </summary>
public class DemoResetService {

    private readonly DemoModeOptions _options;
    private readonly DemoLockService _lockService;
    private readonly Demo101ProcessManager _processManager;
    private readonly ClaudeAgentService _claudeAgent;
    private readonly IHubContext<DemoHub, IDemoHubClient> _hub;
    private readonly ILogger<DemoResetService> _logger;

    public DemoResetService(
        DemoModeOptions options,
        DemoLockService lockService,
        Demo101ProcessManager processManager,
        ClaudeAgentService claudeAgent,
        IHubContext<DemoHub, IDemoHubClient> hub,
        ILogger<DemoResetService> logger) {
        _options = options;
        _lockService = lockService;
        _processManager = processManager;
        _claudeAgent = claudeAgent;
        _hub = hub;
        _logger = logger;
    }

    /// <summary>
    /// リセットを実行する。ロックが取得できなかった場合はfalseを返す(呼び出し側は423等を返すこと)。
    /// </summary>
    public async Task<bool> ResetAsync(string clientId) {
        var handle = await _lockService.TryAcquireAsync(clientId, "環境をリセット中");
        if (handle == null) return false;

        try {
            _processManager.Stop();

            // pristine状態(イメージビルド時にコミットしたコミット)へ強制的に戻す。
            // node_modules/bin/obj はビルドキャッシュとして残し、再ビルドの時間を短縮する。
            await ProcessExtension.ExecuteProcessAsync(psi => {
                psi.FileName = "git";
                psi.ArgumentList.Add("checkout");
                psi.ArgumentList.Add("--");
                psi.ArgumentList.Add(".");
                psi.WorkingDirectory = _options.WorkspaceRoot;
            }, (std, line) => _logger.LogInformation("[git checkout] {line}", line), TimeSpan.FromMinutes(1));

            await ProcessExtension.ExecuteProcessAsync(psi => {
                psi.FileName = "git";
                psi.ArgumentList.Add("clean");
                psi.ArgumentList.Add("-fd");
                psi.ArgumentList.Add("-e");
                psi.ArgumentList.Add("node_modules");
                psi.ArgumentList.Add("-e");
                psi.ArgumentList.Add("**/bin");
                psi.ArgumentList.Add("-e");
                psi.ArgumentList.Add("**/obj");
                psi.WorkingDirectory = _options.WorkspaceRoot;
            }, (std, line) => _logger.LogInformation("[git clean] {line}", line), TimeSpan.FromMinutes(1));

            SchemaGenerateHelper.TryGenerate(_options.WorkspaceRoot, _logger);

            _claudeAgent.ResetConversation();

            await _processManager.StartAsync();
        } finally {
            await handle.DisposeAsync();
        }

        await _hub.Clients.All.ForceReload("環境がリセットされました");
        return true;
    }
}
