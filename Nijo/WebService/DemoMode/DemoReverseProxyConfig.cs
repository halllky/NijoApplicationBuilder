using System.Collections.Generic;
using Yarp.ReverseProxy.Configuration;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// デモ101(vite client / WebApi)を公開するYARPルート定義。
/// appsettings不要でメモリ上に定義する。
/// </summary>
public static class DemoReverseProxyConfig {

    public static (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) Build(DemoModeOptions options) {
        var routes = new List<RouteConfig> {
            new RouteConfig {
                RouteId = "demo-client",
                ClusterId = "demo-client-cluster",
                Match = new RouteMatch { Path = "/demo/{**catch-all}" },
            },
            new RouteConfig {
                RouteId = "demo-api",
                ClusterId = "demo-api-cluster",
                Match = new RouteMatch { Path = "/demo-api/{**rest}" },
                Transforms = new[] {
                    new Dictionary<string, string> { ["PathRemovePrefix"] = "/demo-api" },
                },
            },
        };

        // Viteの開発サーバーはHTML中のアセット参照(/@vite/client, /src/main.tsx等)を
        // base設定に関わらずルート相対で出力するため、/demo/ プレフィックス以外にも
        // これらのVite開発専用パスをこのクラスターへ転送する必要がある。
        // (詳細は demo101 client 側の vite.config.ts のコメント参照。
        //  WebService自身はこれらのパスに何もMapしていないため衝突しない)
        foreach (var pattern in new[] {
            "/@vite/{**rest}",
            "/@id/{**rest}",
            "/@fs/{**rest}",
            "/@react-refresh",
            "/src/{**rest}",
            "/node_modules/{**rest}",
            "/__hmr",
        }) {
            routes.Add(new RouteConfig {
                RouteId = $"demo-client-asset-{pattern.TrimStart('/').Split('/')[0].TrimStart('@')}",
                ClusterId = "demo-client-cluster",
                Match = new RouteMatch { Path = pattern },
            });
        }

        var clusters = new List<ClusterConfig> {
            new ClusterConfig {
                ClusterId = "demo-client-cluster",
                Destinations = new Dictionary<string, DestinationConfig> {
                    ["client"] = new DestinationConfig { Address = options.ViteUrl },
                },
            },
            new ClusterConfig {
                ClusterId = "demo-api-cluster",
                Destinations = new Dictionary<string, DestinationConfig> {
                    ["webapi"] = new DestinationConfig { Address = options.WebApiUrl },
                },
            },
        };

        return (routes, clusters);
    }
}
