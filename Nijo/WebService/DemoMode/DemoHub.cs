using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// サーバーから全クライアントへpushするイベント一覧。
/// </summary>
public interface IDemoHubClient {
    Task LockStateChanged(DemoLockInfo? lockInfo);
    Task ForceReload(string reason);
    Task ProcessOutput(string stream, string line);
    Task ChatMessageAppended(DemoChatMessage message);
    Task ChatStreamChunk(string chunk);
    Task DemoAppStatusChanged(string status);
    /// <summary>AIチャット処理の状態変化。statusは <see cref="ClaudeAgentService.ChatStatus"/> の値(idle | running | building)</summary>
    Task ChatStatusChanged(string status);
}

/// <summary>
/// 共有デモサイトのSignalRハブ。
/// 接続してきたクライアントのIDを記録し、ロック状態やデモアプリの状態変化、
/// AIチャットの出力、他ユーザーによる更新の強制リロード通知を配信する。
/// </summary>
public class DemoHub : Hub<IDemoHubClient> {

    private readonly DemoLockService _lockService;
    private readonly DemoClientRegistry _clientRegistry;

    public DemoHub(DemoLockService lockService, DemoClientRegistry clientRegistry) {
        _lockService = lockService;
        _clientRegistry = clientRegistry;
    }

    public override async Task OnConnectedAsync() {
        var clientId = Context.GetHttpContext()?.Request.Query["clientId"].ToString();
        if (!string.IsNullOrEmpty(clientId)) {
            _clientRegistry.Register(clientId, Context.ConnectionId);
        }

        // 接続直後に現在のロック状態を通知する
        await Clients.Caller.LockStateChanged(_lockService.CurrentLock);

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception) {
        _clientRegistry.Unregister(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
