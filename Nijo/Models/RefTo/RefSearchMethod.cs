using Nijo.Core;
using Nijo.Models.ReadModel2Features;
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
        internal const string COUNT = "count";

        internal string Url => $"/{new Controller(_aggregate.Item).SubDomain}/{ControllerLoadAction}";
        private string ControllerLoadAction => _aggregate.IsRoot()
            ? $"search-refs"
            : $"search-refs/{_aggregate.Item.PhysicalName}";
        private string ControllerCountAction => _aggregate.IsRoot()
            ? $"search-refs-count"
            : $"search-refs-count/{_aggregate.Item.PhysicalName}";
        private string AppSrvLoadMethod => $"SearchRefs{_aggregate.Item.PhysicalName}";
        private string AppSrvCountMethod => $"SearchRefsCount{_aggregate.Item.PhysicalName}";
        private string AppSrvBeforeLoadMethod => $"OnBeforeLoadSearchRefs{_aggregate.Item.PhysicalName}";

        private const string APPEND_WHERE_CLAUSE = "AppendWhereClause";

        internal string RenderHook(CodeRenderingContext context) {
            var controller = new Controller(_aggregate.GetRoot().Item);
            var searchCondition = new RefSearchCondition(_aggregate, _refEntry);
            var searchResult = new RefDisplayData(_aggregate, _refEntry);

            return $$"""
                /** {{_aggregate.Item.DisplayName}}の参照先検索を行いその結果を保持します。 */
                export const {{ReactHookName}} = (disableAutoLoad?: boolean) => {
                  const [{{CURRENT_PAGE_ITEMS}}, setCurrentPageItems] = React.useState<Types.{{searchResult.TsTypeName}}[]>(() => [])
                  const [{{NOW_LOADING}}, setNowLoading] = React.useState(false)
                  const [, dispatchMsg] = Util.useMsgContext()
                  const { post } = Util.useHttpRequest()

                  const {{LOAD}} = React.useCallback(async (searchCondition: Types.{{searchCondition.TsTypeName}}): Promise<Types.{{searchResult.TsTypeName}}[]> => {
                    setNowLoading(true)
                    try {
                      const res = await post<Types.{{searchResult.TsTypeName}}[]>(`/{{controller.SubDomain}}/{{ControllerLoadAction}}`, searchCondition)
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

                  const {{COUNT}} = React.useCallback(async (searchConditionFilter: Types.{{searchCondition.TsFilterTypeName}}): Promise<number> => {
                    try {
                      const res = await post<number>(`/{{controller.SubDomain}}/{{ControllerCountAction}}`, searchConditionFilter)
                      return res.ok ? res.data : 0
                    } catch {
                      return 0
                    }
                  }, [post])

                  React.useEffect(() => {
                    if (!{{NOW_LOADING}} && !disableAutoLoad) {
                      {{LOAD}}(Types.{{searchCondition.CreateNewObjectFnName}}())
                    }
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
                    /** 検索結果件数カウント */
                    {{COUNT}},
                  }
                }
                """;
        }

        internal string RenderController(CodeRenderingContext context) {
            var searchCondition = new RefSearchCondition(_aggregate, _refEntry);

            return $$"""
                [HttpPost("{{ControllerLoadAction}}")]
                public virtual IActionResult Load{{_aggregate.Item.PhysicalName}}([FromBody] {{searchCondition.CsClassName}} searchCondition) {
                    var searchResult = _applicationService.{{AppSrvLoadMethod}}(searchCondition);
                    return this.JsonContent(searchResult.ToArray());
                }
                [HttpPost("{{ControllerCountAction}}")]
                public virtual IActionResult Count{{_aggregate.Item.PhysicalName}}([FromBody] {{searchCondition.CsFilterClassName}} searchConditionFilter) {
                    var count = _applicationService.{{AppSrvCountMethod}}(searchConditionFilter);
                    return this.JsonContent(count);
                }
                """;
        }

        internal string RenderAppSrvMethodOfWriteModel(CodeRenderingContext context) {
            var searchCondition = new RefSearchCondition(_aggregate, _refEntry);
            var searchResult = new RefDisplayData(_aggregate, _refEntry);
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
                /// {{_aggregate.Item.DisplayName}} が他の集約から参照されるときの検索処理の結果件数を数えます。
                /// </summary>
                /// <param name="searchConditionFilter">検索条件</param>
                /// <returns>検索結果</returns>
                public virtual int {{AppSrvCountMethod}}({{searchCondition.CsFilterClassName}} searchConditionFilter) {
                    var searchCondition = new {{searchCondition.CsClassName}}();
                    searchCondition.{{RefSearchCondition.FILTER_CS}} = searchConditionFilter;

                    var querySource = (IQueryable<{{dbEntity.ClassName}}>)DbContext.{{dbEntity.DbSetName}};
                    var query = {{APPEND_WHERE_CLAUSE}}(querySource, searchCondition);
                    var count = query.Count();

                    return count;
                }
                /// <summary>
                /// {{_aggregate.Item.DisplayName}} が他の集約から参照されるときの検索処理
                /// </summary>
                /// <param name="searchCondition">検索条件</param>
                /// <returns>検索結果</returns>
                public virtual IEnumerable<{{searchResult.CsClassName}}> {{AppSrvLoadMethod}}({{searchCondition.CsClassName}} searchCondition) {
                    #pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
                    #pragma warning disable CS8603 // Null 参照戻り値である可能性があります。
                    #pragma warning disable CS8604 // Null 参照引数の可能性があります。

                    var querySource = (IQueryable<{{dbEntity.ClassName}}>)DbContext.{{dbEntity.DbSetName}};

                    // フィルタリング
                    var query = {{APPEND_WHERE_CLAUSE}}(querySource, searchCondition);

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
                        // ソート順未指定の場合
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
                    #pragma warning restore CS8603 // Null 参照戻り値である可能性があります。
                    #pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。
                }

                /// <summary>
                /// <see cref="{{AppSrvLoadMethod}}"/> のフィルタリング処理
                /// </summary>
                protected virtual IQueryable<{{dbEntity.ClassName}}> {{APPEND_WHERE_CLAUSE}}(IQueryable<{{dbEntity.ClassName}}> query, {{searchCondition.CsClassName}} searchCondition) {
                    #pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
                    #pragma warning disable CS8603 // Null 参照戻り値である可能性があります。
                    #pragma warning disable CS8604 // Null 参照引数の可能性があります。
                {{filterMembers.SelectTextTemplate(m => $$"""
                    // フィルタリング: {{m.MemberName}}
                    {{WithIndent(m.Member.Options.MemberType.RenderFilteringStatement(m.Member, "query", "searchCondition", E_SearchConditionObject.RefSearchCondition, E_SearchQueryObject.EFCoreEntity), "    ")}}

                """)}}
                    // フィルタリング: 任意の処理
                    query = {{AppSrvBeforeLoadMethod}}(query);

                    return query;

                    #pragma warning restore CS8604 // Null 参照引数の可能性があります。
                    #pragma warning restore CS8603 // Null 参照戻り値である可能性があります。
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

        const string C_PARENT = "parent";
        const string C_SELF = "self";

        internal string RenderAppSrvMethodOfReadModel(CodeRenderingContext context) {
            var refTargetRoot = _aggregate.GetRoot();
            var load = new LoadMethod(refTargetRoot);
            var searchCondition = new SearchCondition(refTargetRoot, refTargetRoot);
            var searchResult = new DataClassForDisplay(refTargetRoot);
            var refSearchCondition = new RefSearchCondition(_aggregate, _refEntry);
            var refSearchResult = new RefDisplayData(_aggregate, _refEntry);

            // 通常の一覧検索の戻り値は親要素の配列だが、
            // この集約が子孫要素の場合、戻り値はその子孫要素の配列になる。そして祖先は各要素のプロパティになる。
            // つまり親が子をプロパティとして持つのではなく子が親をプロパティとして持つ形になる。その逆転変換処理に使う変数
            var ancestors = _aggregate
                .EnumerateAncestors()
                .Select(edge => new DataClassForDisplayDescendant(edge.Terminal.AsChildRelationMember()))
                .ToArray();

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}} が他の集約から参照されるときの検索結果カウント
                /// </summary>
                public virtual int {{AppSrvCountMethod}}({{refSearchCondition.CsFilterClassName}} refSearchConditionFilter) {
                    // 通常の一覧検索結果カウント処理を流用する
                    var searchCondition = new {{searchCondition.CsClassName}} {
                        Filter = {{WithIndent(RenderFilterConverting(searchCondition).Replace($"refSearchCondition.{RefSearchCondition.FILTER_CS}", "refSearchConditionFilter"), "        ")}},
                    };
                    var querySource = {{load.AppSrvCreateQueryMethod}}(searchCondition);
                    var query = {{LoadMethod.METHOD_NAME_APPEND_WHERE_CLAUSE}}(querySource, searchCondition);

                #pragma warning disable CS8603 // Null 参照戻り値である可能性があります。
                    var count = query
                {{_aggregate.EnumerateAncestors().SelectTextTemplate(edge => edge.Terminal.IsChildrenMember() ? $$"""
                        .SelectMany(e => e.{{edge.Terminal.AsChildRelationMember().MemberName}})
                """ : $$"""
                        .Select(e => e.{{edge.Terminal.AsChildRelationMember().MemberName}})
                """)}}
                        .Count();
                #pragma warning restore CS8603 // Null 参照戻り値である可能性があります。

                    return count;
                }
                /// <summary>
                /// {{_aggregate.Item.DisplayName}} が他の集約から参照されるときの検索処理
                /// </summary>
                /// <param name="refSearchCondition">検索条件</param>
                /// <returns>検索結果</returns>
                public virtual IEnumerable<{{refSearchResult.CsClassName}}> {{AppSrvLoadMethod}}({{refSearchCondition.CsClassName}} refSearchCondition) {
                    // 通常の一覧検索処理を流用するため、検索条件の値を移し替える
                    var searchCondition = new {{searchCondition.CsClassName}} {
                        Filter = {{WithIndent(RenderFilterConverting(searchCondition), "        ")}},
                        Keyword = refSearchCondition.Keyword,
                        Skip = refSearchCondition.Skip,
                        Sort = refSearchCondition.Sort,
                        Take = refSearchCondition.Take,
                    };

                    // 検索処理実行
                    var searchResult = {{load.AppSrvLoadMethod}}(searchCondition);

                    // 通常の一覧検索結果の型を、他の集約から参照されるときの型に変換する
                    var refTargets = searchResult
                {{ancestors.SelectTextTemplate((prop, i) => prop.IsArray ? $$"""
                        .SelectMany(sr => sr.{{(i == 0 ? "" : $"{C_SELF}.")}}{{prop.MemberName}}, ({{C_PARENT}}, {{C_SELF}}) => new { {{C_PARENT}}, {{C_SELF}} })
                """ : $$"""
                        .Select(sr => new { {{C_PARENT}} = sr, {{C_SELF}} = sr.{{(i == 0 ? "" : $"{C_SELF}.")}}{{prop.MemberName}} })
                """)}}
                        .Select(sr => {{WithIndent(RenderResultConverting(refSearchResult, "sr", _aggregate, true), "        ")}});
                    return refTargets;
                }
                """;

            // 参照先検索条件クラスのフィルタ部分を通常の検索条件クラスのものに変換する
            static string RenderFilterConverting(SearchCondition sc) {
                return $$"""
                    new() {
                    {{sc.GetOwnMembers().SelectTextTemplate(member => $$"""
                        {{member.MemberName}} = refSearchCondition.{{member.Member.Declared.GetFullPathAsRefSearchConditionFilter(E_CsTs.CSharp).Join(".")}},
                    """)}}
                    {{sc.GetChildMembers().SelectTextTemplate(child => $$"""
                        {{child.MemberName}} = {{WithIndent(RenderFilterConverting(child), "    ")}},
                    """)}}
                    }
                    """;
            }

            // 通常の一覧検索結果を参照先検索結果に変換する
            string RenderResultConverting(RefDisplayData rsr, string instance, GraphNode<Aggregate> instanceAggregate, bool renderNewClassName) {
                var @new = renderNewClassName
                    ? $"new {rsr.CsClassName}"
                    : $"new()";
                var keys = rsr
                    .GetKeys()
                    .OfType<AggregateMember.ValueMember>()
                    .ToArray();
                return $$"""
                    {{@new}} {
                    {{rsr.GetOwnMembers().SelectTextTemplate(member => $$"""
                        {{RefDisplayData.GetMemberName(member)}} = {{WithIndent(RenderMember(member), "    ")}},
                    """)}}
                    }
                    """;

                string RenderMember(AggregateMember.AggregateMemberBase member) {
                    if (member is AggregateMember.ValueMember vm) {
                        return $$"""
                            {{instance}}.{{GetFullPathBeforeReturn(vm.Declared, instanceAggregate).Join("?.")}}
                            """;

                    } else if (member is AggregateMember.Children children) {
                        var pathToArray = GetFullPathBeforeReturn(children, instanceAggregate);
                        var depth = children.ChildrenAggregate.EnumerateAncestors().Count();
                        var x = depth <= 1 ? "x" : $"x{depth}";
                        var child = new RefDisplayData(children.ChildrenAggregate, _refEntry);
                        return $$"""
                            {{instance}}.{{pathToArray.Join("?.")}}?.Select({{x}} => {{RenderResultConverting(child, x, children.ChildrenAggregate, true)}}).ToList() ?? []
                            """;

                    } else {
                        var memberRefSearchResult = new RefDisplayData(((AggregateMember.RelationMember)member).MemberAggregate, _refEntry);
                        return $$"""
                            {{RenderResultConverting(memberRefSearchResult, instance, instanceAggregate, false)}}
                            """;
                    }
                }
            }
        }

        /// <summary>
        /// 検索処理の最後の変換処理用のフルパス取得
        /// </summary>
        private IEnumerable<string> GetFullPathBeforeReturn(AggregateMember.AggregateMemberBase member, GraphNode<Aggregate>? since = null) {
            var pathFromEntry = member.Owner.PathFromEntry();
            var skip = since != null;
            var self = false;
            foreach (var e in pathFromEntry) {
                var edge = e.As<Aggregate>();

                if (skip && edge.Source == since) skip = false;

                if (edge.Initial.IsInEntryTree()) {
                    if (edge.Terminal.IsInEntryTree()) {
                        // エントリーのツリー内のオブジェクトのパス
                        if (edge.Source == edge.Terminal) {
                            // 子から親へ辿る経路
                            if (!skip) yield return C_PARENT;

                        } else {
                            // 親から子へ辿る経路
                            if (!self) {
                                if (!skip && !edge.Initial.IsRoot()) yield return C_SELF;
                                self = true;
                            }
                            if (!skip) yield return edge.RelationName;
                        }

                    } else {
                        // 参照エントリ
                        if (!self) {
                            if (!skip && !edge.Initial.IsRoot()) yield return C_SELF;
                            self = true;
                        }
                        if (!skip) yield return DataClassForDisplay.VALUES_CS;
                        if (!skip) yield return edge.RelationName;
                    }

                } else {
                    // 参照先オブジェクト内部のパス
                    if (edge.IsParentChild() && edge.Source == edge.Terminal) {
                        if (!skip) yield return RefDisplayData.PARENT;

                    } else {
                        if (!skip) yield return edge.RelationName;
                    }
                }
            }
            if (member.Owner.IsInEntryTree()) {
                if (!self && !member.Owner.IsRoot()) {
                    yield return C_SELF;
                }
                if (member is AggregateMember.ValueMember || member is AggregateMember.Ref) {
                    yield return DataClassForDisplay.VALUES_CS;
                }
            }
            yield return RefDisplayData.GetMemberName(member);
        }
    }
}
