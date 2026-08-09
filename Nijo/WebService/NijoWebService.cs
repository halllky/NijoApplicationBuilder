using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nijo.WebService.Common;
using Nijo.WebService.Previewing;
using Nijo.WebService.SchemaEditor;

namespace Nijo.WebService;

/// <summary>
/// スキーマ定義をGUIで編集するアプリケーションのために、
/// nijo.xml を読み込んだり、バリデーションを行ったり、XMLファイルの保存を行ったりする。
/// また、生成後アプリのデバッグプロセス（プレビュー）を nijo serve の生存期間中保持する。
/// プレビューが保持するOSプロセス・ログファイルハンドルを解放するため <see cref="IDisposable"/> を実装する。
/// </summary>
public class NijoWebService : IDisposable {

    public NijoWebService() {
    }

    /// <summary>
    /// クエリパラメータ名: プロジェクトディレクトリ
    /// ※React側と合わせる必要あり
    /// </summary>
    public const string PROJECT_DIR_PARAMETER = "pj";

    /// <summary>稼働中のプレビュー。キーはプロジェクトルートの絶対パス</summary>
    private readonly ConcurrentDictionary<string, Preview> _previews = new();

    /// <summary>指定プロジェクトのプレビューを返す。無ければ新規に作る</summary>
    public Preview GetPreview(GeneratedProject project) {
        return _previews.GetOrAdd(project.ProjectRoot, _ => new Preview(project.ProjectRoot));
    }

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

    /// <summary>稼働中の全プレビューを停止する</summary>
    public void StopAllPreviews(ILogger logger) {
        foreach (var preview in _previews.Values) {
            preview.Stop(logger);
        }
    }

    /// <summary>
    /// IDisposableの実装。<see cref="StopAllPreviews(ILogger)"/> がログ付きの本来の停止経路であり、
    /// これは using 文や呼び出し漏れに対する最終防衛ラインとして、各プレビューを個別に破棄する。
    /// </summary>
    public void Dispose() {
        foreach (var preview in _previews.Values) {
            preview.Dispose();
        }
    }

    /// <summary>
    /// Reactアプリケーションからのリクエストを受け取るWebサーバーを設定して返す
    /// </summary>
    public WebApplication BuildWebApplication(ILogger logger) {
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
        var schemaHandlers = new SchemaEndpointHandlers(this);
        app.MapGet("/api/load", schemaHandlers.HandleLoadSchema);
        app.MapPost("/api/validate", schemaHandlers.HandleValidateSchema);
        app.MapPost("/api/save", schemaHandlers.HandleSaveSchema);
        app.MapPost("/api/generate", schemaHandlers.HandleGenerateCode);
        app.MapPost("/api/types", schemaHandlers.HandleGetNodeTypes);

        // スキーマ編集エンドポイント（構造化されたデータ形式版。段階的移行中のため /api/... と併存する）
        var schemaEditor2 = new SchemaEditor2.SchemaEditorEndpoints(this);
        app.MapGet("/nijo-api/load", schemaEditor2.HandleLoad);
        app.MapGet("/nijo-api/schema-rule", schemaEditor2.HandleGetRule);
        app.MapPost("/nijo-api/validate", schemaEditor2.HandleValidate);
        app.MapPost("/nijo-api/save", schemaEditor2.HandleSave);
        app.MapPost("/nijo-api/generate", schemaEditor2.HandleGenerate);
        app.MapPost("/nijo-api/types", schemaEditor2.HandleGetTypes);

        // プレビュー（生成後アプリのデバッグプロセス）エンドポイント
        var previewHandlers = new PreviewEndpointHandlers(this);
        app.MapPost("/api/preview/start", previewHandlers.HandleStartPreview);
        app.MapPost("/api/preview/stop", previewHandlers.HandleStopPreview);
        app.MapPost("/api/preview/state", previewHandlers.HandleGetPreviewState);

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
