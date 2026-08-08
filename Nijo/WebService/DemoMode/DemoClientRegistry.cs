using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

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
