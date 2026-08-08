using System.Collections.Generic;
using Yarp.ReverseProxy.Configuration;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// デモ101(publish済みWebApi単体。APIとSPAの両方を配信する)を公開するYARPルート定義。
/// appsettings不要でメモリ上に定義する。
///
/// /demo/** (SPA) と /demo-api/** (業務API) はいずれも同じWebApiプロセスへ、
/// プレフィックスを取り除いて転送する。
/// </summary>
public static class DemoReverseProxyConfig {

    public static (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) Build(DemoModeOptions options) {
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
                    ["webapi"] = new DestinationConfig { Address = options.WebApiUrl },
                },
            },
        };

        return (routes, clusters);
    }
}
