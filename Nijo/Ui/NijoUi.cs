using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nijo.CodeGenerating;
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
                // XDocumentを構築する。XMLとして破綻していたらエラー。
                // 成功した場合、XML要素とJSONのマッピングを作成する。
                var originalXDocument = XDocument.Load(_project.SchemaXmlPath);
                var applicationState = await context.Request.ReadFromJsonAsync<ApplicationState>()
                    ?? throw new Exception("applicationState is null");
                var errors = new List<string>();
                if (!applicationState.TryConvertToXDocument(originalXDocument, errors, out var xDocument, out var uuidToXmlElement)) {
                    context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(errors);
                    return;
                }

                // スキーマ定義として不正があればエラー
                var rule = SchemaParseRule.Default();
                var schemaParseContext = new SchemaParseContext(xDocument, rule);
                if (!schemaParseContext.TryBuildSchema(schemaParseContext.Document, out var applicationSchema, out var xmlErrors)) {
                    // エラーをReactで使うエラー形式に変換する
                    var reactErrorObject = ToReactErrorObject(xmlErrors, uuidToXmlElement);

                    context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(reactErrorObject);
                    return;
                }

                context.Response.StatusCode = (int)HttpStatusCode.OK;

            } catch (Exception ex) {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
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
                if (!applicationState.TryConvertToXDocument(originalXDocument, errors, out var xDocument, out var _)) {
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

        // コード自動生成
        app.MapPost("/generate", async context => {
            try {
                // クライアントからデータを受け取り、XDocumentに変換・保存 (save相当)
                var originalXDocumentBeforeSave = XDocument.Load(_project.SchemaXmlPath);
                var applicationState = await context.Request.ReadFromJsonAsync<ApplicationState>()
                    ?? throw new Exception("applicationState is null");
                var saveErrors = new List<string>();
                if (!applicationState.TryConvertToXDocument(originalXDocumentBeforeSave, saveErrors, out var xDocumentToSave, out var uuidToXmlElementForSave)) {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(saveErrors);
                    return;
                }
                using (var writer = XmlWriter.Create(_project.SchemaXmlPath, new() {
                    Indent = true,
                    Encoding = new UTF8Encoding(false, false),
                    NewLineChars = "\n",
                })) {
                    xDocumentToSave.Save(writer);
                }

                // nijo.xml のフルパスを取得
                var nijoXmlFullPath = Path.GetFullPath(_project.SchemaXmlPath);
                var logger = context.RequestServices.GetRequiredService<ILogger<NijoUi>>();
                var rule = SchemaParseRule.Default();

                // バリデーション (validate相当)
                var validationParseContext = new SchemaParseContext(xDocumentToSave, rule);
                if (!validationParseContext.TryBuildSchema(validationParseContext.Document, out var _, out var xmlErrors)) {
                    // エラーをReactで使うエラー形式に変換する
                    // uuidToXmlElementForSave を使う（保存時のマッピング情報）
                    var reactErrorObject = ToReactErrorObject(xmlErrors, uuidToXmlElementForSave!);

                    context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(reactErrorObject);
                    return;
                }

                // コード生成処理 (エラーがなければ実行)
                var generationParseContext = new SchemaParseContext(xDocumentToSave, rule); // 検証済みXDocumentで再度コンテキスト作成
                var renderingOptions = new CodeRenderingOptions { AllowNotImplemented = false };

                // コード生成処理を実行
                if (_project.GenerateCode(generationParseContext, renderingOptions, logger)) {
                    // 成功応答
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    await context.Response.WriteAsync("Code generation successful.");
                } else {
                    // 失敗応答（GenerateCode内でエラーログは出力されている想定）
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteAsync("Code generation failed. Check server logs for details.");
                }

            } catch (Exception ex) {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new[] { ex.Message });
            }
        });

        // デバッグ用ツール
        new DebugTools(_project).ConfigureWebApplication(app);

        // 型つきアウトライナー用エンドポイント
        new TypedOutliner(_project).ConfigureWebApplication(app);

        // 上位のいずれにも該当しないエンドポイントへのリクエストはReact画面にリダイレクト
        app.MapGet("/{*path}", context => {
            context.Response.Redirect("/nijo-ui");
            return Task.CompletedTask;
        });

        return app;
    }

    /// <summary>
    /// スキーマ定義のエラーをReactで使うエラー形式に変換する。
    /// エラーを表示したあとに画面側で要素の並び替え、名称変更、などの操作があっても
    /// エラーの内容を変更しないために、IDをキーにしてエラーを管理する。
    /// 具体的には以下のようなJSONオブジェクトを返す。
    /// <code>
    /// {
    ///   "xxxx-xxxx-...": {
    ///     "_own": ["xxxが不正です。", "yyyが不正です。"], // XML要素自体に対するエラー
    ///     "DbName": ["テーブル名が不正です。"], // XMLAttributeに対するエラー
    ///     "MaxLength": ["この項目に最大文字数は設定できません。"], // XMLAttributeに対するエラー
    ///     ...
    ///   },
    ///   "yyyy-yyyy-...": {
    ///     ...
    ///   },
    ///   ...
    /// }
    /// </code>
    private static JsonObject ToReactErrorObject(IEnumerable<SchemaParseContext.ValidationError> errors, IReadOnlyDictionary<XElement, string> mapping) {
        // この名前はReact側と合わせる必要がある
        const string OWN_ERRORS = "_own";

        var result = new JsonObject();
        foreach (var error in errors) {
            var id = mapping[error.XElement];

            var thisXmlErrors = new JsonObject();
            result[id] = thisXmlErrors;

            // XML要素自体に対するエラー
            var ownErrors = new JsonArray();
            foreach (var ownError in error.OwnErrors) {
                ownErrors.Add(ownError);
            }

            // XML要素の属性に対するエラー
            thisXmlErrors[OWN_ERRORS] = ownErrors;
            foreach (var attributeError in error.AttributeErrors) {
                var attributeErrors = new JsonArray();
                foreach (var errorMessage in attributeError.Value) {
                    attributeErrors.Add(errorMessage);
                }
                thisXmlErrors[attributeError.Key] = attributeErrors;
            }
        }

        return result;
    }
}
