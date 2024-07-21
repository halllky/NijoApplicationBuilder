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
    internal class BatchUpdateDisplayData : ISummarizedFile {

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
        private static string GetDataTypeLiteral(GraphNode<Aggregate> aggregate) => aggregate.Item.DisplayName;

        /// <summary>データ本体のプロパティの名前（C#側）</summary>
        private const string VALUES_CS = "Values";
        /// <summary>データ本体のプロパティの名前（TypeScript側）</summary>
        private const string VALUES_TS = "values";

        internal const string HOOK_NAME = "batchUpdateDisplayData";
        private const string HOOK_PARA_TYPE = "BatchUpdateDisplayDataParam";
        private const string HOOK_PARAM_ITEMS = "Items";

        private const string CONTROLLER_ACTION = "display-data";

        private const string APPSRV_CONVERT_DISP_TO_SAVE = "ConvertDisplayDataToSaveData";
        private const string APPSRV_BATCH_UPDATE = "BatchUpdate";

        int ISummarizedFile.RenderingOrder => 99; // BatchUpdateのソースに一部埋め込んでいるので
        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {

            // 一括更新処理
            var batchUpdate = context.UseSummarizedFile<BatchUpdate>();
            context.ReactProject.Types.Add(RenderHookParamType());
            batchUpdate.AddReactHook(HOOK_NAME, RenderReactHook(context));
            context.UseSummarizedFile<Parts.Utility.UtilityClass>().AddJsonConverter(RenderJsonConverter());
            batchUpdate.AddControllerAction(RenderControllerAction(context));
            batchUpdate.AddAppSrvMethod(RenderAppSrvMethod(context));
        }

        private string RenderHookParamType() {
            return $$"""
                export type {{HOOK_PARA_TYPE}}
                {{_aggregates.SelectTextTemplate((agg, i) => $$"""
                  {{(i == 0 ? "=" : "|")}} { {{DATA_TYPE_TS}}: '{{GetDataTypeLiteral(agg)}}', {{VALUES_TS}}: {{new DataClassForDisplay(agg).TsTypeName}} }
                """)}}
                """;
        }

        private string RenderReactHook(CodeRenderingContext context) {
            return $$"""
                /** 画面表示用データの一括更新を即時実行します。更新するデータの量によっては長い待ち時間が発生する可能性があります。 */
                const {{HOOK_NAME}} = React.useCallback((...{{HOOK_PARAM_ITEMS}}: Types.{{HOOK_PARA_TYPE}}[]) => {
                    const res = await post(`{{Controller.SUBDOMAIN}}/{{BatchUpdate.CONTROLLER_SUBDOMAIN}}/{{CONTROLLER_ACTION}}`, { {{HOOK_PARAM_ITEMS}} })
                    if (!res.ok) {
                      dispatchMsg(msg => msg.error('一括更新に失敗しました。'))
                    }
                }, [post, dispatchMsg])
                """;
        }

        private Parts.Utility.UtilityClass.CustomJsonConverter RenderJsonConverter() => new() {
            ConverterClassName = "DisplayDataBatchUpdateCommandConverter",
            ConverterClassDeclaring = $$"""
                class DisplayDataBatchUpdateCommandConverter : JsonConverter<{{DataClassForDisplay.BASE_CLASS_NAME}}> {
                    public override {{DataClassForDisplay.BASE_CLASS_NAME}}? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                        using var jsonDocument = JsonDocument.ParseValue(ref reader);
                        var dataType = jsonDocument.RootElement.GetProperty("{{DATA_TYPE_TS}}").GetString();
                        var value = jsonDocument.RootElement.GetProperty("{{VALUES_TS}}");

                {{_aggregates.Select(agg => new { Aggregate = agg, DataClass = new DataClassForDisplay(agg) }).SelectTextTemplate(x => $$"""
                        if (dataType == "{{GetDataTypeLiteral(x.Aggregate)}}") {
                            return JsonSerializer.Deserialize<{{x.DataClass.CsClassName}}>(value.GetRawText(), options)
                                ?? throw new InvalidOperationException($"パラメータを{{x.DataClass.CsClassName}}型に変換できません: {value.GetRawText()}"),
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
                /// <param name="parameter">一括更新内容</param>
                /// <param name="ignoreConfirm">「○○ですがよろしいですか？」などのコンファームを無視します。</param>
                [HttpPost("{{CONTROLLER_ACTION}}")]
                public virtual IActionResult ExecuteImmediately([FromBody] ReadModelsBatchUpdateParameter parameter, [FromQuery] bool ignoreConfirm) {
                    using var tran = _applicationService.DbContext.Database.BeginTransaction();
                    try {
                        var context = new {{BatchUpdateContext.CLASS_NAME}}(ignoreConfirm);
                        _applicationService.{{APPSRV_BATCH_UPDATE}}(parameter.{{HOOK_PARAM_ITEMS}}, context);
                
                        if (context.HasUserError) {
                            tran.Rollback();
                            return Problem($"一括更新に失敗しました。{Environment.NewLine}{string.Join(Environment.NewLine, errors2)}");
                        }
                        tran.Commit();
                        return Ok();
                
                    } catch (Exception ex) {
                        tran.Rollback();
                        return Problem(ex.ToString());
                    }
                }
                public partial class ReadModelsBatchUpdateParameter {
                    public List<{{DataClassForDisplay.BASE_CLASS_NAME}}> {{HOOK_PARAM_ITEMS}} { get; set; } = new();
                }
                #endregion 画面表示用データの一括更新
                """;
        }


        private string RenderAppSrvMethod(CodeRenderingContext context) {
            return $$"""
                /// <summary>
                /// データ一括更新を実行します。
                /// </summary>
                /// <param name="items">更新データ</param>
                /// <param name="saveContext">コンテキスト引数。エラーや警告の送出はこのオブジェクトを通して行なってください。</param>
                public virtual void {{APPSRV_BATCH_UPDATE}}(IEnumerable<{{DataClassForDisplay.BASE_CLASS_NAME}}> items, {{BatchUpdateContext.CLASS_NAME}} saveContext) {

                    // 画面表示用データを登録更新用データに変換
                    var unknownItems = items.ToArray();
                    var converted = new List<{{DataClassForSaveBase.SAVE_COMMAND_BASE}}>();
                    for (int i = 0; i < unknownItems.Length; i++) {
                        var item = unknownItems[i];
                {{_aggregates.SelectTextTemplate((agg, j) => $$"""
                        if (item is {{new DataClassForDisplay(agg).CsClassName}} item{{j}}) {
                            converted.AddRange({{APPSRV_CONVERT_DISP_TO_SAVE}}(item{{j}}));
                            continue;
                        }
                """)}}
                        saveContext.AddError(i, $"型 '{item?.GetType().FullName}' の登録更新用データへの変換処理が定義されていません。");
                    }

                    // 一括更新実行
                    {{BatchUpdate.APPSRV_METHOD}}(converted, saveContext);
                }

                {{_aggregates.SelectTextTemplate(agg => $$"""
                /// <summary>
                /// 画面表示用データを登録更新用データに変換します。
                /// </summary>
                /// <param name="displayData">画面表示用データ</param>
                /// <returns>登録更新用データ（複数）</returns>
                public virtual IEnumerable<{{DataClassForSaveBase.SAVE_COMMAND_BASE}}> {{APPSRV_CONVERT_DISP_TO_SAVE}}({{new DataClassForDisplay(agg).CsClassName}} displayData) {
                    // 変換処理は自動生成されません。
                    // このメソッドをオーバーライドしてデータ変換処理を記述してください。
                    yield break;
                }
                """)}}
                """;
        }
    }
}
