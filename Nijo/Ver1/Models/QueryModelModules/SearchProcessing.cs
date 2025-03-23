using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Common;
using Nijo.Ver1.Parts.CSharp;
using Nijo.Ver1.Parts.JavaScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.QueryModelModules {
    /// <summary>
    /// 検索処理
    /// </summary>
    internal class SearchProcessing {

        internal SearchProcessing(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
        }
        private readonly RootAggregate _rootAggregate;

        internal string ReactHookName => $"use{_rootAggregate.PhysicalName}Loader";
        internal string ReactHookReturnTypeName => $"Use{_rootAggregate.PhysicalName}LoaderReturn";

        private const string CONTROLLER_ACTION_LOAD = "load";
        internal string ActionEndpoint => $"{CONTROLLER_ACTION_LOAD}";

        private const string VALIDATE_METHOD = "ValidateSearchCondition";
        private const string LOAD_METHOD = "LoadAsync";

        internal const string CREATE_QUERY_SOURCE = "CreateQuerySource";
        internal const string APPEND_WHERE_CLAUSE = "AppendWhereClause";
        internal const string APPEND_ORDERBY_CLAUSE = "AppendOrderByClause";
        private const string ON_AFTER_LOADED = "OnAfterLoaded";
        private const string TO_DISPLAY_DATA = "ToDisplayData";
        private const string SET_KEYS_READONLY = "SetKeysReadOnly";


        #region TypeScript用
        internal static string RenderTsTypeMap(IEnumerable<RootAggregate> queryModels) {

            var items = queryModels.Select(rootAggregate => {
                var controller = new AspNetController(rootAggregate);
                var searchCondition = new SearchCondition(rootAggregate);
                var displayData = new DisplayData(rootAggregate);

                return new {
                    EscapedPhysicalName = rootAggregate.PhysicalName.Replace("'", "\\'"),
                    Endpoint = controller.GetActionNameForClient(CONTROLLER_ACTION_LOAD),
                    ParamType = searchCondition.TsTypeName,
                    ReturnType = $"Util.{SearchProcessingReturn.TYPE_TS}<{displayData.TsTypeName}>",
                };
            }).ToArray();

            return $$"""
                /** 一覧検索処理 */
                export namespace LoadFeature {
                  /** 一覧検索処理のURLエンドポイントの一覧 */
                  export const Endpoint: { [key in {{MappingsForCustomize.QUERY_MODEL_TYPE}}]: string } = {
                {{items.SelectTextTemplate(x => $$"""
                    '{{x.EscapedPhysicalName}}': '{{x.Endpoint}}',
                """)}}
                  }

                  /** 一覧検索処理のパラメータ型の一覧 */
                  export interface ParamType {
                {{items.SelectTextTemplate(x => $$"""
                    '{{x.EscapedPhysicalName}}': {{x.ParamType}}
                """)}}
                  }

                  /** 一覧検索処理の処理結果の型の一覧 */
                  export interface ReturnType {
                {{items.SelectTextTemplate(x => $$"""
                    '{{x.EscapedPhysicalName}}': {{x.ReturnType}}
                """)}}
                  }
                }
                """;
        }
        #endregion TypeScript用

        internal string RenderAspNetCoreControllerAction(CodeRenderingContext ctx) {
            var searchCondition = new SearchCondition(_rootAggregate);
            var searchConditionMessages = new SearchConditionMessageContainer(_rootAggregate);

            return $$"""
                /// <summary>
                /// {{_rootAggregate.DisplayName}}の一覧検索処理のエンドポイント
                /// </summary>
                [HttpPost("{{CONTROLLER_ACTION_LOAD}}")]
                public async Task<IActionResult> Load({{ComplexPost.REQUEST_CS}}<{{searchCondition.CsClassName}}> request) {
                    _applicationService.Log.Debug("Load {{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}");
                    if (_applicationService.GetAuthorizedLevel(E_AuthorizedAction.{{_rootAggregate.PhysicalName}}) == E_AuthLevel.None) return Forbid();

                    var messages = new {{searchConditionMessages.CsClassName}}([]);
                    var context = new {{PresentationContext.CLASS_NAME}}(request.Options, messages);

                    // エラーチェック
                    _applicationService.{{VALIDATE_METHOD}}(request.Data, context);
                    if (context.HasError() || (!context.Options.IgnoreConfirm && context.HasConfirm())) {
                        return new {{ComplexPost.RESULT_CS}}(context.ToResult());
                    }

                    // 検索処理実行
                    var returnValue = await _applicationService.{{LOAD_METHOD}}(request.Data, context);
                    return new {{ComplexPost.RESULT_CS}}(context.ToResult(), returnValue);
                }
                """;
        }

        internal string RenderAppSrvMethods(CodeRenderingContext ctx) {
            var searchCondition = new SearchCondition(_rootAggregate);
            var searchResult = new SearchResult(_rootAggregate);
            var displayData = new DisplayData(_rootAggregate);

            return $$"""
                /// <summary>
                /// {{_rootAggregate.DisplayName}}の検索条件の内容を検証します。
                /// 不正な場合、検索処理自体の実行が中止されます。
                /// </summary>
                /// <param name="searchCondition">検索条件</param>
                /// <param name="context">エラーがある場合はこのオブジェクトの中にエラー内容を追記してください。</param>
                public virtual void {{VALIDATE_METHOD}}({{searchCondition.CsClassName}} searchCondition, {{PresentationContext.CLASS_NAME}} context) {
                    // エラーチェックがある場合はこのメソッドをオーバーライドして記述してください。
                }

                /// <summary>
                /// {{_rootAggregate.DisplayName}}の一覧検索を行ないます。
                /// </summary>
                public async Task<{{SearchProcessingReturn.TYPE_CS}}<{{displayData.CsClassName}}>> {{LOAD_METHOD}}({{searchCondition.CsClassName}} searchCondition, {{PresentationContext.CLASS_NAME}} context) {
                    // FROM句, SELECT句
                    var querySource = {{CREATE_QUERY_SOURCE}}(searchCondition, context);

                    // 絞り込み(WHERE句)
                    var filtered = {{APPEND_WHERE_CLAUSE}}(querySource, searchCondition);

                    // 並び替え(ORDER BY 句)
                    var sorted = {{APPEND_ORDERBY_CLAUSE}}(filtered, searchCondition);

                    // ページング(SKIP, TAKE)
                    var query = (IQueryable<{{searchResult.CsClassName}}>)sorted;
                    if (searchCondition.{{SearchCondition.SKIP_CS}} != null) {
                        query = query.Skip(searchCondition.{{SearchCondition.SKIP_CS}}.Value);
                    }
                    if (searchCondition.{{SearchCondition.TAKE_CS}} != null) {
                        query = query.Take(searchCondition.{{SearchCondition.TAKE_CS}}.Value);
                    }

                    // 画面表示用の型への変換
                    var converted = query.Select({{TO_DISPLAY_DATA}});

                    // 検索処理実行
                    {{displayData.CsClassName}}[] loaded;
                    int totalCount;
                    try {
                        var countTask = filtered.CountAsync();
                        var searchTask = converted.ToArrayAsync();

                        // UIのパフォーマンスのため並列で実行
                        await Task.WhenAll(countTask, searchTask);

                        loaded = await searchTask;
                        totalCount = await countTask;
                    } catch {
                        Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}} エラー発生時検索条件: {0}", searchCondition.ToJson());
                        try {
                            Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}} エラー発生時SQL: {0}", query.ToQueryString());
                        } catch {
                            Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}} 不具合調査用のSQL変換に失敗しました。");
                        }
                        throw;
                    }

                    // 読み取り専用項目の設定や、C#上での追加情報の付加など、任意のカスタマイズ処理
                    var currentPageItems = {{ON_AFTER_LOADED}}(loaded, searchCondition, context).ToArray();

                    // 主キー項目を読み取り専用にする。UI上で主キーが変更されるとあらゆる処理がうまくいかなくなる
                    foreach (var displayData in currentPageItems) {
                        {{SET_KEYS_READONLY}}(displayData);
                    }

                    return new() {
                        {{SearchProcessingReturn.CURRENT_PAGE_ITEMS_CS}} = currentPageItems,
                        {{SearchProcessingReturn.TOTAL_COUNT_CS}} = totalCount,
                    };
                }
                /// <summary>
                /// {{_rootAggregate.DisplayName}}の検索結果型を画面表示用の型に変換する
                /// </summary>
                protected virtual {{displayData.CsClassName}} {{TO_DISPLAY_DATA}}({{searchResult.CsClassName}} searchResult) {
                    throw new NotImplementedException(); // TODO ver.1
                }
                /// <summary>
                /// {{_rootAggregate.DisplayName}}の一覧検索の読み込み後処理
                /// </summary>
                protected virtual IEnumerable<{{displayData.CsClassName}}> {{ON_AFTER_LOADED}}(IEnumerable<{{displayData.CsClassName}}> currentPageItems, {{searchCondition.CsClassName}} searchCondition, {{PresentationContext.CLASS_NAME}} context) {
                    // 読み込み後処理がある場合はここで実装してください。
                    return currentPageItems;
                }
                /// <summary>
                /// {{_rootAggregate.DisplayName}}の画面表示データの主キー項目を読み取り専用にする
                /// </summary>
                protected virtual void {{SET_KEYS_READONLY}}({{displayData.CsClassName}} displayData) {
                    // TODO ver.1
                }
                """;
        }
    }
}
