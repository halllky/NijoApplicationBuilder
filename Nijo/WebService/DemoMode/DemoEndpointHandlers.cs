using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nijo.Util.DotnetEx;
using Nijo.WebService.Common;

namespace Nijo.WebService.DemoMode;

internal record ChatRequestBody([property: JsonPropertyName("message")] string? Message);

/// <summary>
/// 共有デモサイトモードのプレゼンテーション層エンドポイントをまとめて処理する。
/// (旧 DemoChatEndpointHandlers / DemoResetService / DemoStatusEndpointHandler を統合)
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

    private readonly DemoModeOptions _options;
    private readonly DemoLockService _lockService;
    private readonly DemoClientRegistry _registry;
    private readonly DemoActivityTracker _activityTracker;
    private readonly Demo101ProcessManager _processManager;
    private readonly ClaudeAgentService _claudeAgent;
    private readonly IHubContext<DemoHub, IDemoHubClient> _hub;
    private readonly ILogger<DemoEndpointHandlers> _logger;

    public DemoEndpointHandlers(
        DemoModeOptions options,
        DemoLockService lockService,
        DemoClientRegistry registry,
        DemoActivityTracker activityTracker,
        Demo101ProcessManager processManager,
        ClaudeAgentService claudeAgent,
        IHubContext<DemoHub, IDemoHubClient> hub,
        ILogger<DemoEndpointHandlers> logger) {
        _options = options;
        _lockService = lockService;
        _registry = registry;
        _activityTracker = activityTracker;
        _processManager = processManager;
        _claudeAgent = claudeAgent;
        _hub = hub;
        _logger = logger;

        // チャットの結果nijo.xmlが実際に変更されたときだけ、フルリビルド
        // (nijo generate + dotnet publish + npm run build。RELEASE_BUILD.sh参照)して
        // デモ101を再起動したうえで全員へ強制リロードを配信する。
        // ビルドが失敗した場合は、エラーログをAIに差し戻して修正させ、再度ビルドする。
        // (このハンドラはチャットのロックを保持したまま RunAsync の延長で実行されるため、
        //  修復ループ中に他のユーザーの操作が割り込むことはない)
        _claudeAgent.SchemaChanged += async () => {
            for (var attempt = 0; ; attempt++) {
                // スキーマが変わった場合、古いデータモデルのDBファイルを残すと画面が
                // 実行時エラーになるため、DBも初期化する(起動時にダミーデータから再作成される)。
                var built = await _processManager.RebuildAndRestartAsync(resetDatabase: true);
                if (built) {
                    await _hub.Clients.All.ForceReload("AIがスキーマを更新しました");
                    return;
                }
                if (attempt >= MAX_BUILD_REPAIR_ATTEMPTS) {
                    await _claudeAgent.NotifyServiceMessageAsync(
                        "自動ビルドの修復を試みましたが失敗しました。デモアプリが停止している可能性があります。" +
                        "「環境をリセット」ボタンで初期状態に戻すことができます。");
                    return;
                }
                await _claudeAgent.NotifyServiceMessageAsync(
                    $"自動ビルドが失敗しました。AIがエラー内容を確認して修正を試みます… ({attempt + 1}/{MAX_BUILD_REPAIR_ATTEMPTS + 1}回目のビルド)");
                var completed = await _claudeAgent.RunBuildRepairAsync(_processManager.LastBuildLog);
                if (!completed) {
                    // ユーザーが中断した場合はループをやめる(環境はリセットボタンで復旧できる)
                    return;
                }
            }
        };
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
            lockInfo = _lockService.CurrentLock,
            demoUrl = "/demo/",
            demoAppStatus = _processManager.Status,
            lastActivityUtc = _activityTracker.LastActivityUtc,
            chatHistory = _claudeAgent.History,
        });
    }

    /// <summary>
    /// POST /api/demo/chat
    /// チャットはロックを保持したまま非同期に実行されるため、
    /// (保存・生成のような)リクエストスコープの<see cref="DemoLocking.WithLock"/>は使わず、
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
        var handle = await _lockService.TryAcquireAsync(clientId, "AIがスキーマを編集中");
        if (handle == null) {
            context.Response.StatusCode = StatusCodes.Status423Locked;
            await context.Response.WriteAsJsonAsync(new {
                reason = _lockService.CurrentLock?.Reason,
                message = "他のユーザーまたはAIが編集中です",
            });
            return;
        }

        context.Response.StatusCode = StatusCodes.Status202Accepted;
        await context.Response.WriteAsJsonAsync(new { accepted = true });

        // ロックを保持したままバックグラウンドで実行する。完了後に解放する。
        _ = Task.Run(async () => {
            try {
                await _claudeAgent.RunAsync(body.Message);
            } catch (Exception ex) {
                _logger.LogError(ex, "claude実行中にエラーが発生しました。");
            } finally {
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
        var currentLock = _lockService.CurrentLock;
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
    /// 共有デモ環境を pristine 状態(イメージビルド時にgit commitした状態)へ戻す。
    /// 手動リセットボタンと <see cref="IdleResetService"/> の両方から呼ばれる。
    /// </summary>
    public async Task<bool> ResetAsync(string clientId) {
        var handle = await _lockService.TryAcquireAsync(clientId, "環境をリセット中");
        if (handle == null) return false;

        try {
            _processManager.Stop();

            // pristine状態(イメージビルド時にコミットしたコミット)へ強制的に戻す。
            // node_modules/bin/obj はビルドキャッシュとして残し、再ビルドの時間を短縮する。
            var checkoutExitCode = await ProcessExtension.ExecuteProcessAsync(psi => {
                psi.FileName = "git";
                psi.ArgumentList.Add("checkout");
                psi.ArgumentList.Add("--");
                psi.ArgumentList.Add(".");
                psi.WorkingDirectory = _options.WorkspaceRoot;
            }, (std, line) => _logger.LogInformation("[git checkout] {line}", line), TimeSpan.FromMinutes(1));
            if (checkoutExitCode != 0) {
                _logger.LogError("リセット中の git checkout が失敗しました (exit code = {code})。ワークスペースが復元されていない可能性があります。", checkoutExitCode);
            }

            var cleanExitCode = await ProcessExtension.ExecuteProcessAsync(psi => {
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
            if (cleanExitCode != 0) {
                _logger.LogError("リセット中の git clean が失敗しました (exit code = {code})。ワークスペースが復元されていない可能性があります。", cleanExitCode);
            }

            _claudeAgent.ResetConversation();

            // pristine状態(イメージビルド時のコミット)にはビルド済みのpublish成果物/wwwrootも
            // 含めてコミットしてあるため、通常はgit checkoutで復元されておりそのまま起動できる
            // (StartAsyncは成果物が無いときだけ自動的にフルビルドする)。
            await _processManager.StartAsync();
        } finally {
            await handle.DisposeAsync();
        }

        await _hub.Clients.All.ForceReload("環境がリセットされました");
        return true;
    }

    /// <summary>
    /// POST /api/demo/reset のHTTPハンドラ。
    /// </summary>
    public async Task HandleReset(HttpContext context) {
        var clientId = DemoClientIdHeader.GetClientId(context);
        var ok = await ResetAsync(clientId);
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
        return DemoLocking.WithLock(
            _lockService, _registry, _hub,
            async context => {
                await _processManager.RestartAsync();
                await HttpResponseHelper.WriteSuccessMessageAsync(context, "restarted");
            },
            reason: "デモ101を再起動中",
            broadcastReloadOnSuccess: true,
            reloadReason: "デモ101が再起動されました");
    }
}
