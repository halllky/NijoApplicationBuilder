using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
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
/// スキーマ編集関連のエンドポイントハンドラ
/// </summary>
internal class SchemaEndpointHandlers {

    internal SchemaEndpointHandlers(NijoWebService webService) {
        _webService = webService;
    }

    private readonly NijoWebService _webService;

    /// <summary>
    /// 画面初期表示時データ読み込み処理
    /// </summary>
    internal async Task HandleLoadSchema(HttpContext context) {
        try {
            var project = await NijoWebService.OpenProjectOrWriteErrorAsync(context);
            if (project == null) {
                return;
            }

            var xDocument = XDocument.Load(project.SchemaXmlPath);
            var rule = SchemaParseRule.Default();

            var projectOptions = GeneratedProjectOptions.Parse(xDocument, false);

            var generatedProjectInGui = new GeneratedProjectInGui {
                XmlElementTrees = SchemaParseContext.GetAllSectionNames()
                    .Where(sectionName => sectionName != SchemaParseContext.SECTION_CUSTOM_ATTRIBUTES)
                    .Select(sectionName => xDocument.Root?.Element(sectionName))
                    .Where(section => section != null)
                    .SelectMany(section => section!.Elements())
                    .Select(RootAggregateXmlTree.FromXElement)
                    .ToList() ?? [],
                ValueMemberTypes = ValueMemberType.FromSchemaParseRule(rule),
                AttributeDefs = XmlAttributeDef.FromSchemaParseRule(rule),
                CustomAttributes = NijoXmlCustomAttribute.FromXDocument(xDocument).ToList(),
                ProjectOptions = projectOptions.GetCurrentValues(),
                ProjectOptionPropertyInfos = GeneratedProjectOptions.GetPropertyInfos().ToList(),
                GenericLookupTableCategories = GenericLookupTableCategories.FromXDocument(xDocument),
                SchemaGraphViewState = SchemaGraphViewState.Load(project),
                PreviewSetting = PreviewSetting.Load(project),
            };

            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.WriteJsonAsync(generatedProjectInGui, cancellationToken: context.RequestAborted);
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
    internal async Task HandleValidateSchema(HttpContext context) {
        try {
            var project = await NijoWebService.OpenProjectOrWriteErrorAsync(context);
            if (project == null) {
                return;
            }

            var originalXDocument = XDocument.Load(project.SchemaXmlPath);
            var generatedProjectInGui = await context.Request.ReadFromJsonAsync<GeneratedProjectInGui>(context.RequestAborted)
                ?? throw new Exception("generatedProjectInGui is null");

            // XMLとして正しいか検証
            var xmlErrors = new List<string>();
            if (!generatedProjectInGui.TryConvertToXDocument(originalXDocument, xmlErrors, out var xDocument, out var uuidToXmlElement)) {
                context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                await context.WriteJsonAsync(xmlErrors, cancellationToken: context.RequestAborted);
                return;
            }

            // スキーマ定義として正しいか検証
            var rule = SchemaParseRule.Default();
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
    internal async Task HandleSaveSchema(HttpContext context) {
        try {
            var project = await NijoWebService.OpenProjectOrWriteErrorAsync(context);
            if (project == null) {
                return;
            }

            var generatedProjectInGui = await context.Request.ReadFromJsonAsync<GeneratedProjectInGui>(context.RequestAborted)
                ?? throw new Exception("generatedProjectInGui is null");

            // XMLとして正しいか検証（スキーマ定義としてのエラーは見ない。作業中の一時保存のケースがあるため）
            var originalXDocument = XDocument.Load(project.SchemaXmlPath);
            var errors = new List<string>();
            if (!generatedProjectInGui.TryConvertToXDocument(originalXDocument, errors, out var xDocument, out var _)) {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.WriteJsonAsync(errors, cancellationToken: context.RequestAborted);
                return;
            }

            // プロジェクト設定をXMLルート要素の属性として保存
            if (generatedProjectInGui.ProjectOptions != null) {
                var defaultOptions = GeneratedProjectOptions.Parse(null, true);
                var defaultValues = defaultOptions.GetCurrentValues();

                // クライアント側から送られてきたキーを起点に、XMLへの反映を行う。
                // デフォルト値と同じ、またはnullの場合は属性を削除する。
                foreach (var (key, value) in generatedProjectInGui.ProjectOptions) {
                    var kind = value?.GetValueKind();
                    var defaultValue = defaultValues.ContainsKey(key) ? defaultValues[key] : null;

                    // 空か否か
                    var isEmpty = value == null
                        || kind == JsonValueKind.String
                        && string.IsNullOrWhiteSpace(value.GetValue<string>());

                    // デフォルト値と同じか否か
                    var isDefault = false;
                    if (value == null && defaultValue == null) {
                        isDefault = true;
                    } else if (value != null && defaultValue != null) {
                        var valueKind = value.GetValueKind();
                        var defaultKind = defaultValue.GetValueKind();
                        if (valueKind == defaultKind) {
                            isDefault = valueKind switch {
                                JsonValueKind.String => value.GetValue<string>() == defaultValue.GetValue<string>(),
                                JsonValueKind.Number => value.GetValue<decimal>() == defaultValue.GetValue<decimal>(),
                                JsonValueKind.True or JsonValueKind.False => value.GetValue<bool>() == defaultValue.GetValue<bool>(),
                                JsonValueKind.Null => true,
                                _ => false
                            };
                        }
                    }

                    if (isEmpty || isDefault) {
                        xDocument.Root?.Attribute(key)?.Remove();
                    } else {
                        xDocument.Root?.SetAttributeValue(key, value?.ToString());
                    }
                }
            }

            // nijo.xmlの保存
            await project.SaveSchemaXmlAsync(xDocument, context.RequestAborted);

            // nijo.viewState.jsonの保存（nullでない場合のみ）
            if (generatedProjectInGui.SchemaGraphViewState != null) {
                await generatedProjectInGui.SchemaGraphViewState.SaveAsync(project, context.RequestAborted);
            }

            // nijo.preview.jsonの保存
            await generatedProjectInGui.PreviewSetting.SaveAsync(project, context.RequestAborted);

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
    internal async Task HandleGenerateCode(HttpContext context) {
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
    internal async Task HandleGetNodeTypes(HttpContext context) {
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

            // 3. 参照 (ref-to)
            var xDocument = XDocument.Load(project.SchemaXmlPath);
            var generatedProjectInGui = await context.Request.ReadFromJsonAsync<GeneratedProjectInGui>(context.RequestAborted);
            if (generatedProjectInGui != null) {
                var errors = new List<string>();
                if (generatedProjectInGui.TryConvertToXDocument(xDocument, errors, out var editingXDocument, out _)) {
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
