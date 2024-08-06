using Nijo.Core;
using Nijo.Models.ReadModel2Features;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.RefTo {
    /// <summary>
    /// 参照先データ検索処理
    /// </summary>
    internal class RefSearchMethod {
        internal RefSearchMethod(GraphNode<Aggregate> agg, GraphNode<Aggregate> refEntry) {
            _aggregate = agg;
            _refEntry = refEntry;
        }
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<Aggregate> _refEntry;

        internal string ReactHookName => $"useSearchReference{_aggregate.Item.PhysicalName}";
        internal const string CURRENT_PAGE_ITEMS = "currentPageItems";
        internal const string NOW_LOADING = "nowLoading";
        internal const string LOAD = "load";

        internal string Url => $"/{new Controller(_aggregate.Item).SubDomain}/{ControllerAction}";
        private string ControllerAction => _aggregate.IsRoot()
            ? $"search-refs"
            : $"search-refs/{_aggregate.Item.PhysicalName}";
        private string AppSrvLoadMethod => $"SearchRefs{_aggregate.Item.PhysicalName}";
        private string AppSrvBeforeLoadMethod => $"OnBeforeLoadSearchRefs{_aggregate.Item.PhysicalName}";

        internal string RenderHook(CodeRenderingContext context) {
            var controller = new Controller(_aggregate.Item);
            var searchCondition = new RefSearchCondition(_aggregate, _refEntry);
            var searchResult = new RefSearchResult(_aggregate, _refEntry);

            return $$"""
                /** {{_aggregate.Item.DisplayName}}の参照先検索を行いその結果を保持します。 */
                export const {{ReactHookName}} = () => {
                  const [{{CURRENT_PAGE_ITEMS}}, setCurrentPageItems] = React.useState<Types.{{searchResult.TsTypeName}}[]>(() => [])
                  const [{{NOW_LOADING}}, setNowLoading] = React.useState(false)
                  const [, dispatchMsg] = Util.useMsgContext()
                  const { post } = Util.useHttpRequest()

                  /** {{_aggregate.Item.DisplayName}}の参照先検索を行います。結果は戻り値ではなくフックの状態に格納されます。 */
                  const {{LOAD}} = React.useCallback(async (searchCondition: Types.{{searchCondition.TsTypeName}}) => {
                    setNowLoading(true)
                    try {
                      const res = await post<Types.{{searchResult.TsTypeName}}[]>(`{{controller.SubDomain}}/{{ControllerAction}}`, searchCondition)
                      if (!res.ok) {
                        dispatchMsg(msg => msg.error('データ読み込みに失敗しました。'))
                        return
                      }
                      setCurrentPageItems(res.data)
                    } finally {
                      setNowLoading(false)
                    }
                  }, [post, dispatchMsg])

                  React.useEffect(() => {
                    if (!{{NOW_LOADING}}) {{LOAD}}(Types.{{searchCondition.CreateNewObjectFnName}}())
                  }, [{{LOAD}}])

                  return {
                    {{CURRENT_PAGE_ITEMS}},
                    {{NOW_LOADING}},
                    {{LOAD}},
                  }
                }
                """;
        }

        internal string RenderController(CodeRenderingContext context) {
            var searchCondition = new RefSearchCondition(_aggregate, _refEntry);

            return $$"""
                [HttpPost("{{ControllerAction}}")]
                public virtual IActionResult Load{{_aggregate.Item.PhysicalName}}([FromBody] {{searchCondition.CsClassName}} searchCondition) {
                    var searchResult = _applicationService.{{AppSrvLoadMethod}}(searchCondition);
                    return this.JsonContent(searchResult.ToArray());
                }
                """;
        }

        internal string RenderAppSrvMethodOfWriteModel(CodeRenderingContext context) {
            var searchCondition = new RefSearchCondition(_aggregate, _refEntry);
            var searchResult = new RefSearchResult(_aggregate, _refEntry);
            var dbEntity = new EFCoreEntity(_aggregate);

            var filterMembers = searchCondition
                .EnumerateFilterMembersRecursively();
            var sortMembers = searchCondition
                .EnumerateSortMembersRecursively()
                .Select(m => new {
                    AscLiteral = RefSearchCondition.GetSortLiteral(m, E_AscDesc.ASC),
                    DescLiteral = RefSearchCondition.GetSortLiteral(m, E_AscDesc.DESC),
                    Path = m.Member.Declared.GetFullPathAsDbEntity().Join("."),
                })
                .ToArray();
            var keys = _aggregate
                .GetKeys()
                .OfType<AggregateMember.ValueMember>()
                .Select(m => m.Declared.GetFullPathAsDbEntity().Join("."));

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}} が他の集約から参照されるときの検索処理
                /// </summary>
                /// <param name="searchCondition">検索条件</param>
                /// <returns>検索結果</returns>
                public virtual IEnumerable<{{searchResult.CsClassName}}> {{AppSrvLoadMethod}}({{searchCondition.CsClassName}} searchCondition) {
                    #pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
                    #pragma warning disable CS8604 // Null 参照引数の可能性があります。

                    var query = (IQueryable<{{dbEntity.ClassName}}>)DbContext.{{dbEntity.DbSetName}};

                {{filterMembers.SelectTextTemplate(m => $$"""
                    // フィルタリング: {{m.MemberName}}
                    {{WithIndent(m.Member.Options.MemberType.RenderFilteringStatement(m.Member, "query", "searchCondition", E_SearchConditionObject.RefSearchCondition, E_SearchQueryObject.EFCoreEntity), "    ")}}

                """)}}
                    // フィルタリング: 任意の処理
                    query = {{AppSrvBeforeLoadMethod}}(query);

                    // ソート
                    IOrderedQueryable<{{dbEntity.ClassName}}>? sorted = null;
                    foreach (var sortOption in searchCondition.{{RefSearchCondition.SORT_CS}}) {
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
                        query = query
                {{keys.SelectTextTemplate((path, i) => i == 0 ? $$"""
                            .OrderBy(e => e.{{path}})
                """ : $$"""
                            .ThenBy(e => e.{{path}})
                """)}};
                    } else {
                        query = sorted;
                    }

                    // ページング
                    if (searchCondition.{{RefSearchCondition.SKIP_CS}} != null) {
                        query = query.Skip(searchCondition.{{RefSearchCondition.SKIP_CS}}.Value);
                    }
                    if (searchCondition.{{RefSearchCondition.TAKE_CS}} != null) {
                        query = query.Take(searchCondition.{{RefSearchCondition.TAKE_CS}}.Value);
                    }

                    // #35 N+1
                    var searchResult = query.AsEnumerable().Select(e => {{WithIndent(searchResult.RenderConvertFromWriteModelDbEntity("e"), "    ")}});
                    return searchResult;

                    #pragma warning restore CS8604 // Null 参照引数の可能性があります。
                    #pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。
                }

                /// <summary>
                /// <see cref="{{AppSrvLoadMethod}}"/> のフィルタリング処理の前で実行されるカスタマイズフィルタリング処理
                /// </summary>
                /// <param name="query">クエリ</param>
                /// <returns>任意のフィルタリング処理を加えたあとのクエリ。任意のフィルタリング処理が特にない場合は引数をそのまま返す</returns>
                protected virtual IQueryable<{{dbEntity.ClassName}}> {{AppSrvBeforeLoadMethod}}(IQueryable<{{dbEntity.ClassName}}> query) {
                    // 任意のフィルタリング処理がある場合はこのメソッドをオーバーライドして実装してください。
                    return query;
                }
                """;
        }

        internal string RenderAppSrvMethodOfReadModel(CodeRenderingContext context) {
            var load = new LoadMethod(_aggregate.GetRoot());
            var searchCondition = new SearchCondition(_aggregate.GetRoot());
            var searchResult = new DataClassForDisplay(_aggregate.GetRoot());
            var refSearchCondition = new RefSearchCondition(_aggregate, _refEntry);
            var refSearchResult = new RefSearchResult(_aggregate, _refEntry);

            // 通常の一覧検索の戻り値は親要素の配列だが、
            // この集約が子孫要素の場合、戻り値はその子孫要素の配列になる。そして祖先は各要素のプロパティになる。
            // つまり親が子をプロパティとして持つのではなく子が親をプロパティとして持つ形になる。その逆転変換処理に使う変数
            var ancestors = _aggregate
                .EnumerateAncestors()
                .Select(edge => new DataClassForDisplayDescendant(edge.Terminal.AsChildRelationMember()));
            const string C_PARENT = "Parent";
            const string C_CHILD = "Child";

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}} が他の集約から参照されるときの検索処理
                /// </summary>
                /// <param name="refSearchCondition">検索条件</param>
                /// <returns>検索結果</returns>
                public virtual IEnumerable<{{refSearchResult.CsClassName}}> {{AppSrvLoadMethod}}({{refSearchCondition.CsClassName}} refSearchCondition) {
                    // 通常の一覧検索処理を流用する
                    var searchCondition = new {{searchCondition.CsClassName}} {
                        Filter = new() {
                            // TODO #35 参照先検索条件を通常の検索条件に変換する
                        },
                        Keyword = refSearchCondition.Keyword,
                        Skip = refSearchCondition.Skip,
                        Sort = refSearchCondition.Sort,
                        Take = refSearchCondition.Take,
                    };
                    var searchResult = {{load.AppSrvLoadMethod}}(searchCondition);

                    // 通常の一覧検索結果の型を、他の集約から参照されるときの型に変換する
                    var refTargets = searchResult
                {{ancestors.SelectTextTemplate(prop => prop.IsArray ? $$"""
                        .SelectMany(sr => sr.{{prop.MemberName}}, (parent, child) => new { {{C_PARENT}} = parent, {{C_CHILD}} = child })
                """ : $$"""
                        .Select(sr => new { {{C_PARENT}} = sr, {{C_CHILD}} = sr.{{prop.MemberName}} })
                """)}}
                        .Select(sr => new {{refSearchResult.CsClassName}} {
                            {{RefSearchResult.INSTANCE_KEY_CS}} = "", // TODO #35 IsntanceKey
                            // TODO #35 通常の一覧検索結果を参照先検索結果に変換する
                        });
                    return refTargets;
                }
                """;
        }


    }
}
