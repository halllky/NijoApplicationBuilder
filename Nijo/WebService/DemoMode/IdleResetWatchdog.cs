using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// 最終操作から一定時間(<see cref="DemoModeOptions.IdleResetMinutes"/>)操作が無い場合に
/// 環境を自動的にリセットする。
/// </summary>
public class IdleResetWatchdog : BackgroundService {

    private static readonly TimeSpan CHECK_INTERVAL = TimeSpan.FromMinutes(1);

    private readonly DemoModeOptions _options;
    private readonly DemoActivity _activity;
    private readonly DemoEnvironment _environment;
    private readonly ILogger<IdleResetWatchdog> _logger;

    public IdleResetWatchdog(
        DemoModeOptions options,
        DemoActivity activity,
        DemoEnvironment environment,
        ILogger<IdleResetWatchdog> logger) {
        _options = options;
        _activity = activity;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                await Task.Delay(CHECK_INTERVAL, stoppingToken);
            } catch (OperationCanceledException) {
                break;
            }

            var idleFor = DateTime.UtcNow - _activity.LastActivityUtc;
            if (idleFor.TotalMinutes < _options.IdleResetMinutes) continue;

            if (!await _environment.IsDirtyAsync(stoppingToken)) continue;

            _logger.LogInformation("アイドル状態が{minutes}分続いたため環境をリセットします。", _options.IdleResetMinutes);
            await _environment.ResetAsync("system:idle-reset");
        }
    }
}
