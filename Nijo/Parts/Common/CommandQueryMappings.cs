using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.CommandModelModules;
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
        private readonly List<RootAggregate> _queryModels = [];
        private readonly List<RootAggregate> _commandModels = [];

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

            // import {} from "..." で他ファイルからインポートするモジュールを決める
            var imports = new List<(string ImportFrom, string[] Modules)>();
            foreach (var rootAggregate in _queryModels) {
                var searchCondition = new SearchCondition.Entry(rootAggregate);
                var displayData = new DisplayData(rootAggregate);
                var refTarget = new DisplayDataRef.Entry(rootAggregate);

                imports.Add((
                    $"./{rootAggregate.PhysicalName}",
                    new[] {
                        searchCondition.TsTypeName,
                        searchCondition.TsNewObjectFunction,
                        displayData.TsTypeName,
                        displayData.TsNewObjectFunction,
                        refTarget.TsTypeName,
                        refTarget.TsNewObjectFunction,
                    }));
            }
            foreach (var rootAggregate in _commandModels) {
                var param = new ParameterType(rootAggregate);
                var returnType = new ReturnValue(rootAggregate);

                imports.Add((
                    $"./{rootAggregate.PhysicalName}",
                    new[] {
                        param.TsTypeName,
                        param.TsNewObjectFunction,
                        returnType.TsTypeName,
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


                    //#region QueryModelのモジュールの型マッピング
                    /** DisplayData型一覧 */
                    export interface DisplayDataTypeMap {
                    {{_queryModels.SelectTextTemplate(agg => $$"""
                      '{{agg.PhysicalName}}': {{new DisplayData(agg).TsTypeName}}
                    """)}}
                    }

                    /** RefTarget型一覧 */
                    export interface RefTargetTypeMap {
                    {{_queryModels.SelectTextTemplate(agg => $$"""
                      '{{agg.PhysicalName}}': {{new DisplayDataRef.Entry(agg).TsTypeName}}
                    """)}}
                    }

                    /** SearchCondition型一覧 */
                    export interface SearchConditionTypeMap {
                    {{_queryModels.SelectTextTemplate(agg => $$"""
                      '{{agg.PhysicalName}}': {{new SearchCondition.Entry(agg).TsTypeName}}
                    """)}}
                    }

                    /** Commandパラメータ型一覧 */
                    export interface CommandParamTypeMap {
                    {{_commandModels.SelectTextTemplate(agg => $$"""
                      '{{agg.PhysicalName}}': {{new ParameterType(agg).TsTypeName}}
                    """)}}
                    }
                    //#endregion QueryModelのモジュールの型マッピング


                    //#region オブジェクトの新規作成関数
                    /** DisplayData新規作成関数 */
                    export const createNewDisplayDataFunctions: { [K in {{QUERY_MODEL_TYPE}}]: (() => DisplayDataTypeMap[K]) } = {
                    {{_queryModels.SelectTextTemplate(agg => $$"""
                      '{{agg.PhysicalName}}': {{new DisplayData(agg).TsNewObjectFunction}},
                    """)}}
                    }

                    /** RefTarget新規作成関数 */
                    export const createNewRefTargetFunctions: { [K in {{QUERY_MODEL_TYPE}}]: (() => RefTargetTypeMap[K]) } = {
                    {{_queryModels.SelectTextTemplate(agg => $$"""
                      '{{agg.PhysicalName}}': {{new DisplayDataRef.Entry(agg).TsNewObjectFunction}},
                    """)}}
                    }

                    /** SearchCondition新規作成関数 */
                    export const createNewSearchConditionFunctions: {[K in {{QUERY_MODEL_TYPE}}]: (() => SearchConditionTypeMap[K]) } = {
                    {{_queryModels.SelectTextTemplate(agg => $$"""
                      '{{agg.PhysicalName}}': {{new SearchCondition.Entry(agg).TsNewObjectFunction}},
                    """)}}
                    }

                    /** Commandパラメータ新規作成関数 */
                    export const createNewCommandParamFunctions: { [K in {{COMMAND_MODEL_TYPE}}]: (() => CommandParamTypeMap[K]) } = {
                    {{_commandModels.SelectTextTemplate(agg => $$"""
                      '{{agg.PhysicalName}}': {{new ParameterType(agg).TsNewObjectFunction}},
                    """)}}
                    }
                    //#endregion オブジェクトの新規作成関数


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
                    {{SearchProcessingRefs.RenderTsTypeMap(_queryModels)}}
                    //#endregion 参照検索


                    //#region コマンド
                    {{CommandProcessing.RenderTsTypeMap(_commandModels)}}
                    //#endregion コマンド


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
