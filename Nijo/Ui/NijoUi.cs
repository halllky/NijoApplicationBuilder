using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nijo.SchemaParsing;

namespace Nijo.Ui;

/// <summary>
/// スキーマ定義をGUIで編集するアプリケーションのために、
/// nijo.xml を読み込んだり、バリデーションを行ったり、XMLファイルの保存を行ったりする。
/// </summary>
public class NijoUi {

    public NijoUi(GeneratedProject project) {
        _project = project;
    }
    private readonly GeneratedProject _project;

    /// <summary>
    /// Reactアプリケーションからのリクエストを受け取るWebサーバーを設定して返す
    /// </summary>
    public WebApplication BuildWebApplication(ILogger logger) {
        var builder = WebApplication.CreateBuilder();

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

        // React.js のビルド後html（js, css がすべて1つのhtmlファイル内にバンドルされているもの）を返す。
        // ここのルーティング名は React Router で設定している該当の画面と合わせる必要あり
        app.MapGet("/nijo-ui", async context => {
            var assembly = Assembly.GetExecutingAssembly();

            // このプロジェクトのプロジェクト名 + csprojで指定したリソース名 + 実ファイル名(index.html)
            const string RESOURCE_NAME = "Nijo.GuiWebAppHtml.index.html";

            using var stream = assembly.GetManifestResourceStream(RESOURCE_NAME)
                ?? throw new InvalidOperationException($"htmlファイルが見つかりません。{assembly.GetName().Name}のビルド前に 'npm run build:nijo-ui' が実行されたか確認してください。");

            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();

            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(html);
        });

        // 画面初期表示時データ読み込み処理
        app.MapGet("/load", async context => {
            var xDocument = XDocument.Load(_project.SchemaXmlPath);
            var rule = SchemaParseRule.Default();

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ApplicationState {
                ApplicationName = xDocument.Root?.Name.LocalName ?? "",
                XmlElementTrees = xDocument.Root?.Elements().Select(root => new ModelPageForm {
                    XmlElements = XmlElementItem.FromXElement(root).ToList(),
                }).ToList() ?? [],
                ValueMemberTypes = ValueMemberType.FromSchemaParseRule(rule),
                AttributeDefs = XmlElementAttribute.FromSchemaParseRule(rule),
            });
        });

        // 編集中のバリデーション
        app.MapPost("/validate", async context => {
            try {
                // XDocumentを構築する。XMLとして破綻していたらエラー
                var originalXDocument = XDocument.Load(_project.SchemaXmlPath);
                var applicationState = await context.Request.ReadFromJsonAsync<ApplicationState>()
                    ?? throw new Exception("applicationState is null");
                var errors = new List<string>();
                if (!applicationState.TryConvertToXDocument(originalXDocument, errors, out var xDocument)) {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(errors);
                    return;
                }

                // スキーマ定義として不正があればエラー
                var rule = SchemaParseRule.Default();
                var schemaParseContext = new SchemaParseContext(xDocument, rule);
                if (!schemaParseContext.TryBuildSchema(schemaParseContext.Document, out var applicationSchema, logger)) {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(errors);
                    return;
                }

                context.Response.StatusCode = (int)HttpStatusCode.OK;

            } catch (Exception ex) {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new[] { ex.Message });
            }
        });

        // nijo.xmlの保存
        app.MapPost("/save", async context => {
            try {
                // バリデーションをかける。
                // XMLとして破綻していたらエラーにするが、スキーマ定義としてエラーがあるかどうかは見ない。
                // 作業中の一時保存のケースがあるため。
                var originalXDocument = XDocument.Load(_project.SchemaXmlPath);
                var applicationState = await context.Request.ReadFromJsonAsync<ApplicationState>()
                    ?? throw new Exception("applicationState is null");
                var errors = new List<string>();
                if (!applicationState.TryConvertToXDocument(originalXDocument, errors, out var xDocument)) {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(errors);
                    return;
                }

                // 保存
                using (var writer = XmlWriter.Create(_project.SchemaXmlPath, new() {
                    Indent = true,
                    Encoding = new UTF8Encoding(false, false),
                    NewLineChars = "\n",
                })) {
                    xDocument.Save(writer);
                }

                context.Response.StatusCode = (int)HttpStatusCode.OK;

            } catch (Exception ex) {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsJsonAsync(new[] { ex.Message });
            }
        });

        // 上位のいずれにも該当しないエンドポイントへのリクエストはReact画面にリダイレクト
        app.MapGet("/{*path}", context => {
            context.Response.Redirect("/nijo-ui");
            return Task.CompletedTask;
        });

        return app;
    }
}
