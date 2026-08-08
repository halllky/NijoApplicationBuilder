using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// GET /api/demo/status
/// クライアントがデモモードかどうかを検出するためのエンドポイント。
/// 通常モード(--demo-modeなし)ではこのエンドポイント自体がMapされないため404になる。
/// </summary>
public class DemoStatusEndpointHandler {

    private readonly DemoLockService _lockService;
    private readonly DemoActivityTracker _activityTracker;
    private readonly Demo101ProcessManager _processManager;

    public DemoStatusEndpointHandler(DemoLockService lockService, DemoActivityTracker activityTracker, Demo101ProcessManager processManager) {
        _lockService = lockService;
        _activityTracker = activityTracker;
        _processManager = processManager;
    }

    public async Task Handle(HttpContext context) {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new {
            demoMode = true,
            lockInfo = _lockService.CurrentLock,
            demoUrl = "/demo/",
            demoAppStatus = _processManager.Status,
            lastActivityUtc = _activityTracker.LastActivityUtc,
        });
    }
}
