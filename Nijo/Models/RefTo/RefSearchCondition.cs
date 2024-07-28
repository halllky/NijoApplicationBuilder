using Nijo.Core;
using Nijo.Models.ReadModel2Features;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.RefTo {
    /// <summary>
    /// 参照先検索条件。
    /// </summary>
    internal class RefSearchCondition {
        internal RefSearchCondition(GraphNode<Aggregate> agg, GraphNode<Aggregate> refEntry) {
            _aggregate = agg;
            _refEntry = refEntry;
        }

        protected readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<Aggregate> _refEntry;

        internal virtual string CsClassName => _refEntry == _aggregate
            ? $"{_aggregate.Item.PhysicalName}RefSearchCondition"
            : $"{_aggregate.Item.PhysicalName}RefSearchConditionVia{_refEntry.Item.PhysicalName}";
        internal virtual string TsTypeName => _refEntry == _aggregate
            ? $"{_aggregate.Item.PhysicalName}RefSearchCondition"
            : $"{_aggregate.Item.PhysicalName}RefSearchConditionVia{_refEntry.Item.PhysicalName}";
        internal virtual string CsFilterClassName => _refEntry == _aggregate
            ? $"{_aggregate.Item.PhysicalName}RefSearchConditionFilter"
            : $"{_aggregate.Item.PhysicalName}RefSearchConditionFilterVia{_refEntry.Item.PhysicalName}";
        internal virtual string TsFilterTypeName => _refEntry == _aggregate
            ? $"{_aggregate.Item.PhysicalName}RefSearchConditionFilter"
            : $"{_aggregate.Item.PhysicalName}RefSearchConditionFilterVia{_refEntry.Item.PhysicalName}";

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
        private IEnumerable<RefSearchConditionMember> GetOwnMembers() {
            return _aggregate
                .GetMembers()
                .OfType<AggregateMember.ValueMember>()
                .Where(vm => vm.DeclaringAggregate == _aggregate)
                .Select(vm => new RefSearchConditionMember(vm));
        }
        /// <summary>
        /// 直近の子を列挙します。
        /// </summary>
        internal virtual IEnumerable<RefDescendantSearchCondition> GetChildMembers() {
            return _aggregate
                .GetMembers()
                .OfType<AggregateMember.RelationMember>()
                .Where(rm => rm.MemberAggregate != _aggregate.Source?.Source.As<Aggregate>())
                .Select(rm => new RefDescendantSearchCondition(rm, _refEntry));
        }


        #region フィルタリング
        /// <summary>
        /// 絞り込みに指定することができるメンバーを、子孫要素や参照先のそれも含めて列挙します。
        /// </summary>
        internal IEnumerable<RefSearchConditionMember> EnumerateFilterMembersRecursively() {
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
        internal IEnumerable<RefSearchConditionMember> EnumerateSortMembersRecursively() {
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
        private IEnumerable<RefSearchConditionMember> EnumerateSortMembers() {
            return GetOwnMembers();
        }
        /// <summary>
        /// '子要素.孫要素.プロパティ名::ASC' のような並び順候補の文字列を返します。
        /// </summary>
        internal static string GetSortLiteral(RefSearchConditionMember member, E_AscDesc ascDesc) {
            var fullpath = member.Member
                .GetFullPathAsSearchConditionFilter(E_CsTs.CSharp)
                .Skip(1); // "Filter"という名称を除外
            return ascDesc == E_AscDesc.ASC
                ? $"{fullpath.Join(".")}::ASC"
                : $"{fullpath.Join(".")}::DESC";
        }
        #endregion ソート


        internal virtual string RenderCSharpDeclaringRecursively(CodeRenderingContext context) {
            var searchConditions = new List<RefSearchCondition>();
            void CollectRecursively(RefSearchCondition sc) {
                searchConditions.Add(sc);
                foreach (var child in sc.GetChildMembers()) {
                    CollectRecursively(child);
                }
            }
            CollectRecursively(this);

            return $$"""
                #region 検索条件クラス（{{_aggregate.Item.DisplayName}} が他の集約から参照されている場合の参照検索）
                {{searchConditions.SelectTextTemplate(sc => $$"""
                {{sc.RenderCSharpDeclaring(context)}}
                """)}}
                #endregion 検索条件クラス（{{_aggregate.Item.DisplayName}} が他の集約から参照されている場合の参照検索）

                """;
        }
        internal string RenderCSharpDeclaring(CodeRenderingContext context) {
            return $$"""
                {{If(_aggregate == _refEntry, () => $$"""
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

        internal virtual string RenderTypeScriptDeclaringRecursively(CodeRenderingContext context) {
            var searchConditions = new List<RefSearchCondition>();
            void CollectRecursively(RefSearchCondition sc) {
                searchConditions.Add(sc);
                foreach (var child in sc.GetChildMembers()) {
                    CollectRecursively(child);
                }
            }
            CollectRecursively(this);

            return $$"""
                // ----------------------------------------------------------
                // 検索条件クラス（{{_aggregate.Item.DisplayName}} が他の集約から参照されている場合の参照検索）

                {{searchConditions.SelectTextTemplate(sc => $$"""
                {{sc.RenderTypeScriptDeclaring(context)}}
                """)}}

                """;
        }
        internal string RenderTypeScriptDeclaring(CodeRenderingContext context) {
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
                {{If(_aggregate == _refEntry, () => $$"""
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
    /// <see cref="RefSearchCondition"/> と <see cref="DescendantSearchCondition"/> の両方の性質を併せ持つ。
    /// Parentも存在しうるので厳密にはDescendantという名称は正しくない。
    /// </summary>
    internal class RefDescendantSearchCondition : RefSearchCondition {
        internal RefDescendantSearchCondition(AggregateMember.RelationMember relationMember, GraphNode<Aggregate> refEntry) : base(relationMember.MemberAggregate, refEntry) {
            _relationMember = relationMember;
        }

        private readonly AggregateMember.RelationMember _relationMember;
        internal string MemberName => _relationMember.MemberName;
    }


    public class RefSearchConditionMember {
        internal RefSearchConditionMember(AggregateMember.ValueMember vm) {
            Member = vm;
        }
        internal AggregateMember.ValueMember Member { get; }

        internal string MemberName => Member.MemberName;
        internal string CsTypeName => Member.Options.MemberType.GetSearchConditionCSharpType();
        internal string TsTypeName => Member.Options.MemberType.GetSearchConditionTypeScriptType();
    }
}
