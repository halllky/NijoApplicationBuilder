using Nijo.Core;
using Nijo.Parts.WebClient;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 検索条件クラス
    /// </summary>
    internal class SearchCondition {
        internal SearchCondition(GraphNode<Aggregate> agg, GraphNode<Aggregate>? entry = null) {
            _aggregate = agg;
            _entry = entry ?? agg.GetEntry().As<Aggregate>();
        }
        protected readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<Aggregate> _entry;

        internal virtual string CsClassName => _aggregate == _entry
            ? $"{_entry.Item.PhysicalName}SearchCondition"
            : $"{_entry.Item.PhysicalName}SearchCondition_{GetRelationHistory().Join("の")}";
        internal virtual string TsTypeName => _aggregate == _entry
            ? $"{_entry.Item.PhysicalName}SearchCondition"
            : $"{_entry.Item.PhysicalName}SearchCondition_{GetRelationHistory().Join("の")}";
        internal virtual string CsFilterClassName => _aggregate == _entry
            ? $"{_entry.Item.PhysicalName}SearchConditionFilter"
            : $"{_entry.Item.PhysicalName}SearchConditionFilter_{GetRelationHistory().Join("の")}";
        internal virtual string TsFilterTypeName => _aggregate == _entry
            ? $"{_entry.Item.PhysicalName}SearchConditionFilter"
            : $"{_entry.Item.PhysicalName}SearchConditionFilter_{GetRelationHistory().Join("の")}";
        private IEnumerable<string> GetRelationHistory() {
            foreach (var edge in _aggregate.PathFromEntry().Since(_entry)) {
                if (edge.IsParentChild() && edge.Source == edge.Terminal) {
                    yield return edge.Initial.As<Aggregate>().Item.PhysicalName;
                } else {
                    yield return edge.RelationName.ToCSharpSafe();
                }
            }
        }

        internal const string KEYWORD_CS = "Keyword";
        internal const string KEYWORD_TS = "keyword";
        internal const string FILTER_CS = "Filter";
        internal const string FILTER_TS = "filter";
        internal const string SORT_CS = "Sort";
        internal const string SORT_TS = "sort";
        internal const string SKIP_CS = "Skip";
        internal const string SKIP_TS = "skip";
        internal const string TAKE_CS = "Take";
        internal const string TAKE_TS = "take";

        /// <summary>
        /// もしこのオブジェクトがエントリーポイントならば
        /// <see cref="SORT_CS"/> や <see cref="SKIP_CS"/> などの項目を持っているが
        /// そうでなければ持っていない
        /// </summary>
        protected virtual bool IsSearchConditionEntry => _aggregate.IsRoot();

        /// <summary>
        /// TypeScriptの新規オブジェクト作成関数の名前
        /// </summary>
        internal string CreateNewObjectFnName => $"createNew{TsTypeName}";

        /// <summary>
        /// この集約自身がもつ検索条件を列挙します。
        /// </summary>
        internal IEnumerable<SearchConditionMember> GetOwnMembers() {
            return _aggregate
                .GetMembers()
                .OfType<AggregateMember.ValueMember>()
                .Where(vm => vm.DeclaringAggregate == _aggregate)
                .Select(vm => new SearchConditionMember(vm));
        }
        /// <summary>
        /// 直近の子を列挙します。
        /// </summary>
        internal virtual IEnumerable<DescendantSearchCondition> GetChildMembers() {
            foreach (var rm in _aggregate.GetMembers().OfType<AggregateMember.RelationMember>()) {
                if (rm is AggregateMember.Parent) {
                    continue;
                } else if (rm is AggregateMember.Ref @ref) {
                    yield return new RefTo.RefSearchCondition.RefDescendantSearchCondition(@ref, @ref.RefTo);
                } else {
                    yield return new DescendantSearchCondition(rm);
                }
            }
        }


        #region フィルタリング
        /// <summary>
        /// 絞り込みに指定することができるメンバーを、子孫要素や参照先のそれも含めて列挙します。
        /// </summary>
        internal IEnumerable<SearchConditionMember> EnumerateFilterMembersRecursively() {
            foreach (var member in GetOwnMembers()) {
                yield return member;
            }
            foreach (var childMember in GetChildMembers().SelectMany(child => child.EnumerateFilterMembersRecursively())) {
                yield return childMember;
            }
        }
        #endregion フィルタリング


        #region ソート
        /// <summary>
        /// 並び順に指定することができるメンバーを、子孫要素や参照先のそれも含めて列挙します。
        /// </summary>
        internal IEnumerable<SearchConditionMember> EnumerateSortMembersRecursively() {
            foreach (var member in EnumerateSortMembers()) {
                yield return member;
            }
            foreach (var child in GetChildMembers()) {
                // 子配列の要素でのソートは論理的に定義できない
                if (child._aggregate.IsChildrenMember()) continue;

                foreach (var childMember in child.EnumerateSortMembersRecursively()) {
                    yield return childMember;
                }
            }
        }
        /// <summary>
        /// 並び順に指定することができるメンバーを列挙します。
        /// </summary>
        private IEnumerable<SearchConditionMember> EnumerateSortMembers() {
            foreach (var scMember in GetOwnMembers()) {
                // 非表示項目ではソート不可
                if (scMember.Member.Options.InvisibleInGui) continue;

                yield return scMember;
            }
        }
        /// <summary>
        /// '子要素.孫要素.プロパティ名（昇順）' のような並び順候補の文字列の一覧を返します。
        /// </summary>
        internal IEnumerable<string> GetSortLiterals() {
            foreach (var sortMember in EnumerateSortMembersRecursively()) {
                yield return GetSortLiteral(sortMember, E_AscDesc.ASC);
                yield return GetSortLiteral(sortMember, E_AscDesc.DESC);
            }
        }
        /// <summary>
        /// '子要素.孫要素.プロパティ名（昇順）' のような並び順候補の文字列を返します。
        /// </summary>
        internal static string GetSortLiteral(SearchConditionMember member, E_AscDesc ascDesc) {
            var fullpath = member.Member
                .GetFullPathAsSearchConditionFilter(E_CsTs.CSharp)
                .Skip(1); // "Filter"という名称を除外
            return ascDesc == E_AscDesc.ASC
                ? $"{fullpath.Join(".")}{ASC_SUFFIX}"
                : $"{fullpath.Join(".")}{DESC_SUFFIX}";
        }
        internal const string ASC_SUFFIX = "（昇順）";
        internal const string DESC_SUFFIX = "（降順）";
        #endregion ソート


        internal string RenderCSharpDeclaringRecursively(CodeRenderingContext context) {
            var searchConditions = new List<SearchCondition>();
            void CollectRecursively(SearchCondition sc) {
                searchConditions.Add(sc);
                foreach (var child in sc.GetChildMembers()) {
                    // 参照先の型は参照先クラスのほうでレンダリングするので除外
                    if (!child._aggregate.IsInTreeOf(_aggregate)) continue;

                    CollectRecursively(child);
                }
            }
            CollectRecursively(this);

            return $$"""
                #region 検索条件クラス（{{_aggregate.Item.DisplayName}}）
                {{searchConditions.SelectTextTemplate(sc => $$"""
                {{sc.RenderCSharpDeclaring(context)}}
                """)}}
                #endregion 検索条件クラス（{{_aggregate.Item.DisplayName}}）

                """;
        }
        protected virtual string RenderCSharpDeclaring(CodeRenderingContext context) {
            return $$"""
                {{If(IsSearchConditionEntry, () => $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の一覧検索条件
                /// </summary>
                public partial class {{CsClassName}} {
                    /// <summary>絞り込み条件（キーワード検索）</summary>
                    [JsonPropertyName("{{KEYWORD_TS}}")]
                    public string? {{KEYWORD_CS}} { get; set; }
                    /// <summary>絞り込み条件</summary>
                    [JsonPropertyName("{{FILTER_TS}}")]
                    public virtual {{CsFilterClassName}} {{FILTER_CS}} { get; set; } = new();
                    /// <summary>並び順</summary>
                    [JsonPropertyName("{{SORT_TS}}")]
                    public virtual List<string>? {{SORT_CS}} { get; set; } = new();
                    /// <summary>先頭から何件スキップするか</summary>
                    [JsonPropertyName("{{SKIP_TS}}")]
                    public virtual int? {{SKIP_CS}} { get; set; }
                    /// <summary>最大何件取得するか</summary>
                    [JsonPropertyName("{{TAKE_TS}}")]
                    public virtual int? {{TAKE_CS}} { get; set; }
                }
                """)}}
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の一覧検索条件のうち絞り込み条件を指定する部分
                /// </summary>
                public partial class {{CsFilterClassName}} {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                    public virtual {{m.CsTypeName}}? {{m.PhysicalName}} { get; set; }
                """)}}
                {{GetChildMembers().SelectTextTemplate(m => $$"""
                    public virtual {{m.CsFilterClassName}} {{m.PhysicalName}} { get; set; } = new();
                """)}}
                }
                """;
        }

        internal string RenderTypeScriptDeclaringRecursively(CodeRenderingContext context) {
            var searchConditions = new List<SearchCondition>();
            void CollectRecursively(SearchCondition sc) {
                searchConditions.Add(sc);
                foreach (var child in sc.GetChildMembers()) {
                    // 参照先の型は参照先クラスのほうでレンダリングするので除外
                    if (!child._aggregate.IsInTreeOf(_aggregate)) continue;

                    CollectRecursively(child);
                }
            }
            CollectRecursively(this);

            return $$"""
                // ----------------------------------------------------------
                // 検索条件クラス（{{_aggregate.Item.DisplayName}}）

                {{searchConditions.SelectTextTemplate(sc => $$"""
                {{sc.RenderTypeScriptDeclaring(context)}}
                """)}}

                """;
        }
        protected virtual string RenderTypeScriptDeclaring(CodeRenderingContext context) {
            var sortLiteral = GetSortLiterals()
                .Select(sort => $"'{sort}'")
                .ToArray();
            var last = sortLiteral.Length - 1;
            var sortType = sortLiteral.Length == 0
                ? "string[]"
                : $$"""
                    (
                    {{sortLiteral.SelectTextTemplate((sortLiteral, i) => $$"""
                      {{sortLiteral}}{{(i == last ? "" : " |")}}
                    """)}}
                    )[]
                    """;

            return $$"""
                {{If(IsSearchConditionEntry, () => $$"""
                /** {{_aggregate.Item.DisplayName}}の一覧検索条件 */
                export type {{TsTypeName}} = {
                  /** 絞り込み条件（キーワード検索） */
                  {{KEYWORD_TS}}?: string
                  /** 絞り込み条件 */
                  {{FILTER_TS}}: {{TsFilterTypeName}}
                  /** 並び順 */
                  {{SORT_TS}}?: {{WithIndent(sortType, "  ")}}
                  /** 先頭から何件スキップするか */
                  {{SKIP_TS}}?: number
                  /** 最大何件取得するか */
                  {{TAKE_TS}}?: number
                }
                """)}}
                /** {{_aggregate.Item.DisplayName}}の一覧検索条件のうち絞り込み条件を指定する部分 */
                export type {{TsFilterTypeName}} = {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                  {{m.PhysicalName}}?: {{m.TsTypeName}}
                """)}}
                {{GetChildMembers().SelectTextTemplate(m => $$"""
                  {{m.PhysicalName}}: {{m.TsFilterTypeName}}
                """)}}
                }
                """;
        }

        /// <summary>
        /// フォームのUIをレンダリングします。
        /// </summary>
        internal string RenderVForm2(FormUIRenderingContext context, bool usePageSizeAndSortComboBox) {
            var root = new VForm2.RootNode(
                new VForm2.StringLabel("検索条件"),
                _aggregate.Item.Options.FormDepth,
                _aggregate.Item.Options.EstimatedLabelWidth);

            BuildVForm2(context, root);

            if (usePageSizeAndSortComboBox) {
                // 表示件数
                root.Append(new VForm2.ItemNode(new VForm2.StringLabel("表示件数"), true, $$"""
                    <Input.ComboBox
                      {...{{context.Register}}(`{{TAKE_TS}}`)}
                      {...{{MultiView.PAGE_SIZE_COMBO_SETTING}}}
                      className="max-w-[5.5rem]"
                    />
                    """));

                // ソート
                root.Append(new VForm2.ItemNode(new VForm2.StringLabel("検索時並び順"), true, $$"""
                    <Input.MultiSelect
                      {...{{context.Register}}(`{{SORT_TS}}`)}
                      {...{{MultiView.SORT_COMBO_SETTING}}}
                      onFilter={{{MultiView.SORT_COMBO_FILTERING}}}
                      className="items-start"
                    />
                    """));
            }

            return root.Render(context.CodeRenderingContext);
        }
        private void BuildVForm2(FormUIRenderingContext context, VForm2 section) {

            /// <see cref="RefTo.RefSearchCondition.RenderUiComponent"/> とロジックを合わせる

            var renderedMembers = GetOwnMembers().Select(m => new {
                MemberInfo = (AggregateMember.AggregateMemberBase)m.Member,
                m.DisplayName,
                Descendant = (DescendantSearchCondition?)null,
            }).Concat(GetChildMembers().Select(m => new {
                MemberInfo = (AggregateMember.AggregateMemberBase)m.MemberInfo,
                m.DisplayName,
                Descendant = (DescendantSearchCondition?)m,
            }));

            foreach (var m in renderedMembers.OrderBy(m => m.MemberInfo.Order)) {
                if (m.MemberInfo is AggregateMember.ValueMember vm) {
                    if (vm.Options.InvisibleInGui) continue; // 非表示項目

                    var fullpath = context.GetReactHookFormFieldPath(vm);
                    var body = vm.Options.MemberType.RenderSearchConditionVFormBody(vm, context);
                    section.Append(new VForm2.ItemNode(new VForm2.StringLabel(m.DisplayName), false, body));

                } else {
                    // 入れ子コンポーネントをレンダリングする
                    var childSection = new VForm2.IndentNode(new VForm2.StringLabel(m.DisplayName));
                    section.Append(childSection);
                    m.Descendant!.BuildVForm2(context, childSection);
                }
            }
        }
        /// <summary>
        /// 参照先のカスタマイズUIレンダリング時のreact hook form 用パス。
        /// <see cref="SearchCondition"/> と <see cref="RefTo.RefSearchCondition"/> でルールが微妙に異なるためメソッドに切り出している
        /// </summary>
        protected virtual IEnumerable<string> GetFullPathForRefRHFRegisterName(AggregateMember.Ref @ref) {
            return @ref.GetFullPathAsSearchConditionFilter(E_CsTs.TypeScript);
        }

        internal virtual string RenderCreateNewObjectFn(CodeRenderingContext context) {
            return $$"""
                /** {{_aggregate.Item.DisplayName}}の検索条件クラスの空オブジェクトを作成して返します。 */
                export const {{CreateNewObjectFnName}} = (): {{TsTypeName}} => ({
                  {{FILTER_TS}}: {
                {{GetChildMembers().SelectTextTemplate(m => $$"""
                    {{m.PhysicalName}}: {{WithIndent(m.RenderCreateNewObjectFn(context), "    ")}},
                """)}}
                  },
                  {{SORT_TS}}: [],
                  {{TAKE_TS}}: 20,
                })
                """;
        }

        #region URLのクエリパラメータとの変換
        internal string ParseQueryParameter => $"parseQueryParameterAs{TsTypeName}";

        // 画面初期表示時の検索条件をMultiViewに来る前の画面で指定するためのURLクエリパラメータの名前
        internal const string URL_KEYWORD = "k";
        internal const string URL_FILTER = "f";
        internal const string URL_SORT = "s";
        internal const string URL_TAKE = "t";
        internal const string URL_SKIP = "p";

        internal string RenderParseQueryParameterFunction() {
            return $$"""
                /** クエリパラメータを解釈して画面初期表示時検索条件オブジェクトを返します。 */
                export const {{ParseQueryParameter}} = (urlSearch: string): {{TsTypeName}} => {
                  const searchCondition = {{CreateNewObjectFnName}}()
                  if (!urlSearch) return searchCondition

                  const searchParams = new URLSearchParams(urlSearch)
                  if (searchParams.has('{{URL_KEYWORD}}'))
                    searchCondition.{{KEYWORD_TS}} = searchParams.get('{{URL_KEYWORD}}')!
                  if (searchParams.has('{{URL_FILTER}}'))
                    searchCondition.{{FILTER_TS}} = JSON.parse(searchParams.get('{{URL_FILTER}}')!)
                  if (searchParams.has('{{URL_SORT}}'))
                    searchCondition.{{SORT_TS}} = JSON.parse(searchParams.get('{{URL_SORT}}')!)
                  if (searchParams.has('{{URL_TAKE}}'))
                    searchCondition.{{TAKE_TS}} = Number(searchParams.get('{{URL_TAKE}}'))
                  if (searchParams.has('{{URL_SKIP}}'))
                    searchCondition.{{SKIP_TS}} = Number(searchParams.get('{{URL_SKIP}}'))

                  return searchCondition
                }
                """;
        }
        #endregion URLのクエリパラメータとの変換
    }


    /// <summary>
    /// ルート集約ではない検索条件クラス
    /// </summary>
    internal class DescendantSearchCondition : SearchCondition {
        internal DescendantSearchCondition(AggregateMember.RelationMember relationMember) : base(relationMember.MemberAggregate) {
            MemberInfo = relationMember;
        }

        internal AggregateMember.RelationMember MemberInfo { get; }
        internal string PhysicalName => MemberInfo is AggregateMember.Parent
            ? RefTo.RefSearchCondition.PARENT
            : MemberInfo.MemberName;
        internal string DisplayName => MemberInfo is AggregateMember.Parent
            ? MemberInfo.MemberAggregate.Item.DisplayName
            : MemberInfo.DisplayName;

        internal override string RenderCreateNewObjectFn(CodeRenderingContext context) {
            return $$"""
                {
                {{GetChildMembers().SelectTextTemplate(m => $$"""
                  {{m.PhysicalName}}: {{WithIndent(m.RenderCreateNewObjectFn(context), "  ")}},
                """)}}
                }
                """;
        }
    }


    public class SearchConditionMember {
        internal SearchConditionMember(AggregateMember.ValueMember vm) {
            Member = vm;
        }
        internal AggregateMember.ValueMember Member { get; }

        internal string PhysicalName => Member.MemberName;
        internal string DisplayName => Member.DisplayName;
        internal string CsTypeName => Member.Options.MemberType.GetSearchConditionCSharpType();
        internal string TsTypeName => Member.Options.MemberType.GetSearchConditionTypeScriptType();
    }


    internal static partial class GetFullPathExtensions {
        /// <summary>
        /// エントリーからのパスを
        /// <see cref="SearchCondition"/> と
        /// <see cref="RefTo.RefSearchCondition"/> の
        /// インスタンスの型のルールにあわせて返す。
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsSearchConditionFilter(this GraphNode<Aggregate> aggregate, E_CsTs csts, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var entry = aggregate.GetEntry();

            var path = aggregate.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);

            foreach (var edge in path) {
                if (edge.Initial == entry) {
                    yield return csts == E_CsTs.CSharp
                        ? SearchCondition.FILTER_CS
                        : SearchCondition.FILTER_TS;
                }

                if (edge.Source == edge.Terminal && edge.IsParentChild()) {
                    // 子から親へ向かう経路の場合
                    if (edge.Initial.As<Aggregate>().IsOutOfEntryTree()) {
                        yield return RefTo.RefDisplayData.PARENT;
                    } else {
                        yield return $"/* エラー！{nameof(SearchCondition)}では子は親の参照を持っていません */";
                    }
                } else {
                    yield return edge.RelationName;
                }
            }
        }

        /// <inheritdoc cref="GetFullPathAsSearchConditionFilter(GraphNode{Aggregate}, E_CsTs, GraphNode{Aggregate}?, GraphNode{Aggregate}?)"/>
        internal static IEnumerable<string> GetFullPathAsSearchConditionFilter(this AggregateMember.AggregateMemberBase member, E_CsTs csts, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var fullpath = member.Owner
                .GetFullPathAsSearchConditionFilter(csts, since, until)
                .ToArray();
            if (fullpath.Length == 0) {
                yield return csts == E_CsTs.CSharp
                    ? SearchCondition.FILTER_CS
                    : SearchCondition.FILTER_TS;
            }
            foreach (var path in fullpath) {
                yield return path;
            }
            yield return member.MemberName;
        }
    }
}
