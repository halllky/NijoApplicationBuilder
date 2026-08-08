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
/// 共有デモサイトモードのプレゼンテーション層エンドポイント。
/// </summary>
public class DemoEndpointHandlers {

    /// <summary>
    /// チャットメッセージの最大文字数。
    /// 公開デモのため、巨大なプロンプトによるAPIコスト浪費を防ぐ。
    /// </summary>
    private const int MAX_CHAT_MESSAGE_LENGTH = 4000;

    /// <summary>
    /// 自動ビルド失敗時にAIへエラーを差し戻して修正させる最大回数。
    /// AIはビルドを自分で実行できない(Bash不許可)ため、ビルド結果の差し戻しがないと
    /// コンパイルエラーを残したままデモが壊れる。一方、無制限に繰り返すと
    /// APIコストとロック保持時間が暴走するため上限を設ける。
    /// </summary>
    private const int MAX_BUILD_REPAIR_ATTEMPTS = 2;

    private readonly DemoLock _lock;
    private readonly Demo101App _demoApp;
    private readonly ClaudeAgent _claudeAgent;
    private readonly DemoActivity _activity;
    private readonly DemoEnvironment _environment;
    private readonly IHubContext<DemoHub, IDemoHubClient> _hub;
    private readonly ILogger<DemoEndpointHandlers> _logger;

    public DemoEndpointHandlers(
        DemoLock @lock,
        Demo101App demoApp,
        ClaudeAgent claudeAgent,
        DemoActivity activity,
        DemoEnvironment environment,
        IHubContext<DemoHub, IDemoHubClient> hub,
        ILogger<DemoEndpointHandlers> logger) {
        _lock = @lock;
        _demoApp = demoApp;
        _claudeAgent = claudeAgent;
        _activity = activity;
        _environment = environment;
        _hub = hub;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/demo/status
    /// クライアントがデモモードかどうかを検出するためのエンドポイント。
    /// 通常モード(--demo-modeなし)ではこのエンドポイント自体がMapされないため404になる。
    /// </summary>
    public async Task HandleStatus(HttpContext context) {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new {
            demoMode = true,
            lockInfo = _lock.CurrentLock,
            demoUrl = "/demo/",
            demoAppStatus = _demoApp.Status,
            chatStatus = _claudeAgent.ChatStatus,
            lastActivityUtc = _activity.LastActivityUtc,
            chatHistory = _claudeAgent.History,
        });
    }

    /// <summary>
    /// POST /api/demo/chat
    /// チャットはロックを保持したまま非同期に実行されるため、
    /// (保存・生成のような)リクエストスコープの<see cref="DemoLock.WithLock"/>は使わず、
    /// ここでロックの取得・解放を明示的に管理する。
    /// </summary>
    public async Task HandleChat(HttpContext context) {
        var body = await context.Request.ReadFromJsonAsync<ChatRequestBody>(context.RequestAborted);
        if (string.IsNullOrWhiteSpace(body?.Message)) {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpResponseHelper.WriteErrorResponseAsync(context, 400, "message is required", context.RequestAborted);
            return;
        }
        if (body.Message.Length > MAX_CHAT_MESSAGE_LENGTH) {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpResponseHelper.WriteErrorResponseAsync(context, 400, $"メッセージは{MAX_CHAT_MESSAGE_LENGTH}文字以内で入力してください", context.RequestAborted);
            return;
        }

        var clientId = DemoClientIdHeader.GetClientId(context);
        var handle = await _lock.TryAcquireAsync(clientId, "AIが編集中");
        if (handle == null) {
            context.Response.StatusCode = StatusCodes.Status423Locked;
            await context.Response.WriteAsJsonAsync(new {
                reason = _lock.CurrentLock?.Reason,
                message = "他のユーザーまたはAIが編集中です",
            });
            return;
        }

        context.Response.StatusCode = StatusCodes.Status202Accepted;
        await context.Response.WriteAsJsonAsync(new { accepted = true });

        // ロックを保持したままバックグラウンドで実行する。完了後に解放する。
        _ = Task.Run(async () => {
            try {
                var changes = await _claudeAgent.RunAsync(body.Message);

                // チャットの結果ワークスペース内のソースが実際に変更されたときだけ、フルリビルド
                // (nijo generate + dotnet publish + npm run build。RELEASE_BUILD.sh参照)して
                // デモ101を再起動したうえで全員へ強制リロードを配信する。
                // ビルドが失敗した場合は、エラーログをAIに差し戻して修正させ、再度ビルドする。
                if (changes.Any) {
                    for (var attempt = 0; ; attempt++) {
                        // チャット処理中である旨の表示を「ビルド中」に切り替える(claude実行中はRunClaudeAsyncが"running"に戻す)
                        await _claudeAgent.SetChatStatusAsync("building");

                        // スキーマ・ダミーデータ生成が変わった場合だけDBも初期化する(起動時にダミーデータから
                        // 再作成される)。手書きコードのみの変更ではユーザーが入力したデータを保持する。
                        var built = await _demoApp.RebuildAndRestartAsync(resetDatabase: changes.RequiresDatabaseReset);
                        if (built) {
                            await _hub.Clients.All.ForceReload("AIがデモアプリを更新しました");
                            break;
                        }
                        if (attempt >= MAX_BUILD_REPAIR_ATTEMPTS) {
                            await _claudeAgent.NotifyErrorMessageAsync(
                                "自動ビルドの修復を試みましたが失敗しました。デモアプリが停止している可能性があります。" +
                                "「環境をリセット」ボタンで初期状態に戻すことができます。");
                            break;
                        }
                        await _claudeAgent.NotifyServiceMessageAsync(
                            $"自動ビルドが失敗しました。AIがエラー内容を確認して修正を試みます… ({attempt + 1}/{MAX_BUILD_REPAIR_ATTEMPTS + 1}回目のビルド)");
                        var completed = await _claudeAgent.RunBuildRepairAsync(_demoApp.LastBuildLog);
                        if (!completed) {
                            // ユーザーが中断した・claudeの実行自体に失敗した場合はループをやめる
                            // (エラーはチャット欄に通知済み。環境はリセットボタンで復旧できる)
                            break;
                        }
                    }
                }
            } catch (Exception ex) {
                // 想定外の例外の最終防波堤。ログにしか出ないとユーザーには沈黙にしか
                // 見えないため、チャット欄にもエラーを表示する。
                _logger.LogError(ex, "claude実行中にエラーが発生しました。");
                try {
                    await _claudeAgent.NotifyErrorMessageAsync("AIの処理中に予期しないエラーが発生しました。詳細は「ログ」タブまたはサーバーログを確認してください。");
                } catch (Exception notifyEx) {
                    _logger.LogError(notifyEx, "エラーのチャット欄への通知に失敗しました。");
                }
            } finally {
                // 例外・中断を含むどの経路でも、処理が終わったことを画面に反映する
                await _claudeAgent.SetChatStatusAsync("idle");
                await handle.DisposeAsync();
            }
        });
    }

    /// <summary>
    /// POST /api/demo/chat/cancel
    /// 中断できるのはそのチャットを開始した本人(=ロック所有者)のみ。
    /// </summary>
    public async Task HandleChatCancel(HttpContext context) {
        var clientId = DemoClientIdHeader.GetClientId(context);
        var currentLock = _lock.CurrentLock;
        if (currentLock != null && currentLock.OwnerClientId != clientId) {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "他のユーザーの実行は中断できません" });
            return;
        }
        _claudeAgent.Cancel();
        await HttpResponseHelper.WriteSuccessMessageAsync(context, "cancelled");
    }

    /// <summary>
    /// POST /api/demo/reset
    /// </summary>
    public async Task HandleReset(HttpContext context) {
        var clientId = DemoClientIdHeader.GetClientId(context);
        var ok = await _environment.ResetAsync(clientId);
        if (!ok) {
            context.Response.StatusCode = StatusCodes.Status423Locked;
            await context.Response.WriteAsJsonAsync(new { message = "他のユーザーまたはAIが編集中です" });
            return;
        }
        await HttpResponseHelper.WriteSuccessMessageAsync(context, "reset");
    }

    /// <summary>
    /// POST /api/demo/app/restart
    /// </summary>
    public RequestDelegate HandleRestart() {
        return _lock.WithLock(
            async context => {
                await _demoApp.RestartAsync();
                await HttpResponseHelper.WriteSuccessMessageAsync(context, "restarted");
            },
            reason: "デモ101を再起動中",
            broadcastReloadOnSuccess: true,
            reloadReason: "デモ101が再起動されました");
    }
}
