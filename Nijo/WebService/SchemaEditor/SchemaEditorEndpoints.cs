using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nijo.CodeGenerating;
using Nijo.SchemaParsing;
using Nijo.WebService.Common;
using Nijo.WebService.Previewing;

namespace Nijo.WebService.SchemaEditor;

/// <summary>
/// スキーマ編集関連のエンドポイントハンドラ（構造化されたデータ形式版）。
/// </summary>
internal class SchemaEditorEndpoints {

    internal SchemaEditorEndpoints(NijoWebService webService) {
        _webService = webService;
    }

    private readonly NijoWebService _webService;

    /// <summary>
    /// 画面初期表示時データ読み込み処理
    /// </summary>
    internal async Task HandleLoad(HttpContext context) {
        try {
            var project = await NijoWebService.OpenProjectOrWriteErrorAsync(context);
            if (project == null) {
                return;
            }

            var xDocument = XDocument.Load(project.SchemaXmlPath);
            var rule = SchemaParseRule.Default();

            var editingProject = EditingProject.FromXDocument(xDocument, rule);
            editingProject.GraphViewState = SchemaGraphViewState.Load(project);
            editingProject.PreviewSetting = PreviewSetting.Load(project);

            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.WriteJsonAsync(editingProject, cancellationToken: context.RequestAborted);
        } catch (Exception ex) {
            await Console.Error.WriteLineAsync(ex.ToString());

            await context.WriteErrorAsync(
                (int)HttpStatusCode.InternalServerError,
                ex.Message,
                context.RequestAborted);
        }
    }

    /// <summary>
    /// スキーマ編集画面が必要とする、プロジェクトに依存しない固定ルールの取得。
    /// </summary>
    internal async Task HandleGetRule(HttpContext context) {
        try {
            var rule = SchemaEditorRule.FromSchemaParseRule(SchemaParseRule.Default());

            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.WriteJsonAsync(rule, cancellationToken: context.RequestAborted);
        } catch (Exception ex) {
            await Console.Error.WriteLineAsync(ex.ToString());

            await context.WriteErrorAsync(
                (int)HttpStatusCode.InternalServerError,
                ex.Message,
                context.RequestAborted);
        }
    }

    /// <summary>
    /// 編集中のバリデーション
    /// </summary>
    internal async Task HandleValidate(HttpContext context) {
        try {
            var project = await NijoWebService.OpenProjectOrWriteErrorAsync(context);
            if (project == null) {
                return;
            }

            var originalXDocument = XDocument.Load(project.SchemaXmlPath);
            var editingProject = await context.Request.ReadFromJsonAsync<EditingProject>(context.RequestAborted)
                ?? throw new Exception("editingProject is null");

            // XMLとして正しいか検証
            var rule = SchemaParseRule.Default();
            var xmlErrors = new List<string>();
            if (!editingProject.TryToXDocument(originalXDocument, rule, xmlErrors, out var xDocument, out var uuidToXmlElement)) {
                context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                await context.WriteJsonAsync(xmlErrors, cancellationToken: context.RequestAborted);
                return;
            }

            // スキーマ定義として正しいか検証
            var schemaParseContext = new SchemaParseContext(xDocument, rule, GeneratedProjectOptions.Parse(xDocument, true));
            if (!schemaParseContext.TryBuildSchema(schemaParseContext.Document, out var _, out var schemaErrors)) {
                var reactErrorObject = ValidationErrorMap.FromValidationErrors(schemaErrors, uuidToXmlElement);
                context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                await context.WriteJsonAsync(reactErrorObject, cancellationToken: context.RequestAborted);
                return;
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;

        } catch (Exception ex) {
            await Console.Error.WriteLineAsync(ex.ToString());

            await context.WriteErrorAsync(
                (int)HttpStatusCode.InternalServerError,
                ex.Message,
                context.RequestAborted);
        }
    }

    /// <summary>
    /// nijo.xmlの保存
    /// </summary>
    internal async Task HandleSave(HttpContext context) {
        try {
            var project = await NijoWebService.OpenProjectOrWriteErrorAsync(context);
            if (project == null) {
                return;
            }

            var editingProject = await context.Request.ReadFromJsonAsync<EditingProject>(context.RequestAborted)
                ?? throw new Exception("editingProject is null");

            // XMLとして正しいか検証（スキーマ定義としてのエラーは見ない。作業中の一時保存のケースがあるため）
            var originalXDocument = XDocument.Load(project.SchemaXmlPath);
            var rule = SchemaParseRule.Default();
            var errors = new List<string>();
            if (!editingProject.TryToXDocument(originalXDocument, rule, errors, out var xDocument, out var _)) {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.WriteJsonAsync(errors, cancellationToken: context.RequestAborted);
                return;
            }

            // nijo.xmlの保存
            await project.SaveSchemaXmlAsync(xDocument, context.RequestAborted);

            // nijo.viewState.jsonの保存（nullでない場合のみ）
            if (editingProject.GraphViewState != null) {
                await editingProject.GraphViewState.SaveAsync(project, context.RequestAborted);
            }

            // nijo.preview.jsonの保存
            await editingProject.PreviewSetting.SaveAsync(project, context.RequestAborted);

            context.Response.StatusCode = (int)HttpStatusCode.OK;

        } catch (Exception ex) {
            await Console.Error.WriteLineAsync(ex.ToString());

            await context.WriteErrorAsync(
                (int)HttpStatusCode.BadRequest,
                ex.Message,
                context.RequestAborted);
        }
    }

    /// <summary>
    /// コード自動生成
    /// </summary>
    internal async Task HandleGenerate(HttpContext context) {
        try {
            var project = await NijoWebService.OpenProjectOrWriteErrorAsync(context);
            if (project == null) {
                return;
            }

            var xDocumentToSave = XDocument.Load(project.SchemaXmlPath);
            var rule = SchemaParseRule.Default();

            // バリデーション (validate相当)
            var schemaParseContextForValidation = new SchemaParseContext(xDocumentToSave, rule, GeneratedProjectOptions.Parse(xDocumentToSave, true));
            if (!schemaParseContextForValidation.TryBuildSchema(schemaParseContextForValidation.Document, out var _, out var errors)) {
                var reactErrorObject = ValidationErrorMap.FromValidationErrors(errors, new Dictionary<XElement, string>());
                context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                await context.WriteJsonAsync(reactErrorObject, cancellationToken: context.RequestAborted);
                return;
            }

            // コード生成処理 (エラーがなければ実行)
            var generationParseContext = new SchemaParseContext(xDocumentToSave, rule, GeneratedProjectOptions.Parse(xDocumentToSave, true));
            var renderingOptions = new CodeRenderingOptions { AllowNotImplemented = false };

            var logger = context.RequestServices.GetRequiredService<ILogger<NijoWebService>>();
            if (project.GenerateCode(generationParseContext, renderingOptions, logger)) {
                // コード再生成が成功したので、restartOnGenerateCode が true のプレビュープロセスを再起動する
                var previewSetting = PreviewSetting.Load(project);
                _webService.GetPreview(project).RestartAfterCodeGenerating(previewSetting, logger);

                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.WriteJsonAsync(
                    "Code generation successful.",
                    cancellationToken: context.RequestAborted);
            } else {
                await context.WriteErrorAsync(
                    (int)HttpStatusCode.InternalServerError,
                    "Code generation failed. Check server logs for details.",
                    context.RequestAborted);
            }

        } catch (Exception ex) {
            await Console.Error.WriteLineAsync(ex.ToString());

            await context.WriteErrorAsync(
                (int)HttpStatusCode.InternalServerError,
                ex.Message,
                context.RequestAborted);
        }
    }

    /// <summary>
    /// XML要素の種類の候補リストを返す
    /// </summary>
    internal async Task HandleGetTypes(HttpContext context) {
        try {
            var project = await NijoWebService.OpenProjectOrWriteErrorAsync(context);
            if (project == null) {
                return;
            }

            var keyword = context.Request.Query["keyword"].ToString().ToLowerInvariant();

            var candidates = new List<KeyValuePair<string, string>>();

            // 1. 固定のキーワード
            candidates.Add(KeyValuePair.Create(SchemaParseContext.NODE_TYPE_CHILD, "子要素"));
            candidates.Add(KeyValuePair.Create(SchemaParseContext.NODE_TYPE_CHILDREN, "子配列(リスト)"));

            // 2. プリミティブ型
            var rule = SchemaParseRule.Default();
            foreach (var vm in rule.ValueMemberTypes) {
                candidates.Add(KeyValuePair.Create(vm.SchemaTypeName, vm.DisplayName));
            }

            // 3. 参照 (ref-to)。編集中の内容を反映した上で候補にする。
            var xDocument = XDocument.Load(project.SchemaXmlPath);
            var editingProject = await context.Request.ReadFromJsonAsync<EditingProject>(context.RequestAborted);
            if (editingProject != null) {
                var errors = new List<string>();
                if (editingProject.TryToXDocument(xDocument, rule, errors, out var editingXDocument, out _)) {
                    xDocument = editingXDocument;
                }
            }

            var dataStructures = xDocument.Root?.Element(SchemaParseContext.SECTION_DATA_STRUCTURES);
            if (dataStructures != null) {
                foreach (var element in dataStructures.Elements()) {
                    var name = element.Name.LocalName;
                    var value = $"{SchemaParseContext.NODE_TYPE_REFTO}:{name}";
                    candidates.Add(KeyValuePair.Create(value, value));
                }
            }

            var result = candidates
                .Where(c => string.IsNullOrEmpty(keyword) || c.Key.ToLowerInvariant().Contains(keyword))
                .Distinct()
                .OrderBy(c => c.Key)
                .Select(c => new { value = c.Key, text = c.Value })
                .ToList();

            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.WriteJsonAsync(result, cancellationToken: context.RequestAborted);

        } catch (Exception ex) {
            await Console.Error.WriteLineAsync(ex.ToString());

            await context.WriteErrorAsync(
                (int)HttpStatusCode.InternalServerError,
                ex.Message,
                context.RequestAborted);
        }
    }
}
