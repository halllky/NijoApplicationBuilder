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

namespace Nijo.WebService;

/// <summary>
/// スキーマ定義をGUIで編集するアプリケーションのために、
/// nijo.xml を読み込んだり、バリデーションを行ったり、XMLファイルの保存を行ったりする。
/// </summary>
public static class NijoWebService {

    /// <summary>
    /// クエリパラメータ名: プロジェクトディレクトリ
    /// ※React側と合わせる必要あり
    /// </summary>
    public const string PROJECT_DIR_PARAMETER = "pj";

    /// <summary>
    /// HttpContextからプロジェクトディレクトリを取得し、GeneratedProjectを作成する。
    /// 失敗した場合は適切なエラーレスポンスを送信する。
    /// </summary>
    /// <returns>成功した場合はGeneratedProject、失敗した場合はnull（エラーレスポンスは既に送信済み）</returns>
    internal static async Task<GeneratedProject?> OpenProjectOrWriteErrorAsync(HttpContext context) {
        if (!context.TryGetQueryParameter(PROJECT_DIR_PARAMETER, out var projectDir)) {
            await context.WriteErrorAsync(400, $"{PROJECT_DIR_PARAMETER} query parameter is required", context.RequestAborted);
            return null;
        }

        var projectRoot = Path.GetFullPath(projectDir);
        if (!GeneratedProject.TryOpen(projectRoot, out var project, out var error)) {
            await context.WriteErrorAsync(400, error ?? "Unknown error", context.RequestAborted);
            return null;
        }

        return project;
    }

    /// <summary>
    /// Reactアプリケーションからのリクエストを受け取るWebサーバーを設定して返す
    /// </summary>
    public static WebApplication BuildWebApplication(ILogger logger) {
        logger.LogInformation("Webサービスを構築します。");

        var builder = WebApplication.CreateBuilder();

        // JSONオプション
        builder.Services.ConfigureHttpJsonOptions(options => {
            options.SerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        // React側のデバッグのためにポートが異なっていてもアクセスできるようにする
        const string CORS_POLICY_NAME = "AllowAll";
        builder.Services.AddCors(options => {
            options.AddPolicy(CORS_POLICY_NAME, builder => {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        var app = builder.Build();
        app.UseRouting();
        app.UseCors(CORS_POLICY_NAME);

        // スキーマ編集エンドポイント
        var schemaEditor = new SchemaEditor.SchemaEditorEndpoints();
        app.MapGet("/nijo-api/load", schemaEditor.HandleLoad);
        app.MapGet("/nijo-api/schema-rule", schemaEditor.HandleGetRule);
        app.MapPost("/nijo-api/validate", schemaEditor.HandleValidate);
        app.MapPost("/nijo-api/save", schemaEditor.HandleSave);
        app.MapPost("/nijo-api/generate", schemaEditor.HandleGenerate);
        app.MapPost("/nijo-api/types", schemaEditor.HandleGetTypes);

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
