using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
internal class NijoUi {

    internal NijoUi(GeneratedProject project) {
        _project = project;
    }
    private readonly GeneratedProject _project;

    /// <summary>
    /// Reactアプリケーションからのリクエストを受け取るWebサーバーを設定して返す
    /// </summary>
    internal WebApplication BuildWebApplication(ILogger logger) {
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

        // 画面初期表示時データ読み込み処理
        app.MapGet("/load", async context => {
            var xDocument = XDocument.Load(_project.SchemaXmlPath);
            var rule = SchemaParseRule.Default();

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ApplicationState {
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
                if (!applicationState.TryBuildApplicationSchema(originalXDocument, errors, out var xDocument)) {
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
                if (!applicationState.TryBuildApplicationSchema(originalXDocument, errors, out var xDocument)) {
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

        return app;
    }
}
