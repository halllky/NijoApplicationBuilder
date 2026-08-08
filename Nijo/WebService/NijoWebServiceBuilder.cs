using System;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nijo.WebService.Common;
using Nijo.WebService.DemoMode;
using Nijo.WebService.SchemaEditor;

namespace Nijo.WebService;

/// <summary>
/// スキーマ定義をGUIで編集するアプリケーションのために、
/// nijo.xml を読み込んだり、バリデーションを行ったり、XMLファイルの保存を行ったりする。
/// </summary>
public class NijoWebServiceBuilder {

    public NijoWebServiceBuilder() {
    }

    /// <summary>
    /// Reactアプリケーションからのリクエストを受け取るWebサーバーを設定して返す
    /// </summary>
    /// <param name="logger">ロガー</param>
    /// <param name="demo">
    /// 共有デモサイトモードの設定。nullの場合は通常の(単一ユーザー・ローカル利用の)挙動。
    /// </param>
    public WebApplication BuildWebApplication(ILogger logger, DemoModeOptions? demo = null) {
        var builder = WebApplication.CreateBuilder();

        // JSONオプション
        builder.Services.ConfigureHttpJsonOptions(options => {
            options.SerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        // React側のデバッグのためにポートが異なっていてもアクセスできるようにする
        // (デモモードでは同一オリジンのみで完結するのでAllowAllにしない)
        const string CORS_POLICY_NAME = "AllowAll";
        if (demo == null) {
            builder.Services.AddCors(options => {
                options.AddPolicy(CORS_POLICY_NAME, builder => {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
        }

        if (demo != null) {
            builder.Services.AddSingleton(demo);
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<DemoClientRegistry>();
            builder.Services.AddSingleton<DemoLock>();
            builder.Services.AddSingleton<DemoActivity>();
            builder.Services.AddSingleton<Demo101App>();
            builder.Services.AddSingleton<ClaudeAgent>();
            builder.Services.AddSingleton<DemoEnvironment>();
            builder.Services.AddSingleton<DemoEndpointHandlers>();
            builder.Services.AddHostedService<IdleResetWatchdog>();

            var (routes, clusters) = demo.BuildReverseProxyConfig();
            builder.Services.AddReverseProxy().LoadFromMemory(routes, clusters);
        }

        var app = builder.Build();
        app.UseRouting();
        if (demo == null) {
            app.UseCors(CORS_POLICY_NAME);
        }

        if (demo != null) {
            // 全リクエストで最終操作時刻を更新する(IdleResetWatchdogのアイドル判定に使う)
            app.Use(async (context, next) => {
                app.Services.GetRequiredService<DemoActivity>().Touch();
                await next(context);
            });
        }

        // スキーマ編集エンドポイント
        var schemaHandlers = new SchemaEndpointHandlers();
        app.MapGet("/api/load", schemaHandlers.HandleLoadSchema);
        app.MapPost("/api/validate", schemaHandlers.HandleValidateSchema);
        app.MapPost("/api/types", schemaHandlers.HandleGetNodeTypes);

        if (demo == null) {
            app.MapPost("/api/save", schemaHandlers.HandleSaveSchema);
            app.MapPost("/api/generate", schemaHandlers.HandleGenerateCode);
        } else {
            // デモモードでは保存・生成は排他ロック対象。成功時は他クライアントへ強制リロードを配信する。
            var demoLock = app.Services.GetRequiredService<DemoLock>();

            app.MapPost("/api/save", demoLock.WithLock(
                schemaHandlers.HandleSaveSchema,
                reason: "スキーマを保存中",
                broadcastReloadOnSuccess: true,
                reloadReason: "他のユーザーがスキーマを保存しました"));

            app.MapPost("/api/generate", demoLock.WithLock(
                schemaHandlers.HandleGenerateCode,
                reason: "コードを生成中",
                broadcastReloadOnSuccess: true,
                reloadReason: "他のユーザーがコードを生成しました"));

            app.MapHub<DemoHub>("/api/demo/hub");

            // 統合された共有デモサイトのエンドポイントハンドラ。
            var demoHandlers = app.Services.GetRequiredService<DemoEndpointHandlers>();

            app.MapGet("/api/demo/status", demoHandlers.HandleStatus);
            app.MapPost("/api/demo/chat", demoHandlers.HandleChat);
            app.MapPost("/api/demo/chat/cancel", demoHandlers.HandleChatCancel);
            app.MapPost("/api/demo/reset", demoHandlers.HandleReset);
            app.MapPost("/api/demo/app/restart", demoHandlers.HandleRestart());

            // 起動時にデモ101(WebApi + client)を立ち上げる
            app.Lifetime.ApplicationStarted.Register(() => {
                var demoApp = app.Services.GetRequiredService<Demo101App>();
                _ = demo.ForceRebuildOnStart
                    ? demoApp.RebuildAndRestartAsync()
                    : demoApp.StartAsync();
            });
            app.Lifetime.ApplicationStopping.Register(() => {
                app.Services.GetRequiredService<Demo101App>().Stop();
            });

            app.MapReverseProxy();
        }

        // 上位のいずれにも該当しないエンドポイントへのリクエストは
        // React.js のビルド後html（js, css がすべて1つのhtmlファイル内にバンドルされているもの）を返す。
        app.MapGet("/{*path}", ServeReactHtml);

        return app;
    }

    /// <summary>
    /// React.js のビルド後htmlを返す
    /// </summary>
    private static async Task ServeReactHtml(HttpContext context) {
        var assembly = Assembly.GetExecutingAssembly();
        const string RESOURCE_NAME = "Nijo.GuiWebAppHtml.index.html";

        using var stream = assembly.GetManifestResourceStream(RESOURCE_NAME)
            ?? throw new InvalidOperationException($"htmlファイルが見つかりません。{assembly.GetName().Name}のビルド前に 'npm run build:schema-editor' が実行されたか確認してください。");

        using var reader = new StreamReader(stream);
        var html = await reader.ReadToEndAsync();

        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(html);
    }

}
