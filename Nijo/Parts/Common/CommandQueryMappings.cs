using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.CommandModelModules;
using Nijo.Models.DataModelModules;
using Nijo.Models.QueryModelModules;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nijo.Parts.Common {
    /// <summary>
    /// カスタマイズ用のマッピングモジュール。
    /// JavaScript向けには、QueryModelやCommandModelの種類を表す文字列をキーにしてそれと対応するオブジェクトや関数を返すマッピング定義。
    /// C#向けには、QueryModelやCommandModelの種類を表すenum。
    /// </summary>
    internal class CommandQueryMappings : IMultiAggregateSourceFile {

        /// <summary>
        /// JavaScript用: QueryModelの型名のリテラル型
        /// </summary>
        internal const string QUERY_MODEL_TYPE = "QueryModelType";
        /// <summary>
        /// JavaScript用: ほかの集約から参照されているQueryModelの型名のリテラル型
        /// </summary>
        internal const string REFERED_QUERY_MODEL_TYPE = "ReferedQueryModelType";
        /// <summary>
        /// JavaScript用: 一括更新処理が存在するQueryModelの型名のリテラル型
        /// </summary>
        internal const string BATCH_UPDATABLE_QUERY_MODEL_TYPE = "BatchUpdatableQueryModelType";
        /// <summary>
        /// JavaScript用: CommandModelの型名のリテラル型
        /// </summary>
        internal const string COMMAND_MODEL_TYPE = "CommandModelType";
        /// <summary>
        /// C#用: QueryModel, CommandModelの種類を表すenum
        /// </summary>
        internal const string E_COMMAND_QUERY_TYPE = "E_CommandQueryType";

        internal CommandQueryMappings AddQueryModel(RootAggregate rootAggregate) {
            _queryModels.Add(rootAggregate);
            return this;
        }
        internal CommandQueryMappings AddCommandModel(RootAggregate rootAggregate) {
            _commandModels.Add(rootAggregate);
            return this;
        }
        internal CommandQueryMappings AddDataModel(RootAggregate rootAggregate) {
            _dataModels.Add(rootAggregate);
            return this;
        }
        private readonly List<RootAggregate> _queryModels = [];
        private readonly List<RootAggregate> _commandModels = [];
        private readonly List<RootAggregate> _dataModels = [];

        void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // 特になし
        }

        void IMultiAggregateSourceFile.Render(CodeRenderingContext ctx) {
            ctx.CoreLibrary(dir => {
                dir.Directory("Util", utilDir => {
                    utilDir.Generate(RenderCSharp(ctx));
                });
            });
            ctx.ReactProject(dir => {
                dir.Generate(RenderTypeScript(ctx));
            });
        }

        private SourceFile RenderCSharp(CodeRenderingContext ctx) {
            var values = _queryModels.Concat(_commandModels);

            return new SourceFile {
                FileName = "E_CommandQueryType.cs",
                Contents = $$"""
                    using System.ComponentModel.DataAnnotations;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// CommandModel, QueryModel の種類を表すenum
                    /// </summary>
                    public enum {{E_COMMAND_QUERY_TYPE}} {
                    {{values.SelectTextTemplate(agg => $$"""
                        [Display(Name = "{{agg.DisplayName.Replace("\"", "\\\"")}}")]
                        {{agg.PhysicalName}},
                    """)}}
                    }
                    """,
            };
        }

        private SourceFile RenderTypeScript(CodeRenderingContext ctx) {

            // 一括更新処理可能なQueryModel
            var batchUpdatableQueryModels = _dataModels
                .Where(root => root.GenerateBatchUpdateCommand)
                .ToArray();

            // Ref関連モジュールは他の集約から参照されているもののみ使用可能
            var referedRefEntires = new Dictionary<RootAggregate, DisplayDataRef.Entry[]>();
            foreach (var rootAggregate in _queryModels) {
                var (refEntries, _) = DisplayDataRef.GetReferedMembersRecursively(rootAggregate);
                referedRefEntires[rootAggregate] = refEntries;
            }

            // import {} from "..." で他ファイルからインポートするモジュールを決める
            var imports = new List<(string ImportFrom, string[] Modules)>();
            foreach (var rootAggregate in _queryModels) {
                var searchCondition = new SearchCondition.Entry(rootAggregate);
                var displayData = new DisplayData(rootAggregate);

                var modules = new List<string> {
                    searchCondition.TsTypeName,
                    searchCondition.TsNewObjectFunction,
                    displayData.TsTypeName,
                    displayData.TsNewObjectFunction,
                    displayData.PkExtractFunctionName,
                    displayData.PkAssignFunctionName,
                };

                // Ref関連モジュールは他から参照されているもののみを追加
                if (referedRefEntires.TryGetValue(rootAggregate, out var refEntries)) {
                    foreach (var entry in refEntries) {
                        modules.Add(entry.TsTypeName);
                        modules.Add(entry.TsNewObjectFunction);
                        modules.Add(entry.PkExtractFunctionName);
                        modules.Add(entry.PkAssignFunctionName);
                    }
                }

                imports.Add(($"./{rootAggregate.PhysicalName}", modules.ToArray()));
            }
            foreach (var rootAggregate in _commandModels) {
                var param = new ParameterOrReturnValue(rootAggregate, ParameterOrReturnValue.E_Type.Parameter);
                var returnType = new ParameterOrReturnValue(rootAggregate, ParameterOrReturnValue.E_Type.ReturnValue);

                imports.Add((
                    $"./{rootAggregate.PhysicalName}",
                    new[] {
                        param.TsTypeName,
                        param.TsNewObjectFunction,
                        returnType.TsTypeName,
                        returnType.TsNewObjectFunction,
                    }));
            }

            return new SourceFile {
                FileName = "index.ts",
                Contents = $$"""
                    import * as Util from "./util"
                    {{imports.OrderBy(x => x.ImportFrom).SelectTextTemplate(x => $$"""
                    import { {{x.Modules.Join(", ")}} } from "{{x.ImportFrom}}"
                    """)}}

                    //#region Command,Queryの種類の一覧

                    /** QueryModelの種類の一覧 */
                    export type {{QUERY_MODEL_TYPE}}
                    {{If(_queryModels.Count == 0, () => $$"""
                      = never
                    """).Else(() => $$"""
                    {{_queryModels.SelectTextTemplate((agg, i) => $$"""
                      {{(i == 0 ? "=" : "|")}} '{{agg.PhysicalName}}'
                    """)}}
                    """)}}

                    /** ほかの集約から参照されているQueryModelの種類の一覧 */
                    export type {{REFERED_QUERY_MODEL_TYPE}}
                    {{If(referedRefEntires.Values.SelectMany(x => x).Any(), () => $$"""
                    {{referedRefEntires.Values.SelectMany(x => x).SelectTextTemplate((refEntry, i) => $$"""
                      {{(i == 0 ? "=" : "|")}} '{{refEntry.Aggregate.PhysicalName}}'
                    """)}}
                    """).Else(() => $$"""
                      = never
                    """)}}

                    /** 一括更新処理可能なQueryModelの種類の一覧 */
                    export type {{BATCH_UPDATABLE_QUERY_MODEL_TYPE}}
                    {{If(batchUpdatableQueryModels.Any(), () => $$"""
                    {{batchUpdatableQueryModels.SelectTextTemplate((dataModel, i) => $$"""
                      {{(i == 0 ? "=" : "|")}} '{{dataModel.PhysicalName}}'
                    """)}}
                    """).Else(() => $$"""
                      = never
                    """)}}

                    /** CommandModelの種類の一覧 */
                    export type {{COMMAND_MODEL_TYPE}}
                    {{If(_commandModels.Count == 0, () => $$"""
                      = never
                    """).Else(() => $$"""
                    {{_commandModels.SelectTextTemplate((agg, i) => $$"""
                      {{(i == 0 ? "=" : "|")}} '{{agg.PhysicalName}}'
                    """)}}
                    """)}}

                    /** CommandModel, QuerModel の種類の一覧 */
                    export type CommandOrQueryModelType = {{QUERY_MODEL_TYPE}} | {{COMMAND_MODEL_TYPE}}

                    /** QueryModelの種類の一覧を文字列として返します。 */
                    export const getQueryModelTypeList = (): {{QUERY_MODEL_TYPE}}[] => [
                    {{_queryModels.SelectTextTemplate((agg, i) => $$"""
                      '{{agg.PhysicalName}}',
                    """)}}
                    ]

                    /** CommandModelの種類の一覧を文字列として返します。 */
                    export const getCommandModelTypeList = (): {{COMMAND_MODEL_TYPE}}[] => [
                    {{_commandModels.SelectTextTemplate((agg, i) => $$"""
                      '{{agg.PhysicalName}}',
                    """)}}
                    ]

                    /** CommandModel, QuerModel の種類の一覧を文字列として返します。 */
                    export const getCommandOrQueryModelTypeList = (): CommandOrQueryModelType[] => [
                      ...getQueryModelTypeList(),
                      ...getCommandModelTypeList(),
                    ]
                    //#endregion Command,Queryの種類の一覧


                    //#region DisplayData
                    /** 画面表示用データ */
                    export namespace DisplayData {
                      /** DisplayData型一覧 */
                      export interface TypeMap {
                    {{_queryModels.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new DisplayData(agg).TsTypeName}}
                    """)}}
                      }
                      /** DisplayData新規作成関数 */
                      export const create: { [K in {{QUERY_MODEL_TYPE}}]: (() => TypeMap[K]) } = {
                    {{_queryModels.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new DisplayData(agg).TsNewObjectFunction}},
                    """)}}
                      }
                      /** 主キーの抽出関数 */
                      export const extractKeys: { [K in {{QUERY_MODEL_TYPE}}]: ((data: TypeMap[K]) => unknown[]) } = {
                    {{_queryModels.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new DisplayData(agg).PkExtractFunctionName}},
                    """)}}
                      }
                      /** 主キーの設定関数 */
                      export const assignKeys: { [K in {{QUERY_MODEL_TYPE}}]: ((data: TypeMap[K], keys: unknown[]) => void) } = {
                    {{_queryModels.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new DisplayData(agg).PkAssignFunctionName}} as (data: {{new DisplayData(agg).TsTypeName}}, keys: unknown[]) => void,
                    """)}}
                      }
                    }
                    //#endregion DisplayData

                    //#region RefTarget
                    /** 画面表示用データ（外部参照） */
                    export namespace RefTarget {
                      /** RefTarget型一覧 */
                      export interface TypeMap {
                    {{referedRefEntires.Values.SelectMany(x => x).SelectTextTemplate(refEntry => $$"""
                        '{{refEntry.Aggregate.PhysicalName}}': {{refEntry.TsTypeName}}
                    """)}}
                      }
                      /** RefTarget新規作成関数 */
                      export const create: { [K in {{REFERED_QUERY_MODEL_TYPE}}]: (() => TypeMap[K]) } = {
                    {{referedRefEntires.Values.SelectMany(x => x).SelectTextTemplate(refEntry => $$"""
                        '{{refEntry.Aggregate.PhysicalName}}': {{refEntry.TsNewObjectFunction}},
                    """)}}
                      }
                      /** 主キーの抽出関数 */
                      export const extractKeys: { [K in {{REFERED_QUERY_MODEL_TYPE}}]: ((data: TypeMap[K]) => unknown[]) } = {
                    {{referedRefEntires.Values.SelectMany(x => x).SelectTextTemplate(refEntry => $$"""
                        '{{refEntry.Aggregate.PhysicalName}}': {{refEntry.PkExtractFunctionName}},
                    """)}}
                      }
                      /** 主キーの設定関数 */
                      export const assignKeys: { [K in {{REFERED_QUERY_MODEL_TYPE}}]: ((data: TypeMap[K], keys: unknown[]) => void) } = {
                    {{referedRefEntires.Values.SelectMany(x => x).SelectTextTemplate(refEntry => $$"""
                        '{{refEntry.Aggregate.PhysicalName}}': {{refEntry.PkAssignFunctionName}} as (data: {{refEntry.TsTypeName}}, keys: unknown[]) => void,
                    """)}}
                      }
                    }
                    //#endregion RefTarget


                    //#region SearchCondition
                    /** 検索条件 */
                    export namespace SearchCondition {
                      /** SearchCondition型一覧 */
                      export interface TypeMap {
                    {{_queryModels.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new SearchCondition.Entry(agg).TsTypeName}}
                    """)}}
                      }
                      /** SearchCondition新規作成関数 */
                      export const create: { [K in {{QUERY_MODEL_TYPE}}]: (() => TypeMap[K]) } = {
                    {{_queryModels.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new SearchCondition.Entry(agg).TsNewObjectFunction}},
                    """)}}
                      }
                    }
                    //#endregion SearchCondition


                    //#region Commandパラメータ
                    /** Commandパラメータ */
                    export namespace CommandParam {
                      /** Commandパラメータ型一覧 */
                      export interface TypeMap {
                    {{_commandModels.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new ParameterOrReturnValue(agg, ParameterOrReturnValue.E_Type.Parameter).TsTypeName}}
                    """)}}
                      }
                      /** Commandパラメータ新規作成関数 */
                      export const create: { [K in {{COMMAND_MODEL_TYPE}}]: (() => TypeMap[K]) } = {
                    {{_commandModels.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new ParameterOrReturnValue(agg, ParameterOrReturnValue.E_Type.Parameter).TsNewObjectFunction}},
                    """)}}
                      }
                    }
                    //#endregion Commandパラメータ


                    //#region Command戻り値
                    /** Command戻り値 */
                    export namespace CommandReturnValue {
                      /** Command戻り値型一覧 */
                      export interface TypeMap {
                    {{_commandModels.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new ParameterOrReturnValue(agg, ParameterOrReturnValue.E_Type.ReturnValue).TsTypeName}}
                    """)}}
                      }
                      /** Command戻り値新規作成関数 */
                      export const create: { [K in {{COMMAND_MODEL_TYPE}}]: (() => TypeMap[K]) } = {
                    {{_commandModels.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new ParameterOrReturnValue(agg, ParameterOrReturnValue.E_Type.ReturnValue).TsNewObjectFunction}},
                    """)}}
                      }
                    }
                    //#endregion Command戻り値


                    //#region SearchConditionソート可能メンバー
                    // TODO ver.1
                    //#endregion SearchConditionソート可能メンバー

                    /** 一覧検索処理のパラメータ指定でメンバー名の後ろにこの文字列をつけるとサーバー側処理でソートしてくれる */
                    export type ASC_SUFFIX = '{{SearchCondition.ASC_SUFFIX}}'
                    /** 一覧検索処理のパラメータ指定でメンバー名の後ろにこの文字列をつけるとサーバー側処理でソートしてくれる */
                    export type DESC_SUFFIX = '{{SearchCondition.DESC_SUFFIX}}'


                    //#region 検索
                    {{SearchProcessing.RenderTsTypeMap(_queryModels)}}
                    //#endregion 検索


                    //#region 参照検索
                    {{SearchProcessingRefs.RenderTsTypeMap(referedRefEntires.Values.SelectMany(x => x))}}
                    //#endregion 参照検索


                    //#region コマンド
                    {{CommandProcessing.RenderTsTypeMap(_commandModels)}}
                    //#endregion コマンド


                    //#region DataModel一括更新
                    {{BatchUpdate.RenderTsTypeMap(_dataModels)}}
                    //#endregion DataModel一括更新


                    //#region 画面遷移用フック（MultiView）
                    // TODO ver.1
                    //#endregion 画面遷移用フック（MultiView）

                    //#region 画面遷移用フック（SingleView）
                    // TODO ver.1
                    //#endregion 画面遷移用フック（SingleView）

                    //#region URLから検索条件オブジェクトへの変換
                    // TODO ver.1
                    //#endregion URLから検索条件オブジェクトへの変換

                    //#region 検索条件オブジェクトからURLへの変換
                    // TODO ver.1
                    //#endregion 検索条件オブジェクトからURLへの変換

                    //#region ディープイコール関数
                    // TODO ver.1
                    //#endregion ディープイコール関数

                    //#region UI制約型一覧
                    // TODO ver.1
                    //#endregion UI制約型一覧
                    """,
            };
        }
    }
}
