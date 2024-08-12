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
        /// TypeScriptの新規オブジェクト作成関数の名前
        /// </summary>
        internal string CreateNewObjectFnName => $"createEmpty{TsTypeName}";

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
        internal IEnumerable<DescendantSearchCondition> GetChildMembers() {
            var isOutOfEntryTree = _aggregate.IsOutOfEntryTree();

            foreach (var rm in _aggregate.GetMembers().OfType<AggregateMember.RelationMember>()) {

                if (isOutOfEntryTree) {
                    // 参照先の集約の場合のロジック
                    if (rm.MemberAggregate != _aggregate.Source?.Source.As<Aggregate>())
                        yield return new DescendantSearchCondition(rm);

                } else {
                    // ルート集約のツリー内の場合のロジック
                    if (rm is not AggregateMember.Parent)
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
            var searchConditions = new List<SearchCondition>();
            void CollectRecursively(SearchCondition sc) {
                searchConditions.Add(sc);
                foreach (var child in sc.GetChildMembers()) {
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
            var searchConditions = new List<SearchCondition>();
            void CollectRecursively(SearchCondition sc) {
                searchConditions.Add(sc);
                foreach (var child in sc.GetChildMembers()) {
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
                  {{FILTER_TS}}: {{TsFilterTypeName}}
                  /** 並び順 */
                  {{SORT_TS}}: {{WithIndent(sortType, "  ")}}
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

        /// <summary>
        /// フォームのUIをレンダリングします。
        /// </summary>
        internal string RenderVForm2(ReactPageRenderingContext context) {
            var builder = new Parts.WebClient.VerticalFormBuilder();
            BuildVForm2(context, builder);
            return builder.RenderAsRoot(context.CodeRenderingContext);
        }
        private void BuildVForm2(ReactPageRenderingContext context, Parts.WebClient.VerticalFormSection section) {
            foreach (var m in GetOwnMembers()) {
                section.AddItem(
                    m.Member.Options.MemberType is Core.AggregateMemberTypes.Sentence,
                    m.MemberName,
                    Parts.WebClient.E_VForm2LabelType.String,
                    m.Member.Options.MemberType.RenderSearchConditionVFormBody(m.Member, context));
            }
            foreach (var m in GetChildMembers()) {
                var childSection = section.AddSection(
                    m.DisplayMemberName,
                    Parts.WebClient.E_VForm2LabelType.String);
                m.BuildVForm2(context, childSection);
            }
        }

        internal virtual string RenderCreateNewObjectFn(CodeRenderingContext context) {
            return $$"""
                /** {{_aggregate.Item.DisplayName}}の検索条件クラスの空オブジェクトを作成して返します。 */
                export const {{CreateNewObjectFnName}} = (): {{TsTypeName}} => ({
                  {{FILTER_TS}}: {
                {{GetChildMembers().SelectTextTemplate(m => $$"""
                    {{m.MemberName}}: {{WithIndent(m.RenderCreateNewObjectFn(context), "    ")}},
                """)}}
                  },
                  {{SORT_TS}}: [],
                })
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
        internal string MemberName => _relationMember is AggregateMember.Parent
            ? RefTo.RefSearchCondition.PARENT
            : _relationMember.MemberName;
        internal string DisplayMemberName => _relationMember is AggregateMember.Parent
            ? _relationMember.MemberAggregate.Item.DisplayName
            : _relationMember.MemberName;

        internal override string RenderCreateNewObjectFn(CodeRenderingContext context) {
            return $$"""
                {
                {{GetChildMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}: {{WithIndent(m.RenderCreateNewObjectFn(context), "  ")}},
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

        internal string MemberName => Member.MemberName;
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
