using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models.QueryModelModules;
using System;
using System.Collections.Generic;

namespace Nijo.Ver1.Parts.JavaScript {
    /// <summary>
    /// UI用モジュール。
    /// QueryModelの種類を表す文字列をキーにしてそれと対応するオブジェクトや関数を返すマッピング定義。
    /// </summary>
    internal class MappingsForCustomize : IMultiAggregateSourceFile {

        /// <summary>
        /// QueryModelの型名のリテラル型
        /// </summary>
        internal const string QUERY_MODEL_TYPE = "QueryModelType";
        /// <summary>
        /// CommandModelの型名のリテラル型
        /// </summary>
        internal const string COMMAND_MODEL_TYPE = "CommandModelType";

        internal MappingsForCustomize AddQueryModel(RootAggregate rootAggregate) {
            _queryModels.Add(rootAggregate);
            return this;
        }
        internal MappingsForCustomize AddCommandModel(RootAggregate rootAggregate) {
            _commandModels.Add(rootAggregate);
            return this;
        }
        private readonly List<RootAggregate> _queryModels = [];
        private readonly List<RootAggregate> _commandModels = [];

        void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // 特になし
        }

        void IMultiAggregateSourceFile.Render(CodeRenderingContext ctx) {
            ctx.ReactProject(dir => {
                dir.Generate(Render(ctx));
            });
        }

        private SourceFile Render(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "index.ts",
                Contents = $$"""
                    //#region QueryModelの型名の一覧
                    export type {{QUERY_MODEL_TYPE}} = never // TODO ver.1
                    export const getQueryModelTypeList = (): {{QUERY_MODEL_TYPE}}[] => [/* TODO ver.1 */]
                    //#endregion QueryModelの型名の一覧

                    //#region CommandModelの型名の一覧
                    export type {{COMMAND_MODEL_TYPE}} = never // TODO ver.1
                    export const getCommandModelTypeList = (): {{COMMAND_MODEL_TYPE}}[] => [/* TODO ver.1 */]
                    //#endregion CommandModelの型名の一覧

                    //#region Query,Commandの型名の一覧
                    export type QueryOrCommandModel = {{QUERY_MODEL_TYPE}} | {{COMMAND_MODEL_TYPE}}
                    export const getQueryOrCommandModel = (): ({{QUERY_MODEL_TYPE}} | {{COMMAND_MODEL_TYPE}})[] => [
                      ...getQueryModelTypeList(),
                      ...getCommandModelTypeList(),
                    ]
                    //#endregion Query,Commandの型名の一覧

                    //#region DisplayData型一覧
                    //#endregion DisplayData型一覧

                    //#region DisplayData新規作成関数
                    //#endregion DisplayData新規作成関数

                    //#region SearchCondition型一覧
                    //#endregion SearchCondition型一覧

                    //#region SearchCondition新規作成関数
                    //#endregion SearchCondition新規作成関数

                    //#region SearchConditionソート可能メンバー
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

                    //#region 画面遷移用フック（MultiView）
                    //#endregion 画面遷移用フック（MultiView）

                    //#region 画面遷移用フック（SingleView）
                    //#endregion 画面遷移用フック（SingleView）

                    //#region URLから検索条件オブジェクトへの変換
                    //#endregion URLから検索条件オブジェクトへの変換

                    //#region 検索条件オブジェクトからURLへの変換
                    //#endregion 検索条件オブジェクトからURLへの変換

                    //#region ディープイコール関数
                    //#endregion ディープイコール関数

                    //#region UI制約型一覧
                    //#endregion UI制約型一覧

                    //#region 一括更新フック
                    //#endregion 一括更新フック
                    """,
            };
        }
    }
}
