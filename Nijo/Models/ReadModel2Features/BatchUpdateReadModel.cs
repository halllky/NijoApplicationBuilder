using Nijo.Core;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// <see cref="DataClassForDisplay"/> を一括更新する処理。
    /// サーバー側で画面表示用データを <see cref="DataClassForSave"/> に変換してForSaveの一括更新処理を呼ぶ。
    /// </summary>
    internal class BatchUpdateReadModel : ISummarizedFile {

        private readonly List<GraphNode<Aggregate>> _aggregates = new();
        internal void Register(GraphNode<Aggregate> aggregate) {
            _aggregates.Add(aggregate);
        }

        // --------------------------------------------

        /// <summary>データ種別のプロパティ名（C#側）</summary>
        private const string DATA_TYPE_CS = "DataType";
        /// <summary>データ種別のプロパティ名（TypeScript側）</summary>
        private const string DATA_TYPE_TS = "dataType";
        /// <summary>データ種別の値</summary>
        internal static string GetDataTypeLiteral(GraphNode<Aggregate> aggregate) => aggregate.Item.PhysicalName;

        /// <summary>データ本体のプロパティの名前（C#側）</summary>
        private const string VALUES_CS = "Values";
        /// <summary>データ本体のプロパティの名前（TypeScript側）</summary>
        private const string VALUES_TS = "values";

        internal const string HOOK_NAME = "useBatchUpdateReadModels";
        internal static string GetHookName(GraphNode<Aggregate> agg) => $"useBatchUpdate{agg.Item.PhysicalName}";

        private const string HOOK_PARA_TYPE = "BatchUpdateDisplayDataParam";
        private const string HOOK_PARAM_ITEMS = "Items";

        private const string CONTROLLER_ACTION = "display-data";
        private const string CONTROLLER_ACTION_VER2 = "batch-update";

        /// <summary><see cref="DisplayMessageContainer.INTERFACE"/>エラーが格納されるプロパティ</summary>
        private const string HTTP_RESULT_DETAIL = "detail";
        /// <summary>ブラウザのアラートに表示されるstringの配列</summary>
        private const string HTTP_RESULT_CONFIRM = "confirm";

        private const string APPSRV_CONVERT_DISP_TO_SAVE = "ConvertDisplayDataToSaveData";
        private const string APPSRV_BATCH_UPDATE = "BatchUpdateReadModels";

        int ISummarizedFile.RenderingOrder => 99; // BatchUpdateのソースに一部埋め込んでいるので
        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {

            // 一括更新処理
            var batchUpdate = context.UseSummarizedFile<BatchUpdateWriteModel>();
            if (!context.Config.CustomizeAllUi) {
                context.ReactProject.Types.Add(RenderFuncParamType(context));
            }
            batchUpdate.AddReactHook(RenderFunction(context, null));
            context.UseSummarizedFile<UtilityClass>().AddJsonConverter(RenderJsonConverter());
            batchUpdate.AddControllerAction(RenderControllerAction(context));
            batchUpdate.AddAppSrvMethod(RenderAppSrvMethod(context));
        }

        private string RenderFuncParamType(CodeRenderingContext context) {
            var prefix = context.Config.CustomizeAllUi ? "Types." : "";

            return $$"""
                export type {{HOOK_PARA_TYPE}}
                {{_aggregates.SelectTextTemplate((agg, i) => $$"""
                  {{(i == 0 ? "=" : "|")}} { {{DATA_TYPE_TS}}: '{{GetDataTypeLiteral(agg)}}', {{VALUES_TS}}: {{prefix}}{{new DataClassForDisplay(agg).TsTypeName}} }
                """)}}
                """;
        }

        internal string RenderFunction(CodeRenderingContext context, GraphNode<Aggregate>? aggregate) {

            // aggregateがnullならver.1用ソースをレンダリング。nullでないならver.2

            var hookName = aggregate == null
                ? HOOK_NAME
                : GetHookName(aggregate);
            var prefix = context.Config.CustomizeAllUi ? "" : "Types.";
            var argType = aggregate == null
                ? $"{prefix}{HOOK_PARA_TYPE}"
                : new DataClassForDisplay(aggregate).TsTypeName;
            var url = aggregate == null
                ? $"/{Controller.SUBDOMAIN}/{BatchUpdateWriteModel.CONTROLLER_SUBDOMAIN}/{CONTROLLER_ACTION}"
                : $"/{new Controller(aggregate.Item).SubDomain}/{CONTROLLER_ACTION_VER2}";
            var parameter = aggregate == null
                ? $"{{ {HOOK_PARAM_ITEMS}: items }}"
                : $"items";

            return $$"""
                {{If(aggregate == null && context.Config.CustomizeAllUi, () => $$"""
                {{RenderFuncParamType(context)}}

                """)}}
                /** 画面表示用データの一括更新を即時実行します。更新するデータの量によっては長い待ち時間が発生する可能性があります。 */
                export const {{hookName}} = () => {
                  const { complexPost } = Util.useHttpRequest()
                  const [nowSaving, setNowSaving] = React.useState(false)

                  type BatchUpdateOptions = Omit<Util.ComplexPostOptions<never>, 'setError'> & {
                    setError?: (itemIndex: number, name: string, errors: { types: { [key: string]: string } }) => void
                  }
                  type ErrorDetailType = [
                    itemIndex: number,
                    name: ReactHookForm.FieldPath<ReactHookForm.FieldValues>,
                    error: { types: { [key: string]: string } },
                  ]

                  const batchUpdateReadModels = useEvent(async (items: {{argType}}[], options?: BatchUpdateOptions): Promise<{ ok: boolean }> => {
                    if (nowSaving) return { ok: false }
                    setNowSaving(true)
                    try {
                      // 一括更新の戻り値のエラー情報は通常のcomplexPostのものと異なる（何番目のデータでエラーが発生したかの情報を持っている）ため、独自のハンドリングを行う
                      const { setError, responseHandler: reshandle, ...rest } = (options ?? {})
                      const responseHandler: Util.ComplexPostOptions<never>['responseHandler'] = async response => {
                        // Unprocessable Content. 入力内容エラー
                        if (response.status === 422 && setError) {
                          const errors = await response.json() as { {{Parts.BothOfClientAndServer.ComplexPost.RESPONSE_DETAIL}}: ErrorDetailType[] }
                          for (const error of errors.detail) setError(...error)
                          return { handled: true, ok: false }

                        } else if (reshandle) {
                          return await reshandle(response)
                        } else {
                          return { handled: false }
                        }
                      }

                      return await complexPost(`{{url}}`, {{parameter}}, { ...rest, responseHandler })
                    } finally {
                      setNowSaving(false)
                    }
                  })
                  return {
                    batchUpdateReadModels,
                    nowSaving,
                  }
                }
                """;
        }


        #region version.1
        private UtilityClass.CustomJsonConverter RenderJsonConverter() => new() {
            ConverterClassName = $"{UtilityClass.CUSTOM_CONVERTER_NAMESPACE}.DisplayDataBatchUpdateCommandConverter",
            ConverterClassDeclaring = $$"""
                class DisplayDataBatchUpdateCommandConverter : JsonConverter<{{DataClassForDisplay.BASE_CLASS_NAME}}> {
                    public override {{DataClassForDisplay.BASE_CLASS_NAME}}? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                        using var jsonDocument = JsonDocument.ParseValue(ref reader);
                        var dataType = jsonDocument.RootElement.GetProperty("{{DATA_TYPE_TS}}").GetString();
                        var value = jsonDocument.RootElement.GetProperty("{{VALUES_TS}}");

                {{_aggregates.Select(agg => new { Aggregate = agg, DataClass = new DataClassForDisplay(agg) }).SelectTextTemplate(x => $$"""
                        if (dataType == "{{GetDataTypeLiteral(x.Aggregate)}}") {
                            return JsonSerializer.Deserialize<{{x.DataClass.CsClassName}}>(value.GetRawText(), options)
                                ?? throw new InvalidOperationException($"パラメータを{{x.DataClass.CsClassName}}型に変換できません: {value.GetRawText()}");
                        }
                """)}}

                        throw new InvalidOperationException($"更新パラメータの種別 '{dataType}' を認識できません。");
                    }

                    public override void Write(Utf8JsonWriter writer, {{DataClassForDisplay.BASE_CLASS_NAME}}? value, JsonSerializerOptions options) {
                        JsonSerializer.Serialize(writer, value, options);
                    }
                }
                """,
        };

        private string RenderControllerAction(CodeRenderingContext context) {
            return $$"""
                #region 画面表示用データの一括更新
                /// <summary>
                /// 画面表示用データの一括更新処理を実行します。
                /// </summary>
                /// <param name="request">一括更新内容</param>
                [HttpPost("{{CONTROLLER_ACTION}}")]
                public virtual IActionResult BatchUpdateReadModels(ComplexPostRequest<ReadModelsBatchUpdateParameter> request) {
                    _applicationService.Log.Debug("Batch Update: {0}", Request.Form[ComplexPostRequest.PARAM_DATA].ToString());

                    var options = new {{SaveContext.SAVE_OPTIONS}} {
                        IgnoreConfirm = request.IgnoreConfirm,
                    };
                    var result = _applicationService.{{APPSRV_BATCH_UPDATE}}(request.Data.{{HOOK_PARAM_ITEMS}}, options);
                
                    if (result.HasError()) {
                        return UnprocessableEntity(new {
                            {{HTTP_RESULT_DETAIL}} = result.GetErrorDataJson(),
                        });
                    }
                    if (!request.IgnoreConfirm && result.HasConfirm()) {
                        return Accepted(new {
                            {{HTTP_RESULT_CONFIRM}} = result.GetConfirms().ToArray(),
                            {{HTTP_RESULT_DETAIL}} = result.GetErrorDataJson(),
                        });
                    }
                    return Ok();
                }
                public partial class ReadModelsBatchUpdateParameter {
                    public List<{{DataClassForDisplay.BASE_CLASS_NAME}}> {{HOOK_PARAM_ITEMS}} { get; set; } = new();
                }
                #endregion 画面表示用データの一括更新
                """;
        }


        private string RenderAppSrvMethod(CodeRenderingContext context) {
            var aggregates = _aggregates
                .Select(a => new {
                    Aggregate = a,
                    DisplayData = new DataClassForDisplay(a),
                })
                .ToArray();

            return $$"""
                /// <summary>
                /// データ一括更新を実行します。
                /// </summary>
                /// <param name="items">更新データ</param>
                /// <param name="saveContext">コンテキスト引数。エラーや警告の送出はこのオブジェクトを通して行なってください。</param>
                public virtual {{SaveContext.STATE_CLASS_NAME}} {{APPSRV_BATCH_UPDATE}}(IEnumerable<{{DataClassForDisplay.BASE_CLASS_NAME}}> items, {{SaveContext.SAVE_OPTIONS}} options) {
                    var batchUpdateState = new {{SaveContext.STATE_CLASS_NAME}}(options);

                    var converted = new List<({{DataClassForSaveBase.SAVE_COMMAND_BASE}}, {{DisplayMessageContainer.INTERFACE}})>();

                    // ----------- 画面表示用データから登録更新用データへの変換 -----------
                    var unknownItems = items.ToArray();
                    for (int i = 0; i < unknownItems.Length; i++) {
                        var item = unknownItems[i];
                {{aggregates.SelectTextTemplate((x, j) => $$"""
                        if (item is {{x.DisplayData.CsClassName}} item{{j}}) {
                            // ReadModelからWriteModelへ変換
                            var readModelErrorMessageContainer = new {{x.DisplayData.MessageDataCsClassName}}([]);
                            batchUpdateState.RegisterErrorDataWithIndex(i, readModelErrorMessageContainer);

                            var writeModels = {{APPSRV_CONVERT_DISP_TO_SAVE}}(item{{j}}, readModelErrorMessageContainer, batchUpdateState).ToArray();
                            converted.AddRange(writeModels);
                            continue;
                        }
                """)}}

                        throw new InvalidOperationException($"型 '{item?.GetType().FullName}' の登録更新用データへの変換処理が定義されていません。");
                    }

                    // ----------- 一括更新実行 -----------
                    using var tran = DbContext.Database.BeginTransaction();
                    try {
                        {{BatchUpdateWriteModel.APPSRV_METHOD_PRIVATE}}(converted, batchUpdateState);

                        if (!batchUpdateState.HasError() || batchUpdateState.ForceCommit) {
                            tran.Commit();
                        } else {
                            tran.Rollback();
                        }
                    } catch {
                        tran.Rollback();
                        throw;
                    }

                    return batchUpdateState;
                }

                {{aggregates.SelectTextTemplate(x => $$"""
                /// <summary>
                /// 画面表示用データを登録更新用データに変換します。
                /// </summary>
                /// <param name="displayData">画面表示用データ</param>
                /// <param name="errors">メッセージの入れ物</param>
                /// <param name="saveContext">コンテキスト引数。警告の送出はこのオブジェクトを通して行なってください。</param>
                /// <returns>登録更新用データ（複数返却可）</returns>
                public virtual IEnumerable<({{DataClassForSaveBase.SAVE_COMMAND_BASE}}, {{DisplayMessageContainer.INTERFACE}})> {{APPSRV_CONVERT_DISP_TO_SAVE}}({{x.DisplayData.CsClassName}} displayData, {{x.DisplayData.MessageDataCsClassName}} errors, {{SaveContext.STATE_CLASS_NAME}} saveContextState) {
                {{If(x.Aggregate.Item.Options.GenerateDefaultReadModel, () => $$"""
                    {{WithIndent(RenderWriteModelDefaultConversion(x.Aggregate), "    ")}}
                """).Else(() => $$"""
                    // 変換処理は自動生成されません。
                    // このメソッドをオーバーライドしてデータ変換処理を記述してください。
                    yield break;
                """)}}
                }
                """)}}
                """;
        }

        /// <summary>
        /// 既定のReadModelを生成するオプションが指定されている場合のWriteModelの変換処理定義
        /// </summary>
        private static string RenderWriteModelDefaultConversion(GraphNode<Aggregate> aggregate) {
            var createCommand = new DataClassForSave(aggregate, DataClassForSave.E_Type.Create);
            var saveCommand = new DataClassForSave(aggregate, DataClassForSave.E_Type.UpdateOrDelete);

            return $$"""
                var saveType = displayData.{{DataClassForDisplay.GET_SAVE_TYPE}}();
                if (saveType == {{DataClassForSaveBase.ADD_MOD_DEL_ENUM_CS}}.ADD) {
                    yield return (new {{DataClassForSaveBase.CREATE_COMMAND}}<{{createCommand.CsClassName}}> {
                        {{DataClassForSaveBase.VALUES_CS}} = {{WithIndent(RenderValues(createCommand, "displayData", aggregate, false), "        ")}},
                    }, errors);
                } else if (saveType == {{DataClassForSaveBase.ADD_MOD_DEL_ENUM_CS}}.MOD) {
                    if (displayData.{{DataClassForDisplay.VERSION_CS}} == null) {
                        errors.AddError("更新対象データの更新前のバージョンが指定されていません。");
                        yield break;
                    }
                    yield return (new {{DataClassForSaveBase.UPDATE_COMMAND}}<{{saveCommand.CsClassName}}> {
                        {{DataClassForSaveBase.VALUES_CS}} = {{WithIndent(RenderValues(saveCommand, "displayData", aggregate, false), "        ")}},
                        {{DataClassForSaveBase.VERSION_CS}} = displayData.{{DataClassForDisplay.VERSION_CS}}.Value,
                    }, errors);
                } else if (saveType == {{DataClassForSaveBase.ADD_MOD_DEL_ENUM_CS}}.DEL) {
                    if (displayData.{{DataClassForDisplay.VERSION_CS}} == null) {
                        errors.AddError("更新対象データの更新前のバージョンが指定されていません。");
                        yield break;
                    }
                    yield return (new {{DataClassForSaveBase.DELETE_COMMAND}}<{{saveCommand.CsClassName}}> {
                        {{DataClassForSaveBase.VALUES_CS}} = {{WithIndent(RenderValues(saveCommand, "displayData", aggregate, false), "        ")}},
                        {{DataClassForSaveBase.VERSION_CS}} = displayData.{{DataClassForDisplay.VERSION_CS}}.Value,
                    }, errors);
                }
                """;

            static string RenderValues(
                DataClassForSave forSave,
                string instance,
                GraphNode<Aggregate> instanceAggregate,
                bool renderNewClassName) {

                var newStatement = renderNewClassName
                    ? $"new {forSave.CsClassName}"
                    : $"new()";
                return $$"""
                    {{newStatement}} {
                    {{forSave.GetOwnMembers().SelectTextTemplate(m => $$"""
                        {{DataClassForSave.GetMemberName(m)}} = {{WithIndent(RenderMember(m), "    ")}},
                    """)}}
                    }
                    """;

                string RenderMember(AggregateMember.AggregateMemberBase member) {
                    if (member is AggregateMember.ValueMember vm) {
                        return $$"""
                            {{instance}}.{{vm.Declared.GetFullPathAsDataClassForDisplay(E_CsTs.CSharp, since: instanceAggregate).Join("?.")}}
                            """;

                    } else if (member is AggregateMember.Ref @ref) {
                        var refTargetKeys = new DataClassForRefTargetKeys(@ref.RefTo, @ref.RefTo);
                        return $$"""
                            {{refTargetKeys.RenderFromDisplayData(instance, instanceAggregate, false)}}
                            """;

                    } else if (member is AggregateMember.Children children) {
                        var pathToArray = children.ChildrenAggregate.GetFullPathAsDataClassForDisplay(E_CsTs.CSharp, since: instanceAggregate);
                        var depth = children.ChildrenAggregate.EnumerateAncestors().Count();
                        var x = depth <= 1 ? "x" : $"x{depth}";
                        var child = new DataClassForSave(children.ChildrenAggregate, forSave.Type);
                        return $$"""
                            {{instance}}.{{pathToArray.Join("?.")}}?.Select({{x}} => {{RenderValues(child, x, children.ChildrenAggregate, true)}}).ToList() ?? []
                            """;

                    } else {
                        var memberDataClass = new DataClassForSave(((AggregateMember.RelationMember)member).MemberAggregate, forSave.Type);
                        return $$"""
                            {{RenderValues(memberDataClass, instance, instanceAggregate, false)}}
                            """;
                    }
                }
            }
        }
        #endregion version.1


        #region version.2
        internal static string RenderControllerActionVersion2(CodeRenderingContext context, GraphNode<Aggregate> aggregate) {
            var displayData = new DataClassForDisplay(aggregate);

            return $$"""
                /// <summary>
                /// 画面表示用データの一括更新処理を実行します。
                /// </summary>
                /// <param name="request">一括更新内容</param>
                [HttpPost("{{CONTROLLER_ACTION_VER2}}")]
                public virtual async Task<IActionResult> BatchUpdateReadModels(ComplexPostRequest<List<{{displayData.CsClassName}}>> request) {
                    _applicationService.Log.Debug("Batch Update: {0}", Request.Form[ComplexPostRequest.PARAM_DATA].ToString());

                    using (var tran = await _applicationService.DbContext.Database.BeginTransactionAsync()) {
                        var batchUpdateState = new {{SaveContext.STATE_CLASS_NAME}}(new {{SaveContext.SAVE_OPTIONS}} {
                            IgnoreConfirm = request.IgnoreConfirm,
                        });
                        await _applicationService.{{APPSRV_BATCH_UPDATE}}(request.Data, batchUpdateState);

                        if (batchUpdateState.HasError()) {
                            return this.ShowErrorsUsingReactHook(batchUpdateState.GetErrorDataJson());
                        }
                        if (!request.IgnoreConfirm && batchUpdateState.HasConfirm()) {
                            return this.ShowConfirmUsingReactHook(batchUpdateState.GetConfirms(), batchUpdateState.GetErrorDataJson());
                        }

                        await tran.CommitAsync();
                    }
                    return this.ShowSuccessMessageReactHook();
                }
                """;
        }


        internal static string RenderAppSrvMethodVersion2(CodeRenderingContext context, GraphNode<Aggregate> aggregate) {
            var displayData = new DataClassForDisplay(aggregate);

            return $$"""
                /// <summary>
                /// データ一括更新を実行します。
                /// </summary>
                /// <param name="items">更新データ</param>
                /// <param name="batchUpdateState">コンテキスト引数。エラーや警告の送出はこのオブジェクトを通して行なってください。</param>
                public virtual Task {{APPSRV_BATCH_UPDATE}}(IEnumerable<{{displayData.CsClassName}}> items, {{SaveContext.STATE_CLASS_NAME}} batchUpdateState) {
                    throw new NotImplementedException("一括更新処理が実装されていません。{{APPSRV_BATCH_UPDATE}}メソッドをオーバーライドして内容を実装してください。");
                }
                """;
        }
        #endregion version.2
    }
}
