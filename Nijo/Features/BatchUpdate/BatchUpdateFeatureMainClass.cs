using Nijo.Core;
using Nijo.Features.Storing;
using Nijo.Parts.Utility;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.BatchUpdate {
    partial class BatchUpdateFeature {

        private static SourceFile RenderMainClass() {
            return new SourceFile {
                FileName = "BatchUpdateFeature.cs",
                RenderContent = context => {
                    var appSrv = new ApplicationService();
                    var aggregates = GetAvailableAggregatesOrderByDataFlow(context)
                        .Select(agg => new {
                            Aggregate = agg,
                            agg.Item.DisplayName,
                            agg.Item.PhysicalName,
                            CreateCommand = new AggregateCreateCommand(agg).CsClassName,
                            DataClass = new DataClassForSave(agg).CsClassName,
                        })
                        .ToArray();

                    return $$"""
                        namespace {{context.Config.RootNamespace}} {

                            /// <summary>
                            /// 一括更新処理
                            /// </summary>
                            public class BatchUpdateFeature {
                                /// <summary>
                                /// 一括更新パラメータ
                                /// </summary>
                                public class Parameter {
                                    public List<ParameterItem> {{PARAM_ITEMS}} { get; set; } = new();
                                }

                                /// <summary>
                                /// 一括更新パラメータの更新データ1件
                                /// </summary>
                                public class ParameterItem {
                                    /// <summary>
                                    /// 更新データ種別名。以下のうちいずれか:
                                    /// {{aggregates.Select(a => a.DisplayName).Join(", ")}}
                                    /// </summary>
                                    public string? {{PARAM_DATATYPE}} { get; set; }
                                    /// <summary>
                                    /// 新規追加か更新か削除か
                                    /// </summary>
                                    public E_ActionType? {{PARAM_ACTION}} { get; set; }
                                    /// <summary>
                                    /// 更新データ。以下のクラスのうちいずれか:
                        {{aggregates.SelectTextTemplate(a => $$"""
                                    /// <see cref="{{a.DataClass}}"/>
                        """)}}
                                    /// </summary>
                                    public object? {{PARAM_DATA}} { get; set; }
                                }

                                public enum E_ActionType {
                                    /// <summary>新規作成</summary>
                                    {{ACTION_ADD}},
                                    /// <summary>更新</summary>
                                    {{ACTION_MODIFY}},
                                    /// <summary>削除</summary>
                                    {{ACTION_DELETE}},
                                }

                                /// <summary>
                                /// 一括更新パラメータの内容を検証し、実行コマンドのインスタンスを返します。
                                /// </summary>
                                public static bool TryCreate(Parameter parameter, out BatchUpdateFeature command, out ICollection<string> errors) {

                                    // パラメータを各種データクラスに変換してリストに格納する
                                    var allItems = new List<ParameterItem>(parameter.Items);
                                    errors = new List<string>();

                        {{aggregates.SelectTextTemplate(agg => $$"""
                                    var insert{{agg.PhysicalName}} = new List<{{agg.CreateCommand}}>();
                                    var update{{agg.PhysicalName}} = new List<{{agg.DataClass}}>();
                                    var delete{{agg.PhysicalName}} = new List<{{agg.DataClass}}>();
                        """)}}

                                    var i = 0;
                                    while (allItems.Count > 0) {
                                        var item = allItems.First();
                                        allItems.RemoveAt(0);

                        {{aggregates.SelectTextTemplate((agg, i) => $$"""
                                        {{(i == 0 ? "if " : "} else if ")}}(item.DataType == "{{agg.DisplayName}}") {
                                            if (item.Action == E_ActionType.ADD) {
                                                if (Util.{{UtilityClass.TRY_PARSE_AS_OBJECT_TYPE}}<{{agg.CreateCommand}}>(item.Data, out var parsed))
                                                    insert{{agg.PhysicalName}}.Add(parsed);
                                                else
                                                    errors.Add($"{i + 1}件目:\tパラメータを{{agg.DisplayName}}データとして解釈できません => '{item.Data?.{{UtilityClass.TO_JSON}}()}'");

                                            } else if (item.Action == E_ActionType.MOD) {
                                                if (Util.{{UtilityClass.TRY_PARSE_AS_OBJECT_TYPE}}<{{agg.DataClass}}>(item.Data, out var parsed))
                                                    update{{agg.PhysicalName}}.Add(parsed);
                                                else
                                                    errors.Add($"{i + 1}件目:\tパラメータを{{agg.DisplayName}}データとして解釈できません => '{item.Data?.{{UtilityClass.TO_JSON}}()}'");

                                            } else if (item.Action == E_ActionType.DEL) {
                                                if (Util.{{UtilityClass.TRY_PARSE_AS_OBJECT_TYPE}}<{{agg.DataClass}}>(item.Data, out var parsed))
                                                    delete{{agg.PhysicalName}}.Add(parsed);
                                                else
                                                    errors.Add($"{i + 1}件目:\tパラメータを{{agg.DisplayName}}データとして解釈できません => '{item.Data?.{{UtilityClass.TO_JSON}}()}'");

                                            } else {
                                                errors.Add($"{i + 1}件目:\t更新種別が不正です。");
                                            }

                        """)}}
                                        } else {
                                            errors.Add($"{i + 1}件目:\tデータ種別が不正です。");
                                        }

                                        i++;
                                    }

                                    command = new BatchUpdateFeature {
                        {{aggregates.SelectTextTemplate(agg => $$"""
                                        Insert{{agg.PhysicalName}} = insert{{agg.PhysicalName}},
                                        Update{{agg.PhysicalName}} = update{{agg.PhysicalName}},
                                        Delete{{agg.PhysicalName}} = delete{{agg.PhysicalName}},
                        """)}}
                                    };
                                    return errors.Count == 0;
                                }

                                /// <summary>
                                /// 複数の集約データを一括更新します。
                                /// トランザクションの開始と終了は行わないため、このメソッドを呼ぶ側で制御してください。
                                /// </summary>
                                public bool Execute({{appSrv.ClassName}} applicationService, out ICollection<string> errors) {
                                    // データ間の依存関係に注意しつつ順番に処理する。
                                    // 1. 依存する側のデータの削除
                                    // 2. 依存される側のデータの削除
                                    // 3. 依存される側のデータの更新
                                    // 4. 依存される側のデータの新規作成
                                    // 5. 依存する側のデータの更新
                                    // 6. 依存する側のデータの新規作成
                                    errors = new List<string>();
                                    ICollection<string> errors2;

                        {{aggregates.Reverse().SelectTextTemplate(agg => $$"""
                                    foreach (var item in Delete{{agg.PhysicalName}}) {
                                        if (!applicationService.{{new DeleteFeature(agg.Aggregate).MethodName}}(item, out errors2)) {
                                            foreach (var err in errors2) errors.Add(err);
                                        }
                                    }
                        """)}}
                        {{aggregates.SelectTextTemplate(agg => $$"""
                                    foreach (var item in Update{{agg.PhysicalName}}) {
                                        if (!applicationService.{{new UpdateFeature(agg.Aggregate).MethodName}}(item, out var _, out errors2)) {
                                            foreach (var err in errors2) errors.Add(err);
                                        }
                                    }
                                    foreach (var item in Insert{{agg.PhysicalName}}) {
                                        if (!applicationService.{{new CreateFeature(agg.Aggregate).MethodName}}(item, out var _, out errors2)) {
                                            foreach (var err in errors2) errors.Add(err);
                                        }
                                    }
                        """)}}

                                    return errors.Count == 0;
                                }

                        #pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
                                private BatchUpdateFeature() { }
                        #pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。

                        {{aggregates.SelectTextTemplate(agg => $$"""
                                private IReadOnlyList<{{agg.CreateCommand}}> Insert{{agg.PhysicalName}} { get; init; }
                                private IReadOnlyList<{{agg.DataClass}}> Update{{agg.PhysicalName}} { get; init; }
                                private IReadOnlyList<{{agg.DataClass}}> Delete{{agg.PhysicalName}} { get; init; }
                        """)}}
                            }
                        }
                        """;
                },
            };
        }
    }
}
