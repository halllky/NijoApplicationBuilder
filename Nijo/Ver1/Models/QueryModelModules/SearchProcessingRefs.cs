using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Common;
using Nijo.Ver1.Parts.CSharp;
using Nijo.Ver1.Parts.JavaScript;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nijo.Ver1.Models.QueryModelModules {
    internal class SearchProcessingRefs {

        public SearchProcessingRefs(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
        }
        private RootAggregate _rootAggregate;

        private const string CONTROLLER_ACTION_LOAD = "load-refs";

        private const string VALIDATE_METHOD = "ValidateRefSearchCondition";
        private const string LOAD_METHOD = "LoadRefsAsync";
        private const string ON_AFTER_LOADED = "OnAfterLoadedRef";
        private const string TO_DISPLAY_DATA = "ToDisplayDataRef";


        #region TypeScript用
        internal static string RenderTsTypeMap(IEnumerable<RootAggregate> queryModels) {

            var items = queryModels.Select(rootAggregate => {
                var controller = new AspNetController(rootAggregate);
                var searchCondition = new SearchCondition(rootAggregate);
                var displayData = new DisplayDataRefEntry(rootAggregate);

                return new {
                    EscapedPhysicalName = rootAggregate.PhysicalName.Replace("'", "\\'"),
                    Endpoint = controller.GetActionNameForClient(CONTROLLER_ACTION_LOAD),
                    ParamType = searchCondition.TsTypeName,
                    ReturnType = $"Util.{SearchProcessingReturn.TYPE_TS}<{displayData.TsTypeName}>",
                };
            }).ToArray();

            return $$"""
                /** 参照検索処理 */
                export namespace LoadRefFeature {
                  /** 参照検索処理のURLエンドポイントの一覧 */
                  export const Endpoint: { [key in {{MappingsForCustomize.QUERY_MODEL_TYPE}}]: string } = {
                {{items.SelectTextTemplate(x => $$"""
                    '{{x.EscapedPhysicalName}}': '{{x.Endpoint}}',
                """)}}
                  }

                  /** 参照検索処理のパラメータ型の一覧 */
                  export interface ParamType {
                {{items.SelectTextTemplate(x => $$"""
                    '{{x.EscapedPhysicalName}}': {{x.ParamType}}
                """)}}
                  }

                  /** 参照検索処理の処理結果の型の一覧 */
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
                /// {{_rootAggregate.DisplayName}}が他の集約から参照されるときの検索処理のエンドポイント
                /// </summary>
                [HttpPost("{{CONTROLLER_ACTION_LOAD}}")]
                public IActionResult LoadRefs({{ComplexPost.REQUEST_CS}}<{{searchCondition.CsClassName}}> request) {
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
                    var returnValue = _applicationService.{{LOAD_METHOD}}(request.Data, context).ToArray();
                    return new {{ComplexPost.RESULT_CS}}(context.ToResult(), returnValue);
                }
                """;
        }

        internal string RenderAppSrvMethod(CodeRenderingContext ctx) {
            var searchCondition = new SearchCondition(_rootAggregate);
            var searchResult = new SearchResult(_rootAggregate);
            var displayData = new DisplayDataRefEntry(_rootAggregate);

            return $$"""
                #region 参照検索
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
                /// {{_rootAggregate.DisplayName}}が他の集約から参照されるときの検索を行ないます。
                /// </summary>
                public async Task<{{SearchProcessingReturn.TYPE_CS}}<{{displayData.CsClassName}}>> {{LOAD_METHOD}}({{searchCondition.CsClassName}} searchCondition, {{PresentationContext.CLASS_NAME}} context) {
                    // FROM句, SELECT句
                    var querySource = {{SearchProcessing.CREATE_QUERY_SOURCE}}(searchCondition, context);

                    // 絞り込み(WHERE句)
                    var filtered = {{SearchProcessing.APPEND_WHERE_CLAUSE}}(querySource, searchCondition);

                    // 並び替え(ORDER BY 句)
                    var sorted = {{SearchProcessing.APPEND_ORDERBY_CLAUSE}}(filtered, searchCondition);

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

                    return new() {
                        {{SearchProcessingReturn.CURRENT_PAGE_ITEMS_CS}} = currentPageItems,
                        {{SearchProcessingReturn.TOTAL_COUNT_CS}} = totalCount,
                    };
                }
                /// <summary>
                /// {{_rootAggregate.DisplayName}}の参照検索結果型を画面表示用の型に変換する
                /// </summary>
                protected virtual {{displayData.CsClassName}} {{TO_DISPLAY_DATA}}({{searchResult.CsClassName}} searchResult) {
                    throw new NotImplementedException(); // TODO ver.1
                }
                /// <summary>
                /// {{_rootAggregate.DisplayName}}の参照検索の読み込み後処理
                /// </summary>
                protected virtual IEnumerable<{{displayData.CsClassName}}> {{ON_AFTER_LOADED}}(IEnumerable<{{displayData.CsClassName}}> currentPageItems, {{searchCondition.CsClassName}} searchCondition, {{PresentationContext.CLASS_NAME}} context) {
                    // 読み込み後処理がある場合はここで実装してください。
                    return currentPageItems;
                }
                #endregion 参照検索
                """;
        }
    }
}
