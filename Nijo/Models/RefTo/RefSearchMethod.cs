using Nijo.Core;
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
        internal const string RELOAD = "reload";

        private const string CONTROLLER_ACTION = "search-refs";
        private string AppSrvLoadMethod => $"SearchRefs{_aggregate.Item.PhysicalName}";
        private string AppSrvBeforeLoadMethod => $"OnBeforeLoadSearchRefs{_aggregate.Item.PhysicalName}";

        internal string RenderHook(CodeRenderingContext context) {
            var controller = new Controller(_aggregate.Item);
            var searchCondition = new RefSearchCondition(_aggregate, _refEntry);
            var searchResult = new RefSearchResult(_aggregate, _refEntry);

            return $$"""
                export const {{ReactHookName}} = (searchCondition?: Types.{{searchCondition.TsTypeName}}) => {
                  const [{{CURRENT_PAGE_ITEMS}}, setCurrentPageItems] = React.useState<{{searchResult.TsTypeName}}[]>(() => [])
                  const [{{NOW_LOADING}}, setNowLoading] = React.useState(false)
                  const [, dispatchMsg] = Util.useMsgContext()
                  const { post } = Util.useHttpRequest()

                  const {{RELOAD}} = React.useCallback(async () => {
                    setNowLoading(true)
                    try {
                      const res = await post<{{searchResult.TsTypeName}}[]>(`{{controller.SubDomain}}/{{CONTROLLER_ACTION}}`, searchCondition)
                      if (!res.ok) {
                        dispatchMsg(msg => msg.error('データ読み込みに失敗しました。'))
                        return
                      }
                      setCurrentPageItems(res.data)
                    } finally {
                      setNowLoading(false)
                    }
                  }, [searchCondition, post, dispatchMsg])

                  React.useEffect(() => {
                    if (!{{NOW_LOADING}}) {{RELOAD}}()
                  }, [{{RELOAD}}])

                  return {
                    {{CURRENT_PAGE_ITEMS}},
                    {{NOW_LOADING}},
                    {{RELOAD}},
                  }
                }
                """;
        }

        internal string RenderController(CodeRenderingContext context) {
            var searchCondition = new RefSearchCondition(_aggregate, _refEntry);

            return $$"""
                [HttpPost("{{CONTROLLER_ACTION}}")]
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

                    var searchResult = query.Select(e => {{WithIndent(searchResult.RenderConvertFromWriteModelDbEntity("e"), "    ")}});
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
            return $$"""
                // TODO #35 RefSearchMethod RenderAppSrvMethod
                """;
        }
    }
}
