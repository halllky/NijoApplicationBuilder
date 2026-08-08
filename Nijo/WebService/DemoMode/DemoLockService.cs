using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// 共有デモ環境の排他ロック。
/// AIによる編集や保存・生成・リセットなど、環境を変更する操作は
/// 同時に1つしか実行できないようにするためのもの。
/// 単一プロセス・単一Machine前提なのでメモリ内のSemaphoreSlimで足りる。
/// </summary>
public class DemoLockService {

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IHubContext<DemoHub, IDemoHubClient> _hub;

    public DemoLockService(IHubContext<DemoHub, IDemoHubClient> hub) {
        _hub = hub;
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

    private async Task ReleaseAsync() {
        CurrentLock = null;
        _semaphore.Release();
        await _hub.Clients.All.LockStateChanged(null);
    }

    private sealed class LockHandle : IAsyncDisposable {
        private readonly DemoLockService _owner;
        private int _disposed;

        public LockHandle(DemoLockService owner) {
            _owner = owner;
        }

        public async ValueTask DisposeAsync() {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            await _owner.ReleaseAsync();
        }
    }
}
