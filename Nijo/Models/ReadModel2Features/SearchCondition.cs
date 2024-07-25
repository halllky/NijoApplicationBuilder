using Nijo.Core;
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
        internal SearchCondition(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }
        protected readonly GraphNode<Aggregate> _aggregate;

        internal virtual string CsClassName => $"{_aggregate.Item.PhysicalName}SearchCondition";
        internal virtual string TsTypeName => $"{_aggregate.Item.PhysicalName}SearchCondition";
        internal virtual string CsFilterClassName => $"{_aggregate.Item.PhysicalName}SearchConditionFilter";
        internal virtual string TsFilterTypeName => $"{_aggregate.Item.PhysicalName}SearchConditionFilter";

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
        /// この集約自身がもつ検索条件を列挙します。
        /// </summary>
        private IEnumerable<SearchConditionMember> GetOwnMembers() {
            return _aggregate
                .GetMembers()
                .OfType<AggregateMember.ValueMember>()
                .Where(vm => vm.DeclaringAggregate == _aggregate)
                .Select(vm => new SearchConditionMember(vm));
        }
        /// <summary>
        /// 直近の子を列挙します。
        /// </summary>
        private IEnumerable<DescendantSearchCondition> GetChildMembers() {
            return _aggregate
                .GetMembers()
                .OfType<AggregateMember.RelationMember>()
                .Where(rm => rm is AggregateMember.Child
                          || rm is AggregateMember.Children
                          || rm is AggregateMember.VariationItem
                          || rm is AggregateMember.Ref)
                .Select(rm => new DescendantSearchCondition(rm));
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
            return GetOwnMembers();
        }
        /// <summary>
        /// '子要素.孫要素.プロパティ名::ASC' のような並び順候補の文字列を返します。
        /// </summary>
        internal static string GetSortLiteral(SearchConditionMember member, E_AscDesc ascDesc) {
            var fullpath = member.Member
                .GetFullPathAsSearchConditionFilter(E_CsTs.CSharp)
                .Skip(1); // "Filter"という名称を除外
            return ascDesc == E_AscDesc.ASC
                ? $"{fullpath.Join(".")}::ASC"
                : $"{fullpath.Join(".")}::DESC";
        }
        #endregion ソート


        internal string RenderCSharpDeclaringRecursively(CodeRenderingContext context) {
            var descendants = _aggregate
                .EnumerateDescendants()
                .Select(agg => new SearchCondition(agg));
            var refToConditions = _aggregate
                .EnumerateThisAndDescendants()
                .SelectMany(agg => agg.GetMembers())
                .OfType<AggregateMember.Ref>()
                .Select(@ref => new DescendantSearchCondition(@ref));

            return $$"""
                #region 検索条件クラス（{{_aggregate.Item.DisplayName}}）
                {{RenderCSharpDeclaring(context)}}
                {{descendants.SelectTextTemplate(sc => $$"""
                {{sc.RenderCSharpDeclaring(context)}}
                """)}}
                {{refToConditions.SelectTextTemplate(sc => $$"""
                {{sc.RenderCSharpDeclaring(context)}}
                """)}}
                #endregion 検索条件クラス（{{_aggregate.Item.DisplayName}}）

                """;
        }
        protected virtual string RenderCSharpDeclaring(CodeRenderingContext context) {
            return $$"""
                {{If(_aggregate.IsRoot(), () => $$"""
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
                    public virtual List<string> {{SORT_CS}} { get; set; } = new();
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
                    public virtual {{m.CsTypeName}}? {{m.MemberName}} { get; set; }
                """)}}
                {{GetChildMembers().SelectTextTemplate(m => $$"""
                    public virtual {{m.CsFilterClassName}} {{m.MemberName}} { get; set; } = new();
                """)}}
                }
                """;
        }

        internal string RenderTypeScriptDeclaringRecursively(CodeRenderingContext context) {
            var descendants = _aggregate
                .EnumerateDescendants()
                .Select(agg => new SearchCondition(agg));
            var refToConditions = _aggregate
                .EnumerateThisAndDescendants()
                .SelectMany(agg => agg.GetMembers())
                .OfType<AggregateMember.Ref>()
                .Select(@ref => new DescendantSearchCondition(@ref));

            return $$"""
                {{RenderTypeScriptDeclaring(context)}}
                {{descendants.SelectTextTemplate(sc => $$"""
                {{sc.RenderTypeScriptDeclaring(context)}}
                """)}}
                {{refToConditions.SelectTextTemplate(sc => $$"""
                {{sc.RenderTypeScriptDeclaring(context)}}
                """)}}

                """;
        }
        protected virtual string RenderTypeScriptDeclaring(CodeRenderingContext context) {
            var sortLiteral = new List<string>();
            foreach (var sortMember in EnumerateSortMembersRecursively()) {
                sortLiteral.Add($"'{GetSortLiteral(sortMember, E_AscDesc.ASC)}'");
                sortLiteral.Add($"'{GetSortLiteral(sortMember, E_AscDesc.DESC)}'");
            }
            var last = sortLiteral.Count - 1;
            var sortType = sortLiteral.Count == 0
                ? "never[]"
                : $$"""
                    (
                    {{sortLiteral.SelectTextTemplate((sortLiteral, i) => $$"""
                      {{sortLiteral}}{{(i == last ? "" : " |")}}
                    """)}}
                    )[]
                    """;

            return $$"""
                {{If(_aggregate.IsRoot(), () => $$"""
                /** {{_aggregate.Item.DisplayName}}の一覧検索条件 */
                export type {{TsTypeName}} = {
                  /** 絞り込み条件（キーワード検索） */
                  {{KEYWORD_TS}}?: string
                  /** 絞り込み条件 */
                  {{FILTER_TS}}?: {{TsFilterTypeName}}
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
                  {{m.MemberName}}?: {{m.TsTypeName}}
                """)}}
                {{GetChildMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}: {{m.TsFilterTypeName}}
                """)}}
                }
                """;
        }
    }


    /// <summary>
    /// ルート集約ではない検索条件クラス
    /// </summary>
    internal class DescendantSearchCondition : SearchCondition {
        internal DescendantSearchCondition(AggregateMember.RelationMember relationMember) : base(relationMember.MemberAggregate) {
            _relationMember = relationMember;
        }

        private readonly AggregateMember.RelationMember _relationMember;
        internal string MemberName => _relationMember.MemberName;

        // Refの場合は複数のReadModelから1つのWriteModelへの参照がある可能性があり名前衝突するかもしれないので"RefFrom～"をつける
        internal override string CsClassName => _relationMember.MemberAggregate.IsOutOfEntryTree()
            ? $"{base.CsClassName}_RefFrom{_aggregate.GetEntry().As<Aggregate>().Item.PhysicalName}の{_relationMember.MemberAggregate.GetRefEdge().RelationName}"
            : base.CsClassName;
        internal override string TsTypeName => _relationMember.MemberAggregate.IsOutOfEntryTree()
            ? $"{base.TsTypeName}_RefFrom{_aggregate.GetEntry().As<Aggregate>().Item.PhysicalName}の{_relationMember.MemberAggregate.GetRefEdge().RelationName}"
            : base.TsTypeName;
    }


    public class SearchConditionMember {
        internal SearchConditionMember(AggregateMember.ValueMember vm) {
            Member = vm;
        }
        internal AggregateMember.ValueMember Member { get; }

        internal string MemberName => Member.MemberName;
        internal string CsTypeName => Member.Options.MemberType.GetSearchConditionCSharpType();
        internal string TsTypeName => Member.Options.MemberType.GetSearchConditionTypeScriptType();
    }


    internal static partial class GetFullPathExtensions {
        /// <summary>
        /// エントリーからのパスを <see cref="SearchCondition"/> のインスタンスの型のルールにあわせて返す。
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
                yield return edge.RelationName;
            }
        }
        /// <summary>
        /// エントリーからのパスを <see cref="SearchCondition"/> のインスタンスの型のルールにあわせて返す。
        /// </summary>
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
