using System.Collections.Generic;
using Yarp.ReverseProxy.Configuration;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// 共有デモサイトモードの設定。
/// --demo-mode で serve した場合のみ生成され、DIに登録される。
/// </summary>
public class DemoModeOptions {

    /// <summary>
    /// デモ用に固定するプロジェクトのルートディレクトリ絶対パス。
    /// このモードでは pj クエリパラメータは無視され、常にこのパスが使われる。
    /// </summary>
    public required string WorkspaceRoot { get; init; }

    /// <summary>
    /// この分数の間だれも操作しない場合、環境をリセットする。
    /// </summary>
    public int IdleResetMinutes { get; init; } = 30;

    /// <summary>
    /// デモ101 の WebApi が動くURL。
    /// 単一プロセス構成のため、SPA(client)もこのWebApiが静的配信する。
    /// </summary>
    public string WebApiUrl { get; init; } = "http://localhost:5290";

    /// <summary>
    /// 起動時に既存のpublish済み成果物を使い回さず、必ずフルビルドしなおしてから起動する。
    /// </summary>
    public bool ForceRebuildOnStart { get; init; }

    /// <summary>
    /// デモ101(publish済みWebApi単体。APIとSPAの両方を配信する)を公開するYARPルート定義を組み立てる。
    /// appsettings不要でメモリ上に定義する。
    ///
    /// /demo/** (SPA) と /demo-api/** (業務API) はいずれも同じWebApiプロセスへ、
    /// プレフィックスを取り除いて転送する。
    /// </summary>
    public (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) BuildReverseProxyConfig() {
        var routes = new List<RouteConfig> {
            new RouteConfig {
                RouteId = "demo-client",
                ClusterId = "demo101-cluster",
                Match = new RouteMatch { Path = "/demo/{**catch-all}" },
                Transforms = new[] {
                    new Dictionary<string, string> { ["PathRemovePrefix"] = "/demo" },
                },
            },
            new RouteConfig {
                RouteId = "demo-api",
                ClusterId = "demo101-cluster",
                Match = new RouteMatch { Path = "/demo-api/{**rest}" },
                Transforms = new[] {
                    new Dictionary<string, string> { ["PathRemovePrefix"] = "/demo-api" },
                },
            },
        };

        var clusters = new List<ClusterConfig> {
            new ClusterConfig {
                ClusterId = "demo101-cluster",
                Destinations = new Dictionary<string, DestinationConfig> {
                    ["webapi"] = new DestinationConfig { Address = WebApiUrl },
                },
            },
        };

        return (routes, clusters);
    }
}
