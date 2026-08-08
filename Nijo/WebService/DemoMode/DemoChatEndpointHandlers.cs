using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nijo.WebService.Common;

namespace Nijo.WebService.DemoMode;

internal record ChatRequestBody([property: JsonPropertyName("message")] string? Message);

/// <summary>
/// POST /api/demo/chat, /api/demo/chat/cancel のハンドラ。
/// チャットはロックを保持したまま非同期に実行されるため、
/// (保存・生成のような)リクエストスコープの<see cref="DemoLocking.WithLock"/>は使わず、
/// ここでロックの取得・解放を明示的に管理する。
/// </summary>
public static class DemoChatEndpointHandlers {

    public static RequestDelegate HandleChat(
        DemoLockService lockService,
        ClaudeAgentService claudeAgent,
        ILogger logger) {

        return async context => {
            var body = await context.Request.ReadFromJsonAsync<ChatRequestBody>(context.RequestAborted);
            if (string.IsNullOrWhiteSpace(body?.Message)) {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await HttpResponseHelper.WriteErrorResponseAsync(context, 400, "message is required", context.RequestAborted);
                return;
            }

            var clientId = DemoClientIdHeader.GetClientId(context);
            var handle = await lockService.TryAcquireAsync(clientId, "AIがスキーマを編集中");
            if (handle == null) {
                context.Response.StatusCode = StatusCodes.Status423Locked;
                await context.Response.WriteAsJsonAsync(new {
                    reason = lockService.CurrentLock?.Reason,
                    message = "他のユーザーまたはAIが編集中です",
                });
                return;
            }

            context.Response.StatusCode = StatusCodes.Status202Accepted;
            await context.Response.WriteAsJsonAsync(new { accepted = true });

            // ロックを保持したままバックグラウンドで実行する。完了後に解放する。
            _ = Task.Run(async () => {
                try {
                    await claudeAgent.RunAsync(body.Message);
                } catch (Exception ex) {
                    logger.LogError(ex, "claude実行中にエラーが発生しました。");
                } finally {
                    await handle.DisposeAsync();
                }
            });
        };
    }

    public static RequestDelegate HandleCancel(ClaudeAgentService claudeAgent) {
        return async context => {
            claudeAgent.Cancel();
            await HttpResponseHelper.WriteSuccessMessageAsync(context, "cancelled");
        };
    }
}
