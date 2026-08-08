using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// 排他ロックの現在の保持状況。
/// </summary>
/// <param name="OwnerClientId">ロックを取得したクライアントのID</param>
/// <param name="Reason">ロックの理由(画面表示用。例: "AIが編集中")</param>
/// <param name="AcquiredAtUtc">ロック取得時刻</param>
public record DemoLockInfo(string OwnerClientId, string Reason, DateTime AcquiredAtUtc);

/// <summary>
/// 共有デモ環境の排他ロック。
/// AIによる編集や保存・生成・リセットなど、環境を変更する操作は
/// 同時に1つしか実行できないようにするためのもの。
/// 単一プロセス・単一Machine前提なのでメモリ内のSemaphoreSlimで足りる。
/// </summary>
public class DemoLock {

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IHubContext<DemoHub, IDemoHubClient> _hub;
    private readonly DemoClientRegistry _registry;

    public DemoLock(IHubContext<DemoHub, IDemoHubClient> hub, DemoClientRegistry registry) {
        _hub = hub;
        _registry = registry;
    }

    /// <summary>
    /// 現在のロック状態(誰も持っていなければnull)
    /// </summary>
    public DemoLockInfo? CurrentLock { get; private set; }

    /// <summary>
    /// ロックの取得を試みる。取得できればDispose時に解放するハンドルを返す。
    /// 既に他者が保持している場合はnullを返す(呼び出し側は423を返すこと)。
    /// </summary>
    public async Task<IAsyncDisposable?> TryAcquireAsync(string clientId, string reason) {
        if (!await _semaphore.WaitAsync(0)) {
            return null;
        }

        CurrentLock = new DemoLockInfo(clientId, reason, DateTime.UtcNow);
        await _hub.Clients.All.LockStateChanged(CurrentLock);

        return new LockHandle(this);
    }

    /// <summary>
    /// 既存/新規の変異エンドポイントに排他ロックをかけるためのラッパー。
    /// ロックが取得できなければ423を返す。取得できれば内部ハンドラを実行し、
    /// 成功時(4xx/5xxでなければ)は他クライアントへForceReloadを配信する。
    /// </summary>
    public RequestDelegate WithLock(
        Func<HttpContext, Task> inner,
        string reason,
        bool broadcastReloadOnSuccess,
        string reloadReason = "他のユーザーが更新しました") {

        return async context => {
            var clientId = DemoClientIdHeader.GetClientId(context);

            await using var handle = await TryAcquireAsync(clientId, reason);
            if (handle == null) {
                context.Response.StatusCode = StatusCodes.Status423Locked;
                await context.Response.WriteAsJsonAsync(new {
                    reason = CurrentLock?.Reason,
                    message = "他のユーザーまたはAIが編集中です",
                });
                return;
            }

            await inner(context);

            if (broadcastReloadOnSuccess && context.Response.StatusCode < 400) {
                var others = _registry.GetConnectionIdsExcluding(clientId);
                if (others.Count > 0) {
                    await _hub.Clients.Clients(others).ForceReload(reloadReason);
                }
            }
        };
    }

    private async Task ReleaseAsync() {
        CurrentLock = null;
        _semaphore.Release();
        await _hub.Clients.All.LockStateChanged(null);
    }

    private sealed class LockHandle : IAsyncDisposable {
        private readonly DemoLock _owner;
        private int _disposed;

        public LockHandle(DemoLock owner) {
            _owner = owner;
        }

        public async ValueTask DisposeAsync() {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            await _owner.ReleaseAsync();
        }
    }
}
