using Nijo.Core;
using Nijo.Models.RefTo;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts.Utility;
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
    /// 検索処理
    /// </summary>
    internal class LoadMethod {
        internal LoadMethod(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string ReactHookName => $"use{_aggregate.Item.PhysicalName}Loader";
        internal const string CURRENT_PAGE_ITEMS = "currentPageItems";
        internal const string NOW_LOADING = "nowLoading";
        internal const string LOAD = "load";

        private const string CONTROLLER_ACTION = "load";
        internal string AppSrvLoadMethod => $"Load{_aggregate.Item.PhysicalName}";
        internal string AppSrvCreateQueryMethod => $"Create{_aggregate.Item.PhysicalName}QuerySource";
        private string AppSrvAfterLoadedMethod => $"OnAfter{_aggregate.Item.PhysicalName}Loaded";

        /// <summary>
        /// クライアント側から検索処理を呼び出すReact hook をレンダリングします。
        /// </summary>
        internal string RenderReactHook(CodeRenderingContext context) {
            var controller = new Controller(_aggregate.Item);
            var searchCondition = new SearchCondition(_aggregate);
            var searchResult = new DataClassForDisplay(_aggregate);

            return $$"""
                /** {{_aggregate.Item.DisplayName}}の一覧検索を行いその結果を保持します。 */
                export const {{ReactHookName}} = () => {
                  const [{{CURRENT_PAGE_ITEMS}}, setCurrentPageItems] = React.useState<Types.{{searchResult.TsTypeName}}[]>(() => [])
                  const [{{NOW_LOADING}}, setNowLoading] = React.useState(false)
                  const [, dispatchMsg] = Util.useMsgContext()
                  const { post } = Util.useHttpRequest()

                  const {{LOAD}} = React.useCallback(async (searchCondition: Types.{{searchCondition.TsTypeName}}): Promise<Types.{{searchResult.TsTypeName}}[]> => {
                    setNowLoading(true)
                    try {
                      const res = await post<Types.{{searchResult.TsTypeName}}[]>(`/{{controller.SubDomain}}/{{CONTROLLER_ACTION}}`, searchCondition)
                      if (!res.ok) {
                        dispatchMsg(msg => msg.error('データ読み込みに失敗しました。'))
                        return []
                      }
                      setCurrentPageItems(res.data)
                      return res.data
                    } finally {
                      setNowLoading(false)
                    }
                  }, [post, dispatchMsg])

                  React.useEffect(() => {
                    if (!{{NOW_LOADING}}) {{LOAD}}(Types.{{searchCondition.CreateNewObjectFnName}}())
                  }, [{{LOAD}}])

                  return {
                    /** 読み込み結果の一覧です。現在表示中のページのデータのみが格納されています。 */
                    {{CURRENT_PAGE_ITEMS}},
                    /** 現在読み込み中か否かを返します。 */
                    {{NOW_LOADING}},
                    /**
                     * {{_aggregate.Item.DisplayName}}の一覧検索を行います。
                     * 結果はこの関数の戻り値として返されます。
                     * また戻り値と同じものがこのフックの状態（{{CURRENT_PAGE_ITEMS}}）に格納されます。
                     * どちらか使いやすい方で参照してください。
                     */
                    {{LOAD}},
                  }
                }
                """;
        }

        /// <summary>
        /// 検索処理のASP.NET Core Controller アクションをレンダリングします。
        /// </summary>
        internal string RenderControllerAction(CodeRenderingContext context) {
            var searchCondition = new SearchCondition(_aggregate);

            return $$"""
                [HttpPost("{{CONTROLLER_ACTION}}")]
                public virtual IActionResult Load{{_aggregate.Item.PhysicalName}}([FromBody] {{searchCondition.CsClassName}} searchCondition) {
                    var searchResult = _applicationService.{{AppSrvLoadMethod}}(searchCondition);
                    return this.JsonContent(searchResult.ToArray());
                }
                """;
        }

        /// <summary>
        /// 検索処理の抽象部分をレンダリングします。
        /// </summary>
        internal string RenderAppSrvAbstractMethod(CodeRenderingContext context) {
            var searchCondition = new SearchCondition(_aggregate);
            var searchResult = new SearchResult(_aggregate);
            var forDisplay = new DataClassForDisplay(_aggregate);

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の検索クエリのソース定義。
                /// <para>
                /// このメソッドでやること
                /// - クエリのソース定義（SQLで言うとFROM句とSELECT句に相当する部分）
                /// - カスタム検索条件による絞り込み
                /// - その他任意の絞り込み（例えばログイン中のユーザーのIDを参照して検索結果に含まれる他者の個人情報を除外するなど）
                /// </para>
                /// <para>
                /// このメソッドで書かなくてよいこと
                /// - 自動生成される検索条件による絞り込み
                /// - ソート
                /// - ページング
                /// </para>
                /// </summary>
                protected virtual IQueryable<{{searchResult.CsClassName}}> {{AppSrvCreateQueryMethod}}({{searchCondition.CsClassName}} searchCondition) {
                {{If(_aggregate.Item.Options.GenerateDefaultReadModel, () => $$"""
                    {{WithIndent(RenderWriteModelDefaultQuerySource(), "    ")}}
                """).Else(() => $$"""
                    // クエリのソース定義部分は自動生成されません。
                    // このメソッドをオーバーライドしてソース定義処理を記述してください。
                    return Enumerable.Empty<{{searchResult.CsClassName}}>().AsQueryable();
                """)}}
                }

                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の画面表示用データの、インメモリでのカスタマイズ処理。
                /// 任意の項目のC#上での計算、読み取り専用項目の設定、画面に表示するメッセージの設定などを行います。
                /// この処理はSQLに変換されるのではなくインメモリ上で実行されるため、
                /// データベースから読み込んだデータにしかアクセスできない代わりに、
                /// C#のメソッドやインターフェースなどを無制限に利用することができます。
                /// </summary>
                /// <param name="currentPageSearchResult">検索結果。ページングされた後の、そのページのデータしかないので注意。</param>
                protected virtual IEnumerable<{{forDisplay.CsClassName}}> {{AppSrvAfterLoadedMethod}}(IEnumerable<{{forDisplay.CsClassName}}> currentPageSearchResult) {
                    return currentPageSearchResult;
                }
                """;
        }

        /// <summary>
        /// 既定のReadModelを生成するオプションが指定されている場合のWriteModelのクエリソース定義処理
        /// </summary>
        private string RenderWriteModelDefaultQuerySource() {
            var dbEntity = new EFCoreEntity(_aggregate);
            var searchResult = new SearchResult(_aggregate);
            return $$"""
                #pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
                return DbContext.{{dbEntity.DbSetName}}.Select(e => {{RenderResultConverting(searchResult, "e", _aggregate, true)}});
                #pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。
                """;

            static string RenderResultConverting(SearchResult searchResult, string instance, GraphNode<Aggregate> instanceAggregate, bool renderNewClassName) {
                var newStatement = renderNewClassName
                    ? $"new {searchResult.CsClassName}"
                    : $"new()";
                return $$"""
                    {{newStatement}} {
                    {{searchResult.GetOwnMembers().SelectTextTemplate(member => $$"""
                        {{member.MemberName}} = {{WithIndent(RenderMember(member), "    ")}},
                    """)}}
                    {{If(searchResult.HasLifeCycle, () => $$"""
                        {{SearchResult.VERSION}} = {{instance}}.{{EFCoreEntity.VERSION}}!.Value,
                    """)}}
                    }
                    """;

                string RenderMember(AggregateMember.AggregateMemberBase member) {
                    if (member is AggregateMember.ValueMember vm) {
                        return $$"""
                            {{instance}}.{{vm.Declared.GetFullPathAsSearchResult(since: instanceAggregate).Join(".")}}
                            """;

                    } else if (member is AggregateMember.Ref @ref) {
                        var refSearchResult = new RefSearchResult(@ref.RefTo, @ref.RefTo);
                        return $$"""
                            {{refSearchResult.RenderConvertFromDbEntity(instance, instanceAggregate, false)}}
                            """;

                    } else if (member is AggregateMember.Children children) {
                        var pathToArray = children.ChildrenAggregate.GetFullPathAsSearchResult(since: instanceAggregate);
                        var depth = children.ChildrenAggregate.EnumerateAncestors().Count();
                        var x = depth <= 1 ? "x" : $"x{depth}";
                        var child = new SearchResult(children.ChildrenAggregate);
                        return $$"""
                            {{instance}}.{{pathToArray.Join(".")}}.Select({{x}} => {{RenderResultConverting(child, x, children.ChildrenAggregate, true)}}).ToList()
                            """;

                    } else {
                        var memberSearchResult = new SearchResult(((AggregateMember.RelationMember)member).MemberAggregate);
                        return $$"""
                            {{RenderResultConverting(memberSearchResult, instance, instanceAggregate, false)}}
                            """;
                    }
                }
            }
        }

        /// <summary>
        /// 検索処理の基底処理をレンダリングします。
        /// パラメータの検索条件によるフィルタリング、
        /// パラメータの並び順指定順によるソート、
        /// パラメータのskip, take によるページングを行います。
        /// </summary>
        internal string RenderAppSrvBaseMethod(CodeRenderingContext context) {
            var argType = new SearchCondition(_aggregate);
            var returnType = new DataClassForDisplay(_aggregate);
            var searchResult = new SearchResult(_aggregate);
            var filterMembers = argType
                .EnumerateFilterMembersRecursively();
            var sortMembers = argType
                .EnumerateSortMembersRecursively()
                .Select(m => new {
                    AscLiteral = SearchCondition.GetSortLiteral(m, E_AscDesc.ASC),
                    DescLiteral = SearchCondition.GetSortLiteral(m, E_AscDesc.DESC),
                    Path = m.Member.Declared.GetFullPathAsSearchResult().Join("."),
                })
                .ToArray();
            var keys = _aggregate
                .GetKeys()
                .OfType<AggregateMember.ValueMember>()
                .Select(m => m.Declared.GetFullPathAsSearchResult().Join("."));

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の一覧検索を行います。
                /// </summary>
                public virtual IEnumerable<{{returnType.CsClassName}}> {{AppSrvLoadMethod}}({{argType.CsClassName}} searchCondition) {
                    #pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
                    #pragma warning disable CS8604 // Null 参照引数の可能性があります。

                    var query = {{AppSrvCreateQueryMethod}}(searchCondition);

                {{filterMembers.SelectTextTemplate(m => $$"""
                    // フィルタリング: {{m.MemberName}}
                    {{WithIndent(m.Member.Options.MemberType.RenderFilteringStatement(m.Member, "query", "searchCondition", E_SearchConditionObject.SearchCondition, E_SearchQueryObject.SearchResult), "    ")}}

                """)}}

                    // ソート
                    IOrderedQueryable<{{searchResult.CsClassName}}>? sorted = null;
                    foreach (var sortOption in searchCondition.{{SearchCondition.SORT_CS}}) {
                {{sortMembers.SelectTextTemplate((m, i) => $$"""
                        {{(i == 0 ? "if" : "} else if")}} (sortOption == "{{m.AscLiteral}}") {
                            sorted = sorted == null
                                ? query.OrderBy(e => e.{{m.Path}})
                                : sorted.ThenBy(e => e.{{m.Path}});
                        } else if (sortOption == "{{m.DescLiteral}}") {
                            sorted = sorted == null
                                ? query.OrderByDescending(e => e.{{m.Path}})
                                : sorted.ThenByDescending(e => e.{{m.Path}});

                """)}}
                {{If(sortMembers.Length > 0, () => $$"""
                        }
                """)}}
                    }
                    if (sorted == null) {
                        // ソート順未指定の場合
                {{keys.SelectTextTemplate((path, i) => i == 0 ? $$"""
                        query = query.OrderBy(e => e.{{path}})
                """ : $$"""
                            .ThenBy(e => e.{{path}})
                """)}};
                    } else {
                        query = sorted;
                    }

                    // ページング
                    if (searchCondition.{{SearchCondition.SKIP_CS}} != null) {
                        query = query.Skip(searchCondition.{{SearchCondition.SKIP_CS}}.Value);
                    }
                    if (searchCondition.{{SearchCondition.TAKE_CS}} != null) {
                        query = query.Take(searchCondition.{{SearchCondition.TAKE_CS}}.Value);
                    }

                    // 検索結果を画面表示用の型に変換
                    var displayDataList = query.AsEnumerable().Select(searchResult => {{WithIndent(returnType.RenderConvertFromSearchResult("searchResult", _aggregate, true), "    ")}}).ToArray();

                    // 読み取り専用項目の設定や、追加情報などを付すなど、任意のカスタマイズ処理
                    var returnValue = {{AppSrvAfterLoadedMethod}}(displayDataList);
                    return returnValue;

                    #pragma warning restore CS8604 // Null 参照引数の可能性があります。
                    #pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。
                }
                """;
        }
    }
}
