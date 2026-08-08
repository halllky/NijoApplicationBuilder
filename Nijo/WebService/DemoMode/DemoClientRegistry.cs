using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// SignalR接続IDと、クライアント側で発行されたclientId(sessionStorageのUUID)の対応表。
/// 「自分が起こした変更については自分自身にはForceReloadを送らない」ために使う。
/// </summary>
public class DemoClientRegistry {

    private readonly ConcurrentDictionary<string, string> _connectionIdToClientId = new();

    public void Register(string clientId, string connectionId) {
        _connectionIdToClientId[connectionId] = clientId;
    }

    public void Unregister(string connectionId) {
        _connectionIdToClientId.TryRemove(connectionId, out _);
    }

    /// <summary>
    /// 指定したclientId以外の接続IDの一覧を返す。
    /// </summary>
    public IReadOnlyList<string> GetConnectionIdsExcluding(string? excludeClientId) {
        if (string.IsNullOrEmpty(excludeClientId)) {
            return _connectionIdToClientId.Keys.ToList();
        }
        return _connectionIdToClientId
            .Where(kv => kv.Value != excludeClientId)
            .Select(kv => kv.Key)
            .ToList();
    }
}
