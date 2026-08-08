using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// クライアントを識別するために送られてくるヘッダ名(sessionStorageのUUID)。
/// ロックの所有者判定・ForceReloadの配信除外にのみ使う(認証ではない)。
/// </summary>
public static class DemoClientIdHeader {
    public const string NAME = "X-Demo-Client-Id";

    public static string GetClientId(HttpContext context) {
        return context.Request.Headers[NAME].ToString();
    }
}

/// <summary>
/// 既存/新規の変異エンドポイントに排他ロックをかけるためのラッパー。
/// ロックが取得できなければ423を返す。取得できれば内部ハンドラを実行し、
/// 成功時(4xx/5xxでなければ)は他クライアントへForceReloadを配信する。
/// </summary>
public static class DemoLocking {

    public static RequestDelegate WithLock(
        DemoLockService lockService,
        DemoClientRegistry registry,
        IHubContext<DemoHub, IDemoHubClient> hub,
        Func<HttpContext, Task> inner,
        string reason,
        bool broadcastReloadOnSuccess,
        string reloadReason = "他のユーザーが更新しました") {

        return async context => {
            var clientId = DemoClientIdHeader.GetClientId(context);

            await using var handle = await lockService.TryAcquireAsync(clientId, reason);
            if (handle == null) {
                context.Response.StatusCode = StatusCodes.Status423Locked;
                await context.Response.WriteAsJsonAsync(new {
                    reason = lockService.CurrentLock?.Reason,
                    message = "他のユーザーまたはAIが編集中です",
                });
                return;
            }

            await inner(context);

            if (broadcastReloadOnSuccess && context.Response.StatusCode < 400) {
                var others = registry.GetConnectionIdsExcluding(clientId);
                if (others.Count > 0) {
                    await hub.Clients.Clients(others).ForceReload(reloadReason);
                }
            }
        };
    }
}
