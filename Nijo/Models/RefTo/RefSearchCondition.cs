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
    internal class RefSearchCondition : SearchCondition {
        internal RefSearchCondition(GraphNode<Aggregate> agg, GraphNode<Aggregate> refEntry) : base(agg) {
            _refEntry = refEntry;
        }

        private readonly GraphNode<Aggregate> _refEntry;

        internal override string CsClassName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefSearchCondition"
            : $"{_refEntry.Item.PhysicalName}RefSearchCondition_{GetRelationHistory(_aggregate, _refEntry).Join("の")}";
        internal override string TsTypeName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefSearchCondition"
            : $"{_refEntry.Item.PhysicalName}RefSearchCondition_{GetRelationHistory(_aggregate, _refEntry).Join("の")}";
        internal override string CsFilterClassName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefSearchConditionFilter"
            : $"{_refEntry.Item.PhysicalName}RefSearchConditionFilter_{GetRelationHistory(_aggregate, _refEntry).Join("の")}";
        internal override string TsFilterTypeName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefSearchConditionFilter"
            : $"{_refEntry.Item.PhysicalName}RefSearchConditionFilter_{GetRelationHistory(_aggregate, _refEntry).Join("の")}";

        protected override bool IsSearchConditionEntry => _refEntry == _aggregate;

        /// <summary>
        /// 型名重複回避のためにフルパスを型名に含める
        /// </summary>
        private static IEnumerable<string> GetRelationHistory(GraphNode<Aggregate> agg, GraphNode<Aggregate> refEntry) {
            foreach (var edge in agg.PathFromEntry().Since(refEntry)) {
                if (edge.IsParentChild() && edge.Source == edge.Terminal) {
                    yield return edge.Initial.As<Aggregate>().Item.PhysicalName;
                } else {
                    yield return edge.RelationName.ToCSharpSafe();
                }
            }
        }

        internal override IEnumerable<RefDescendantSearchCondition> GetChildMembers() {
            foreach (var rm in _aggregate.GetMembers().OfType<AggregateMember.RelationMember>()) {
                // 無限ループ回避
                if (rm.MemberAggregate == _aggregate.Source?.Source.As<Aggregate>()) continue;

                if (!rm.Relation.IsRef()) {
                    yield return new RefDescendantSearchCondition(rm, _refEntry);

                } else {
                    // 参照先の中でさらに他の集約を参照している場合はRefエントリー仕切りなおし
                    yield return new RefDescendantSearchCondition(rm, rm.MemberAggregate);
                }
            }
        }


        internal const string PARENT = "PARENT";

        /// <summary>
        /// <see cref="RefSearchCondition"/> と <see cref="DescendantSearchCondition"/> の両方の性質を併せ持つ。
        /// Parentも存在しうるので厳密にはDescendantという名称は正しくない。
        /// </summary>
        internal class RefDescendantSearchCondition : DescendantSearchCondition {
            internal RefDescendantSearchCondition(AggregateMember.RelationMember relationMember, GraphNode<Aggregate> refEntry) : base(relationMember) {
                _asRSC = new RefSearchCondition(relationMember.MemberAggregate, refEntry);
            }

            /// <summary>
            /// このクラスにおける <see cref="DescendantSearchCondition"/> と異なる部分のロジックは
            /// <see cref="RefSearchCondition"/> とまったく同じため、そのロジックを流用する
            /// </summary>
            private readonly RefSearchCondition _asRSC;

            internal override string CsClassName => _asRSC.CsClassName;
            internal override string TsTypeName => _asRSC.TsTypeName;
            internal override string CsFilterClassName => _asRSC.CsFilterClassName;
            internal override string TsFilterTypeName => _asRSC.TsFilterTypeName;
            protected override bool IsSearchConditionEntry => _asRSC.IsSearchConditionEntry;
            internal override IEnumerable<RefDescendantSearchCondition> GetChildMembers() => _asRSC.GetChildMembers();
        }
    }


    internal static partial class GetFullPathExtensions {
        /// <summary>
        /// エントリーからのパスを
        /// <see cref="SearchCondition"/> と
        /// <see cref="RefTo.RefSearchCondition"/> の
        /// インスタンスの型のルールにあわせて返す。
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsRefSearchConditionFilter(this GraphNode<Aggregate> aggregate, E_CsTs csts, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var entry = aggregate.GetEntry();

            var path = aggregate.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);

            var first = true;
            foreach (var edge in path) {
                if (first) {
                    yield return csts == E_CsTs.CSharp
                        ? RefSearchCondition.FILTER_CS
                        : RefSearchCondition.FILTER_TS;
                    first = false;
                }

                if (edge.Source == edge.Terminal && edge.IsParentChild()) {
                    // 子から親へ向かう経路の場合
                    yield return RefDisplayData.PARENT;
                } else {
                    yield return edge.RelationName;
                }
            }
        }

        /// <inheritdoc cref="GetFullPathAsRefSearchConditionFilter(GraphNode{Aggregate}, E_CsTs, GraphNode{Aggregate}?, GraphNode{Aggregate}?)"/>
        internal static IEnumerable<string> GetFullPathAsRefSearchConditionFilter(this AggregateMember.AggregateMemberBase member, E_CsTs csts, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var fullpath = member.Owner
                .GetFullPathAsRefSearchConditionFilter(csts, since, until)
                .ToArray();
            if (fullpath.Length == 0) {
                yield return csts == E_CsTs.CSharp
                    ? RefSearchCondition.FILTER_CS
                    : RefSearchCondition.FILTER_TS;
            }
            foreach (var path in fullpath) {
                yield return path;
            }
            yield return member.MemberName;
        }
    }
}
