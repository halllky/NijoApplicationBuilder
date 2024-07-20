using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// <see cref="DataClassForSave"/> の、C#側の基底クラスと、それと対応するTypeScript側の型
    /// </summary>
    internal class DataClassForSaveBase : ISummarizedFile {

        private readonly List<GraphNode<Aggregate>> _aggregates = new();
        internal void Register(GraphNode<Aggregate> aggregate) {
            _aggregates.Add(aggregate);
        }

        internal const string SAVE_COMMAND_BASE = "SaveCommandBase";
        internal const string CREATE_COMMAND = "CreateCommand";
        internal const string UPDATE_COMMAND = "UpdateCommand";
        internal const string DELETE_COMMAND = "DeleteCommand";
        internal const string NO_OPERATION = "NoOperation";
        internal const string TS_SAVE_COMMAND = "BatchUpdateParameter";

        /// <summary>データ本体のプロパティの名前（C#側）</summary>
        internal const string VALUES_CS = "Values";
        /// <summary>データ本体のプロパティの名前（TypeScript側）</summary>
        internal const string VALUES_TS = "values";
        /// <summary>楽観排他制御用のバージョニング情報をもつプロパティの名前（C#側）</summary>
        internal const string VERSION_CS = "Version";
        /// <summary>楽観排他制御用のバージョニング情報をもつプロパティの名前（TypeScript側）</summary>
        internal const string VERSION_TS = "version";

        /// <summary>追加・更新・削除のいずれかを表す区分のプロパティ名（C#側）</summary>
        internal const string ADD_MOD_DEL_CS = "AddOrModOrDel";
        /// <summary>追加・更新・削除のいずれかを表す区分プロパティ名（TypeScript側）</summary>
        internal const string ADD_MOD_DEL_TS = "addOrModOrDel";
        /// <summary>追加・更新・削除のいずれかを表す区分のenum名（C#側）</summary>
        internal const string ADD_MOD_DEL_ENUM_CS = "E_AddOrModOrDel";
        /// <summary>追加・更新・削除のいずれかを表す区分の型名（TypeScript側）</summary>
        internal const string ADD_MOD_DEL_ENUM_TS = "AddOrModOrDelType";

        /// <summary>データ種別のプロパティ名（C#側）</summary>
        internal const string DATA_TYPE_CS = "DataType";
        /// <summary>データ種別のプロパティ名（TypeScript側）</summary>
        internal const string DATA_TYPE_TS = "dataType";
        /// <summary>データ種別の列挙体名（C#側）</summary>
        internal const string DATA_TYPE_ENUM_CS = "E_UpdateDataType";
        /// <summary>データ種別の列挙体名（TypeScript側）</summary>
        internal const string DATA_TYPE_ENUM_TS = "UpdateDataType";
        /// <summary>データ種別の値を返します（C#, TypeScript共通）</summary>
        internal static string GetEnumValueOf(GraphNode<Aggregate> aggregate) {
            return aggregate.Item.DisplayName.ToCSharpSafe();
        }


        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {

            // 基底クラス
            context.CoreLibrary.UtilDir(dir => {
                dir.Generate(RenderCSharpBaseClass());
            });
            context.ReactProject.Types.Add(RenderTypeScriptType());

            // JSONコンバータ
            context.UseSummarizedFile<Parts.Utility.UtilityClass>().AddJsonConverter(RenderJsonDeserialization());

            // データ種別列挙体
            context.CoreLibrary.Enums.Add(RenderDataTypeEnumCs());
            context.ReactProject.Types.Add(RenderDataTypeEnumTs());

            // 追加更新削除区分
            context.CoreLibrary.Enums.Add(RenderAddModDelEnum());
            context.ReactProject.Types.Add(RenderAddModDelType());
        }

        /// <summary>
        /// C#側のデータ構造定義クラスの基底クラス
        /// </summary>
        private static SourceFile RenderCSharpBaseClass() => new SourceFile {
            FileName = "SaveCommandBase.cs",
            RenderContent = context => {
                return $$"""
                    using System.Text.Json.Serialization;

                    namespace {{context.Config.RootNamespace}};

                    /// <summary>
                    /// なんらかの集約の新規登録・更新・削除のいずれかのパラメータ。
                    /// 一括更新のWebAPIでは多種多様な種類のオブジェクトが一気に送られてくるので
                    /// どの集約の更新なのか、新規登録・更新・削除のどれなのかが混在している。
                    /// JSONからのデシリアライズ時、オブジェクトに付随している区分値を参照してそれらのうちどれなのかを判断する。
                    /// </summary>
                    public abstract class {{SAVE_COMMAND_BASE}} {
                    }

                    /// <summary>
                    /// 新規データ登録パラメータ
                    /// </summary>
                    public partial class {{CREATE_COMMAND}}<T> : {{SAVE_COMMAND_BASE}} {
                        /// <summary>新規登録内容</summary>
                        [JsonPropertyName("{{VALUES_TS}}")]
                        public required T {{VALUES_CS}} { get; init; }
                    }
                    /// <summary>
                    /// 既存データ更新パラメータ
                    /// </summary>
                    public partial class {{UPDATE_COMMAND}}<T> : {{SAVE_COMMAND_BASE}} {
                        /// <summary>更新内容</summary>
                        [JsonPropertyName("{{VALUES_TS}}")]
                        public required T {{VALUES_CS}} { get; init; }
                        /// <summary>楽観排他制御用のバージョニング情報</summary>
                        [JsonPropertyName("{{VERSION_TS}}")]
                        public required int {{VERSION_CS}} { get; init; }
                    }
                    /// <summary>
                    /// 既存データ削除パラメータ
                    /// </summary>
                    public partial class {{DELETE_COMMAND}}<T> : {{SAVE_COMMAND_BASE}} {
                        /// <summary>削除内容</summary>
                        [JsonPropertyName("{{VALUES_TS}}")]
                        public required T {{VALUES_CS}} { get; init; }
                        /// <summary>楽観排他制御用のバージョニング情報</summary>
                        [JsonPropertyName("{{VERSION_TS}}")]
                        public required int {{VERSION_CS}} { get; init; }
                    }
                    /// <summary>
                    /// 既存データを更新も削除もしない
                    /// </summary>
                    public partial class {{NO_OPERATION}}<T> : {{SAVE_COMMAND_BASE}} {
                        /// <summary>データ内容</summary>
                        [JsonPropertyName("{{VALUES_TS}}")]
                        public required T {{VALUES_CS}} { get; init; }
                        /// <summary>楽観排他制御用のバージョニング情報</summary>
                        [JsonPropertyName("{{VERSION_TS}}")]
                        public required int {{VERSION_CS}} { get; init; }
                    }
                    """;
            },
        };

        private string RenderTypeScriptType() {
            return $$"""
                /**
                 * 一括更新のパラメータ。
                 * どの種類のデータの更新なのかの情報や、新規登録・更新・削除のいずれかの区分をもつ。
                 */
                export type {{TS_SAVE_COMMAND}}
                  = (CreateCommandMetadata & CreateCommandItem)
                  | (SaveCommandMetadata & SaveCommandItem)

                type CreateCommandMetadata = {
                  /** 追加・更新・削除のいずれかを表す区分 */
                  {{ADD_MOD_DEL_TS}}: 'ADD'
                  /** 楽観排他制御用のバージョニング情報。新規登録の場合はundefined。更新削除の場合は更新前のバージョン */
                  {{VERSION_TS}}?: undefined
                }
                type SaveCommandMetadata = {
                  /** 追加・更新・削除のいずれかを表す区分 */
                  {{ADD_MOD_DEL_TS}}: 'MOD' | 'DEL' | 'NONE'
                  /** 楽観排他制御用のバージョニング情報。新規登録の場合はundefined。更新削除の場合は更新前のバージョン */
                  {{VERSION_TS}}: number
                }

                {{If(_aggregates.Count == 0, () => $$"""
                type CreateCommandItem = never
                """).Else(() => $$"""
                type CreateCommandItem
                {{_aggregates.Select(agg => new { dataType = GetEnumValueOf(agg), dataClass = new DataClassForSave(agg, DataClassForSave.E_Type.Create) }).SelectTextTemplate((x, i) => $$"""
                  {{(i == 0 ? "=" : "|")}} { {{DATA_TYPE_TS}}: '{{x.dataType}}', {{VALUES_TS}}: {{x.dataClass.TsTypeName}} }
                """)}}
                """)}}

                {{If(_aggregates.Count == 0, () => $$"""
                type SaveCommandItem = never
                """).Else(() => $$"""
                type SaveCommandItem
                {{_aggregates.Select(agg => new { dataType = GetEnumValueOf(agg), dataClass = new DataClassForSave(agg, DataClassForSave.E_Type.UpdateOrDelete) }).SelectTextTemplate((x, i) => $$"""
                  {{(i == 0 ? "=" : "|")}} { {{DATA_TYPE_TS}}: '{{x.dataType}}', {{VALUES_TS}}: {{x.dataClass.TsTypeName}} }
                """)}}
                """)}}
                """;
        }


        /// <summary>
        /// クライアント側からTypeScriptの型定義で送られてきたJSONをC#側のクラスに変換する
        /// </summary>
        private Parts.Utility.UtilityClass.CustomJsonConverter RenderJsonDeserialization() => new Parts.Utility.UtilityClass.CustomJsonConverter {
            ConverterClassName = "SaveCommandBaseConverter",
            ConverterClassDeclaring = $$"""
                class SaveCommandBaseConverter : JsonConverter<{{SAVE_COMMAND_BASE}}> {
                    public override {{SAVE_COMMAND_BASE}}? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                        using var jsonDocument = JsonDocument.ParseValue(ref reader);
                        var dataType = jsonDocument.RootElement.GetProperty("{{DATA_TYPE_TS}}").GetString();
                        var addOrModOrDel = jsonDocument.RootElement.GetProperty("{{ADD_MOD_DEL_TS}}").GetString();
                        var value = jsonDocument.RootElement.GetProperty("{{VALUES_TS}}");

                {{_aggregates.Select(agg => new { Aggregate = agg, Create = new DataClassForSave(agg, DataClassForSave.E_Type.Create), Upd = new DataClassForSave(agg, DataClassForSave.E_Type.UpdateOrDelete) }).SelectTextTemplate(x => $$"""
                        if (dataType == "{{GetEnumValueOf(x.Aggregate)}}") {
                            if (addOrModOrDel == "ADD") {
                                return new {{CREATE_COMMAND}}<{{x.Create.CsClassName}}> {
                                    {{VALUES_CS}} = JsonSerializer.Deserialize<{{x.Create.CsClassName}}>(value.GetRawText(), options)
                                        ?? throw new InvalidOperationException($"パラメータを{{x.Create.CsClassName}}型に変換できません: {value.GetRawText()}"),
                                };
                            } else if (addOrModOrDel == "MOD") {
                                return new {{UPDATE_COMMAND}}<{{x.Upd.CsClassName}}> {
                                    {{VALUES_CS}} = JsonSerializer.Deserialize<{{x.Upd.CsClassName}}>(value.GetRawText(), options)
                                        ?? throw new InvalidOperationException($"パラメータを{{x.Upd.CsClassName}}型に変換できません: {value.GetRawText()}"),
                                    {{VERSION_CS}} = jsonDocument.RootElement.GetProperty("{{VERSION_TS}}").GetInt32(),
                                };
                            } else if (addOrModOrDel == "DEL") {
                                return new {{DELETE_COMMAND}}<{{x.Upd.CsClassName}}> {
                                    {{VALUES_CS}} = JsonSerializer.Deserialize<{{x.Upd.CsClassName}}>(value.GetRawText(), options)
                                        ?? throw new InvalidOperationException($"パラメータを{{x.Upd.CsClassName}}型に変換できません: {value.GetRawText()}"),
                                    {{VERSION_CS}} = jsonDocument.RootElement.GetProperty("{{VERSION_TS}}").GetInt32(),
                                };
                            } else if (addOrModOrDel == "NONE") {
                                return new {{NO_OPERATION}}<{{x.Upd.CsClassName}}> {
                                    {{VALUES_CS}} = JsonSerializer.Deserialize<{{x.Upd.CsClassName}}>(value.GetRawText(), options)
                                        ?? throw new InvalidOperationException($"パラメータを{{x.Upd.CsClassName}}型に変換できません: {value.GetRawText()}"),
                                    {{VERSION_CS}} = jsonDocument.RootElement.GetProperty("{{VERSION_TS}}").GetInt32(),
                                };
                            }
                        }
                """)}}

                        throw new InvalidOperationException($"更新パラメータの種別を認識できません: {jsonDocument.RootElement.GetRawText()}");
                    }

                    public override void Write(Utf8JsonWriter writer, {{SAVE_COMMAND_BASE}}? value, JsonSerializerOptions options) {
                        JsonSerializer.Serialize(writer, value, options);
                    }
                }
                """,
        };


        #region データ種別
        /// <summary>
        /// データ種別の列挙体（C#）
        /// </summary>
        private string RenderDataTypeEnumCs() {
            return $$"""
                /// <summary>一括更新の際に必要になる、そのオブジェクトがどの集約のデータの更新なのかを示す種別名</summary>
                public enum {{DATA_TYPE_ENUM_CS}} {
                {{_aggregates.SelectTextTemplate(agg => $$"""
                    {{GetEnumValueOf(agg)}},
                """)}}
                }
                """;
        }

        /// <summary>
        /// データ種別の列挙体（TypeScript）
        /// </summary>
        private string RenderDataTypeEnumTs() {
            if (_aggregates.Count == 0) {
                return $$"""
                    /** 一括更新の際に必要になる、そのオブジェクトがどの集約のデータの更新なのかを示す種別名 */
                    export type {{DATA_TYPE_ENUM_TS}} = never
                    """;

            } else {
                return $$"""
                    /** 一括更新の際に必要になる、そのオブジェクトがどの集約のデータの更新なのかを示す種別名 */
                    export type {{DATA_TYPE_ENUM_TS}}
                    {{_aggregates.SelectTextTemplate((agg, i) => $$"""
                      {{(i == 0 ? "=" : "|")}} '{{GetEnumValueOf(agg)}}'
                    """)}}
                    """;

            }
        }
        #endregion データ種別


        #region 追加更新削除区分
        /// <summary>
        /// 追加・更新・削除のいずれかを表す区分（C#側）の定義をレンダリングします。
        /// </summary>
        private static string RenderAddModDelEnum() {
            return $$"""
                /// <summary>追加・更新・削除のいずれかを表す区分</summary>
                public enum {{ADD_MOD_DEL_ENUM_CS}} {
                    /// <summary>新規追加</summary>
                    ADD,
                    /// <summary>更新</summary>
                    MOD,
                    /// <summary>削除</summary>
                    DEL,
                    /// <summary>変更なし</summary>
                    NONE,
                }
                """;
        }
        /// <summary>
        /// 追加・更新・削除のいずれかを表す区分（TypeScript側）の定義をレンダリングします。
        /// </summary>
        private static string RenderAddModDelType() {
            return $$"""
                /** 追加・更新・削除のいずれかを表す区分 */
                export type {{ADD_MOD_DEL_ENUM_TS}}
                  = 'ADD'  // 新規追加
                  | 'MOD'  // 更新
                  | 'DEL'  // 削除
                  | 'NONE' // 変更なし
                """;
        }
        #endregion 追加更新削除区分
    }
}
