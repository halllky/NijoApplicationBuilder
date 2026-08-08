using System;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// 最終操作時刻を記録する。IdleResetServiceがこれを見てアイドル判定を行う。
/// SignalRの接続維持だけでは更新されない(接続放置でリセットが妨げられないようにするため)。
/// </summary>
public class DemoActivityTracker {

    private DateTime _lastActivityUtc = DateTime.UtcNow;

    public DateTime LastActivityUtc => _lastActivityUtc;

    public void Touch() {
        _lastActivityUtc = DateTime.UtcNow;
    }
}
