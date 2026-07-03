using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.CommandModelModules;
using Nijo.Models.QueryModelModules;
using Nijo.Parts.JavaScript;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Nijo.Parts.Common {
    /// <summary>
    /// カスタマイズ用のマッピングモジュール。
    /// JavaScript向けには、QueryModelやCommandModelの種類を表す文字列をキーにしてそれと対応するオブジェクトや関数を返すマッピング定義。
    /// C#向けには、QueryModelやCommandModelの種類を表すenum。
    /// </summary>
    internal class CommandQueryMappings : IMultiAggregateSourceFile {

        /// <summary>
        /// JavaScript用: DataModelの型名のリテラル型
        /// </summary>
        internal const string DATA_MODEL_TYPE = "DataModelType";
        /// <summary>
        /// JavaScript用: QueryModelの型名のリテラル型
        /// </summary>
        internal const string QUERY_MODEL_TYPE = "QueryModelType";
        /// <summary>
        /// JavaScript用: QueryModelのルート集約, Child, Children の集約名。
        /// 子孫集約の名前はルート集約からのスラッシュ区切り。
        /// </summary>
        internal const string QUERY_MODEL_TYPE_ALL = "QueryModelTypeAll";
        /// <summary>
        /// JavaScript用: ほかの集約から参照されているQueryModelの型名のリテラル型
        /// </summary>
        internal const string REFERED_QUERY_MODEL_TYPE = "ReferedQueryModelType";
        /// <summary>
        /// JavaScript用: CommandModelの型名のリテラル型
        /// </summary>
        internal const string COMMAND_MODEL_TYPE = "CommandModelType";
        /// <summary>
        /// JavaScript用: StructureModelの型名のリテラル型
        /// </summary>
        internal const string STRUCTURE_MODEL_TYPE = "StructureModelType";
        /// <summary>
        /// JavaScript用: StructureModelの編集用データの型名のリテラル型
        /// </summary>
        internal const string STRUCTURE_MODEL_DISPLAY_DATA_TYPE = "StructureModelDisplayDataType";
        /// <summary>
        /// C#用: QueryModel, CommandModelの種類を表すenum
        /// </summary>
        internal const string E_COMMAND_QUERY_TYPE = "E_CommandQueryType";

        private readonly Lock _lock = new();
        private readonly List<RootAggregate> _queryModels = [];
        private readonly List<RootAggregate> _commandModels = [];
        private readonly List<RootAggregate> _dataModels = [];
        private readonly List<RootAggregate> _structureModels = [];

        void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // ディープイコールのオプション型に依存しているので
            ctx.Use<DeepEqualFunction.OptionType>();
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
            var values = _queryModels.Concat(_commandModels).OrderByDataFlow(ctx);

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

            var dataModelsOrderByDataFlow = _dataModels.OrderByDataFlow(ctx).ToArray();
            var queryModelsOrderByDataFlow = _queryModels.OrderByDataFlow(ctx).ToArray();
            var commandModelsOrderByDataFlow = _commandModels.OrderByDataFlow(ctx).ToArray();
            var structureModelsOrderByDataFlow = _structureModels.OrderByDataFlow(ctx).ToArray();

            // QueryModelのルート集約だけでなくツリー全部
            var queryModelAggregateTypes = queryModelsOrderByDataFlow
                .SelectMany(x => x.EnumerateThisAndDescendants())
                .OrderBy(x => x.GetRoot().GetIndexOfDataFlow(ctx))
                .ThenBy(x => x.GetOrderInTree())
                .ToArray();

            // CommandModel のパラメータまたは戻り値に指定されている StructureModel の型名
            var parameterStructureModels = structureModelsOrderByDataFlow
                .Where(x => x.EnumerateCommandModelsRefferingAsParameter().Any()
                         || x.EnumerateCommandModelsRefferingAsReturnValue().Any())
                .ToArray();

            // Ref関連モジュールは他の集約から参照されているもののみ使用可能
            var referedRefEntires = new Dictionary<RootAggregate, DisplayDataRef.Entry[]>();
            foreach (var rootAggregate in queryModelsOrderByDataFlow) {
                var (refEntries, _) = DisplayDataRef.GetReferedMembersRecursively(rootAggregate);
                referedRefEntires[rootAggregate] = refEntries;
            }

            // import {} from "..." で他ファイルからインポートするモジュールを決める
            var imports = new List<(string ImportFrom, string[] Modules)>();
            foreach (var rootAggregate in queryModelsOrderByDataFlow) {
                var searchCondition = new SearchCondition.Entry(rootAggregate);
                var displayData = new DisplayData(rootAggregate);

                // ルート集約のモジュール
                var modules = new List<string> {
                    searchCondition.TsTypeName,
                    searchCondition.TsNewObjectFunction,
                    searchCondition.TypeScriptSortableMemberType,
                    searchCondition.GetTypeScriptSortableMemberType,
                    displayData.TsTypeName,
                    displayData.TsNewObjectFunction,
                    new DeepEqualFunction(displayData).FunctionName,
                };

                // 子孫集約のモジュール
                foreach (var child in rootAggregate.EnumerateDescendants()) {
                    var childDisplayData = new DisplayData(child);
                    modules.Add(childDisplayData.TsTypeName);
                    modules.Add(childDisplayData.TsNewObjectFunction);
                }

                // Ref関連モジュールは他から参照されているもののみを追加
                if (referedRefEntires.TryGetValue(rootAggregate, out var refEntries)) {
                    foreach (var entry in refEntries) {
                        modules.Add(entry.TsTypeName);
                        modules.Add(entry.TsNewObjectFunction);
                    }
                }

                imports.Add(($"./{rootAggregate.PhysicalName}", modules.ToArray()));
            }
            foreach (var rootAggregate in structureModelsOrderByDataFlow) {
                var structureRoot = new Models.StructureModelModules.PlainStructure(rootAggregate);
                var structureDisplayData = new Models.StructureModelModules.StructureDisplayData(rootAggregate);
                var modules = new List<string> {
                    structureRoot.TsTypeName,
                    structureRoot.TsNewObjectFunction,
                };
                if (parameterStructureModels.Contains(rootAggregate)) {
                    modules.Add(structureDisplayData.TsTypeName);
                    modules.Add(structureDisplayData.TsNewObjectFunction);
                    modules.Add(new DeepEqualFunction(structureDisplayData).FunctionName);
                }
                imports.Add(($"./{rootAggregate.PhysicalName}", modules.ToArray()));
            }

            return new SourceFile {
                FileName = "index.ts",
                Contents = $$"""
                    import * as Util from "./util"
                    {{imports.OrderBy(x => x.ImportFrom).SelectTextTemplate(x => $$"""
                    import { {{x.Modules.Join(", ")}} } from "{{x.ImportFrom}}"
                    """)}}

                    //#region Data,Command,Queryの種類の一覧

                    /** DataModelの種類の一覧。ルート集約のみ。 */
                    export type {{DATA_MODEL_TYPE}}
                    {{If(dataModelsOrderByDataFlow.Length == 0, () => $$"""
                      = never
                    """).Else(() => $$"""
                    {{dataModelsOrderByDataFlow.SelectTextTemplate((agg, i) => $$"""
                      {{(i == 0 ? "=" : "|")}} '{{agg.PhysicalName}}'
                    """)}}
                    """)}}

                    /** QueryModelの種類の一覧。ルート集約のみ。 */
                    export type {{QUERY_MODEL_TYPE}}
                    {{If(queryModelsOrderByDataFlow.Length == 0, () => $$"""
                      = never
                    """).Else(() => $$"""
                    {{queryModelsOrderByDataFlow.SelectTextTemplate((agg, i) => $$"""
                      {{(i == 0 ? "=" : "|")}} '{{agg.PhysicalName}}'
                    """)}}
                    """)}}

                    /** QueryModelのルート集約, Child, Children の集約名。 */
                    export type {{QUERY_MODEL_TYPE_ALL}}
                    {{If(queryModelAggregateTypes.Length == 0, () => $$"""
                      = never
                    """).Else(() => $$"""
                    {{queryModelAggregateTypes.SelectTextTemplate((agg, i) => $$"""
                      {{(i == 0 ? "=" : "|")}} '{{agg.EnumerateThisAndAncestors().Select(x => x.PhysicalName).Join("/")}}'
                    """)}}
                    """)}}

                    /** ほかの集約から参照されているQueryModelの種類の一覧 */
                    export type {{REFERED_QUERY_MODEL_TYPE}}
                    {{If(referedRefEntires.Values.SelectMany(x => x).Any(), () => $$"""
                    {{referedRefEntires.Values.SelectMany(x => x).OrderBy(x => x.CsClassName).SelectTextTemplate((refEntry, i) => $$"""
                      {{(i == 0 ? "=" : "|")}} '{{refEntry.Aggregate.RefEntryName}}'
                    """)}}
                    """).Else(() => $$"""
                      = never
                    """)}}

                    /** CommandModelの種類の一覧 */
                    export type {{COMMAND_MODEL_TYPE}}
                    {{If(commandModelsOrderByDataFlow.Length == 0, () => $$"""
                      = never
                    """).Else(() => $$"""
                    {{commandModelsOrderByDataFlow.SelectTextTemplate((agg, i) => $$"""
                      {{(i == 0 ? "=" : "|")}} '{{agg.PhysicalName}}'
                    """)}}
                    """)}}

                    /** StructureModelの種類の一覧 */
                    export type {{STRUCTURE_MODEL_TYPE}}
                    {{If(structureModelsOrderByDataFlow.Length == 0, () => $$"""
                      = never
                    """).Else(() => $$"""
                    {{structureModelsOrderByDataFlow.SelectTextTemplate((agg, i) => $$"""
                      {{(i == 0 ? "=" : "|")}} '{{agg.PhysicalName}}'
                    """)}}
                    """)}}

                    /** StructureModelの編集用データの種類の一覧 */
                    export type {{STRUCTURE_MODEL_DISPLAY_DATA_TYPE}}
                    {{If(parameterStructureModels.Length == 0, () => $$"""
                      = never
                    """).Else(() => $$"""
                    {{parameterStructureModels.SelectTextTemplate((agg, i) => $$"""
                      {{(i == 0 ? "=" : "|")}} '{{agg.PhysicalName}}'
                    """)}}
                    """)}}

                    /** DataModelの種類の一覧を文字列として返します。 */
                    export const getDataModelTypeList = (): {{DATA_MODEL_TYPE}}[] => [
                    {{dataModelsOrderByDataFlow.SelectTextTemplate((agg, i) => $$"""
                      '{{agg.PhysicalName}}',
                    """)}}
                    ]

                    /** QueryModelの種類の一覧を文字列として返します。 */
                    export const getQueryModelTypeList = (): {{QUERY_MODEL_TYPE}}[] => [
                    {{queryModelsOrderByDataFlow.SelectTextTemplate((agg, i) => $$"""
                      '{{agg.PhysicalName}}',
                    """)}}
                    ]

                    /** CommandModelの種類の一覧を文字列として返します。 */
                    export const getCommandModelTypeList = (): {{COMMAND_MODEL_TYPE}}[] => [
                    {{commandModelsOrderByDataFlow.SelectTextTemplate((agg, i) => $$"""
                      '{{agg.PhysicalName}}',
                    """)}}
                    ]

                    /** StructureModelの種類の一覧を文字列として返します。 */
                    export const getStructureModelTypeList = (): {{STRUCTURE_MODEL_TYPE}}[] => [
                    {{structureModelsOrderByDataFlow.SelectTextTemplate((agg, i) => $$"""
                      '{{agg.PhysicalName}}',
                    """)}}
                    ]
                    //#endregion Data,Command,Queryの種類の一覧


                    //#region DisplayData
                    /** 画面表示用データ */
                    export namespace DisplayData {
                      /** DisplayData型一覧 */
                      export interface TypeMap {
                    {{queryModelAggregateTypes.SelectTextTemplate(agg => $$"""
                        '{{agg.EnumerateThisAndAncestors().Select(x => x.PhysicalName).Join("/")}}': {{new DisplayData(agg).TsTypeName}}
                    """)}}
                      }
                      /** DisplayData新規作成関数 */
                      export const create: { [K in {{QUERY_MODEL_TYPE_ALL}}]: (() => TypeMap[K]) } = {
                    {{queryModelAggregateTypes.SelectTextTemplate(agg => $$"""
                        '{{agg.EnumerateThisAndAncestors().Select(x => x.PhysicalName).Join("/")}}': {{new DisplayData(agg).TsNewObjectFunction}},
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
                        '{{refEntry.Aggregate.RefEntryName}}': {{refEntry.TsTypeName}}
                    """)}}
                      }
                      /** RefTarget新規作成関数 */
                      export const create: { [K in {{REFERED_QUERY_MODEL_TYPE}}]: (() => TypeMap[K]) } = {
                    {{referedRefEntires.Values.SelectMany(x => x).SelectTextTemplate(refEntry => $$"""
                        '{{refEntry.Aggregate.RefEntryName}}': {{refEntry.TsNewObjectFunction}},
                    """)}}
                      }
                    }
                    //#endregion RefTarget


                    //#region SearchCondition
                    /** 検索条件 */
                    export namespace SearchCondition {
                      /** SearchCondition型一覧 */
                      export interface TypeMap {
                    {{queryModelsOrderByDataFlow.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new SearchCondition.Entry(agg).TsTypeName}}
                    """)}}
                      }
                      /** SearchCondition新規作成関数 */
                      export const create: { [K in {{QUERY_MODEL_TYPE}}]: (() => TypeMap[K]) } = {
                    {{queryModelsOrderByDataFlow.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new SearchCondition.Entry(agg).TsNewObjectFunction}},
                    """)}}
                      }
                      /** ソート可能メンバーの型（「昇順」「降順」抜き） */
                      export interface SortableMemberTypeMap {
                    {{queryModelsOrderByDataFlow.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new SearchCondition.Entry(agg).TypeScriptSortableMemberType}}
                    """)}}
                      }
                      /** ソート可能メンバー一覧取得関数 */
                      export const getSortableMembers: { [K in {{QUERY_MODEL_TYPE}}]: (() => string[]) } = {
                    {{queryModelsOrderByDataFlow.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new SearchCondition.Entry(agg).GetTypeScriptSortableMemberType}},
                    """)}}
                      }
                    }
                    //#endregion SearchCondition


                    //#region Commandパラメータ
                    /** Commandパラメータ */
                    export namespace CommandParam {
                      /** Commandパラメータ型一覧 */
                      export interface TypeMap {
                    {{commandModelsOrderByDataFlow.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{agg.GetParameterStructure()?.TsTypeName ?? "Record<string, never> // 引数なし"}}
                    """)}}
                      }
                      /** Commandパラメータ新規作成関数 */
                      export const create: { [K in {{COMMAND_MODEL_TYPE}}]: (() => TypeMap[K]) } = {
                    {{commandModelsOrderByDataFlow.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{agg.GetParameterStructure()?.TsNewObjectFunction ?? "() => ({ /* 引数なし */ })"}},
                    """)}}
                      }
                    }
                    //#endregion Commandパラメータ


                    //#region Command戻り値
                    /** Command戻り値 */
                    export namespace CommandReturnValue {
                      /** Command戻り値型一覧 */
                      export interface TypeMap {
                    {{commandModelsOrderByDataFlow.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{agg.GetReturnValueStructure()?.TsTypeName ?? "Record<string, never> // 戻り値なし"}}
                    """)}}
                      }
                      /** Command戻り値新規作成関数 */
                      export const create: { [K in {{COMMAND_MODEL_TYPE}}]: (() => TypeMap[K]) } = {
                    {{commandModelsOrderByDataFlow.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{agg.GetReturnValueStructure()?.TsNewObjectFunction ?? "() => ({ /* 戻り値なし */ })"}},
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
                    {{SearchProcessing.RenderTsTypeMap(queryModelsOrderByDataFlow)}}
                    //#endregion 検索


                    //#region 参照検索
                    {{SearchProcessingRefs.RenderTsTypeMap(referedRefEntires.Values.SelectMany(x => x))}}
                    //#endregion 参照検索


                    //#region コマンド
                    {{CommandProcessing.RenderTsTypeMap(commandModelsOrderByDataFlow)}}
                    //#endregion コマンド


                    //#region StructureModel
                    /** StructureModel */
                    export namespace StructureModel {
                      /** StructureModel型一覧 */
                      export interface TypeMap {
                    {{structureModelsOrderByDataFlow.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new Models.StructureModelModules.PlainStructure(agg).TsTypeName}}
                    """)}}
                      }
                      /** StructureModel新規作成関数 */
                      export const create: { [K in {{STRUCTURE_MODEL_TYPE}}]: (() => TypeMap[K]) } = {
                    {{structureModelsOrderByDataFlow.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new Models.StructureModelModules.PlainStructure(agg).TsNewObjectFunction}},
                    """)}}
                      }
                    }
                    /** StructureModel（編集用） */
                    export namespace StructureModelDisplayData {
                      /** StructureModel型一覧 */
                      export interface TypeMap {
                    {{parameterStructureModels.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new Models.StructureModelModules.StructureDisplayData(agg).TsTypeName}}
                    """)}}
                      }
                      /** StructureModel新規作成関数 */
                      export const create: { [K in {{STRUCTURE_MODEL_DISPLAY_DATA_TYPE}}]: (() => TypeMap[K]) } = {
                    {{parameterStructureModels.SelectTextTemplate(agg => $$"""
                        '{{agg.PhysicalName}}': {{new Models.StructureModelModules.StructureDisplayData(agg).TsNewObjectFunction}},
                    """)}}
                      }
                    }
                    //#endregion StructureModel


                    //#region ディープイコール関数
                    {{DeepEqualFunction.JSDOC}}
                    export const deepEqualFunction: {
                      [K in {{QUERY_MODEL_TYPE}} | {{STRUCTURE_MODEL_DISPLAY_DATA_TYPE}}]: (
                        left: K extends {{QUERY_MODEL_TYPE}}
                          ? DisplayData.TypeMap[K]
                          : K extends {{STRUCTURE_MODEL_DISPLAY_DATA_TYPE}}
                          ? StructureModelDisplayData.TypeMap[K]
                          : never,
                        right: K extends {{QUERY_MODEL_TYPE}}
                          ? DisplayData.TypeMap[K]
                          : K extends {{STRUCTURE_MODEL_DISPLAY_DATA_TYPE}}
                          ? StructureModelDisplayData.TypeMap[K]
                          : never,
                        option?: Util.{{DeepEqualFunction.OptionType.TYPENAME}}
                      ) => boolean
                    } = {
                    {{queryModelsOrderByDataFlow.SelectTextTemplate(agg => $$"""
                      '{{agg.PhysicalName}}': {{new DeepEqualFunction(new DisplayData(agg)).FunctionName}},
                    """)}}
                    {{parameterStructureModels.SelectTextTemplate(agg => $$"""
                      '{{agg.PhysicalName}}': {{new DeepEqualFunction(new Models.StructureModelModules.StructureDisplayData(agg)).FunctionName}},
                    """)}}
                    }
                    //#endregion ディープイコール関数
                    """,
            };
        }

        internal CommandQueryMappings AddQueryModel(RootAggregate rootAggregate) {
            lock (_lock) {
                _queryModels.Add(rootAggregate);
                return this;
            }
        }
        internal CommandQueryMappings AddCommandModel(RootAggregate rootAggregate) {
            lock (_lock) {
                _commandModels.Add(rootAggregate);
                return this;
            }
        }
        internal CommandQueryMappings AddDataModel(RootAggregate rootAggregate) {
            lock (_lock) {
                _dataModels.Add(rootAggregate);
                return this;
            }
        }
        internal CommandQueryMappings AddStructureModel(RootAggregate rootAggregate) {
            lock (_lock) {
                _structureModels.Add(rootAggregate);
                return this;
            }
        }
    }
}
