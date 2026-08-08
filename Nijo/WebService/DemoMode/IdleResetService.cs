using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nijo.Util.DotnetEx;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// 最終操作から一定時間(<see cref="DemoModeOptions.IdleResetMinutes"/>)操作が無い場合に
/// 環境を自動的にリセットする。
/// </summary>
public class IdleResetService : BackgroundService {

    private static readonly TimeSpan CHECK_INTERVAL = TimeSpan.FromMinutes(1);

    private readonly DemoModeOptions _options;
    private readonly DemoActivityTracker _activityTracker;
    private readonly DemoEndpointHandlers _endpointHandlers;
    private readonly ILogger<IdleResetService> _logger;

    public IdleResetService(
        DemoModeOptions options,
        DemoActivityTracker activityTracker,
        DemoEndpointHandlers endpointHandlers,
        ILogger<IdleResetService> logger) {
        _options = options;
        _activityTracker = activityTracker;
        _endpointHandlers = endpointHandlers;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                await Task.Delay(CHECK_INTERVAL, stoppingToken);
            } catch (OperationCanceledException) {
                break;
            }

            var idleFor = DateTime.UtcNow - _activityTracker.LastActivityUtc;
            if (idleFor.TotalMinutes < _options.IdleResetMinutes) continue;

            if (!await IsWorkspaceDirtyAsync(stoppingToken)) continue;

            _logger.LogInformation("アイドル状態が{minutes}分続いたため環境をリセットします。", _options.IdleResetMinutes);
            await _endpointHandlers.ResetAsync("system:idle-reset");
        }
    }

    /// <summary>
    /// ワークスペースがpristine状態から変化しているか(git status --porcelain が空でないか)を確認する。
    /// 変化が無ければリセットしても意味が無いのでスキップする。
    ///
    /// DEBUG.sqlite3・WebApi.Log は実行時に必ず作られる untracked ファイル/ディレクトリのため
    /// (Program.cs起動時シード等)、これらを含めると常に「変化あり」判定になってしまう。
    /// pathspecで除外し、実際のソース変更(AIによるnijo.xml編集等)の有無だけを見る。
    /// </summary>
    private async Task<bool> IsWorkspaceDirtyAsync(CancellationToken ct) {
        var isDirty = false;
        try {
            await ProcessExtension.ExecuteProcessAsync(psi => {
                psi.FileName = "git";
                psi.ArgumentList.Add("status");
                psi.ArgumentList.Add("--porcelain");
                psi.ArgumentList.Add("--");
                psi.ArgumentList.Add(".");
                psi.ArgumentList.Add(":!DEBUG.sqlite3");
                psi.ArgumentList.Add(":!WebApi.Log");
                psi.WorkingDirectory = _options.WorkspaceRoot;
            }, (std, line) => {
                if (std == ProcessExtension.E_STD.StdOut && !string.IsNullOrWhiteSpace(line)) {
                    isDirty = true;
                }
            }, TimeSpan.FromSeconds(30));
        } catch (Exception ex) {
            _logger.LogWarning(ex, "git status の確認に失敗しました。安全側に倒してリセットを実行します。");
            return true;
        }
        return isDirty;
    }
}
