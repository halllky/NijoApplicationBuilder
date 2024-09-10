using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nijo.Core.DirectedEdgeExtensions;

namespace Nijo.Core {
    public class Aggregate : ValueObject, IGraphNode {
        internal Aggregate(NodeId id, string displayName, bool useKeyInsteadOfName, AggregateBuildOption options) {
            Id = id;
            DisplayName = displayName;
            UseKeyInsteadOfName = useKeyInsteadOfName;
            Options = options;
        }

        public NodeId Id { get; }
        internal string DisplayName { get; }
        internal string UniqueId => Id.Value.ToHashedString();

        public string PhysicalName => DisplayName.ToCSharpSafe();

        // TODO: EFCoerEntityやDbSetを表すクラスがこれらの情報を保持するべき
        public string EFCoreEntityClassName => $"{PhysicalName}DbEntity";
        public string DbSetName => $"{PhysicalName}DbSet";

        internal bool UseKeyInsteadOfName { get; }
        internal AggregateBuildOption Options { get; }

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return Id;
        }

        public override string ToString() => $"Aggregate[{Id}]";

        internal const string KEYEQUALS = "KeyEquals";
    }

    internal static class AggregateExtensions {

        internal static bool IsRoot(this GraphNode<Aggregate> graphNode) {
            return graphNode.GetRoot() == graphNode;
        }
        internal static GraphNode<Aggregate> GetRoot(this GraphNode<Aggregate> graphNode) {
            return graphNode.EnumerateAncestorsAndThis().First();
        }
        internal static GraphEdge<Aggregate>? GetParent(this GraphNode<Aggregate> graphNode) {
            return graphNode.EnumerateAncestors().LastOrDefault();
        }

        /// <summary>
        /// 祖先を列挙する。ルート要素が先。
        /// </summary>
        internal static IEnumerable<GraphNode<Aggregate>> EnumerateAncestorsAndThis(this GraphNode<Aggregate> graphNode) {
            foreach (var ancestor in graphNode.EnumerateAncestors()) {
                yield return ancestor.Initial;
            }
            yield return graphNode;
        }
        /// <summary>
        /// 祖先を列挙する。ルート要素が先。
        /// </summary>
        internal static IEnumerable<GraphEdge<Aggregate>> EnumerateAncestors(this GraphNode<Aggregate> graphNode) {
            var stack = new Stack<GraphEdge<Aggregate>>();
            GraphEdge<Aggregate>? edge = null;
            GraphNode<Aggregate>? node = graphNode;
            while (true) {
                edge = node.In
                    .SingleOrDefault(edge => edge.IsParentChild())?
                    .As<Aggregate>();

                if (edge == null) break;
                stack.Push(edge);

                node = edge.Initial.As<Aggregate>();
            }
            while (stack.Count > 0) {
                yield return stack.Pop();
            }
        }

        internal static IEnumerable<GraphNode<Aggregate>> EnumerateDescendants(this GraphNode<Aggregate> graphNode) {
            return graphNode.SelectNeighbors(node => node
                .Out
                .Where(edge => edge.IsParentChild())
                .Select(edge => edge.Terminal.As<Aggregate>()));
        }
        internal static IEnumerable<GraphNode<Aggregate>> EnumerateThisAndDescendants(this GraphNode<Aggregate> graphNode) {
            yield return graphNode;
            foreach (var desc in graphNode.EnumerateDescendants()) {
                yield return desc;
            }
        }

        internal static bool IsInTreeOf(this GraphNode<Aggregate> agg, GraphNode<Aggregate> target) {
            return target
                .GetRoot()
                .EnumerateThisAndDescendants()
                .Contains(agg);
        }
        /// <summary>
        /// この集約がエントリーのツリー内部にあるかどうかを返します。
        /// つまり、ルート集約またはその子集約か、それとも参照されている外部の集約かを表します。
        /// </summary>
        internal static bool IsInEntryTree(this GraphNode<Aggregate> agg) {
            return agg.IsInTreeOf(agg.GetEntry().As<Aggregate>());
        }
        /// <summary>
        /// この集約がエントリーのツリーの外にあるかどうかを返します（<see cref="IsInEntryTree"/> の逆）
        /// </summary>
        internal static bool IsOutOfEntryTree(this GraphNode<Aggregate> agg) {
            return !agg.IsInEntryTree();
        }
        /// <summary>
        /// ルート集約のツリー内部からツリー外部へ出る瞬間のエッジを返します。
        /// 「(エントリー) == ref-to:A => (集約A) == ref-to:B => (集約B)」のような場合で
        /// 集約Bに対してこのメソッドを使用した場合は「ref-to:A」のエッジが返ります。
        /// この集約がルート集約のツリーの外部の集約でない場合は例外になります。
        /// </summary>
        internal static GraphEdge<Aggregate> GetRefEntryEdge(this GraphNode<Aggregate> agg) {
            var entry = agg.GetEntry().As<Aggregate>();
            foreach (var edge in agg.PathFromEntry()) {
                var edge2 = edge.As<Aggregate>();
                if (edge2.Terminal.GetRoot() != entry) return edge2;
            }
            throw new InvalidOperationException($"'{agg}'は'{entry}'のツリー外部の集約ではありません。");
        }

        internal static bool IsChildrenMember(this GraphNode<Aggregate> graphNode) {
            var parent = graphNode.GetParent();
            return parent != null
                && parent.Attributes.TryGetValue(REL_ATTR_MULTIPLE, out var isArray)
                && (bool)isArray!;
        }
        internal static bool IsChildMember(this GraphNode<Aggregate> graphNode) {
            var parent = graphNode.GetParent();
            return parent != null
                && (!parent.Attributes.TryGetValue(REL_ATTR_MULTIPLE, out var isArray) || (bool)isArray! == false)
                && (!parent.Attributes.TryGetValue(REL_ATTR_VARIATIONGROUPNAME, out var groupName) || (string)groupName! == string.Empty);
        }
        internal static bool IsVariationMember(this GraphNode<Aggregate> graphNode) {
            var parent = graphNode.GetParent();
            return parent != null
                && (!parent.Attributes.TryGetValue(REL_ATTR_MULTIPLE, out var isArray) || (bool)isArray! == false)
                && parent.Attributes.TryGetValue(REL_ATTR_VARIATIONGROUPNAME, out var groupName)
                && (string)groupName! != string.Empty;
        }
        /// <summary>
        /// この集約を <see cref="AggregateMember.RelationMember"/> に変換します。
        /// この集約がChild,Children,Variationのいずれでもない場合は例外になります。
        /// </summary>
        internal static AggregateMember.RelationMember AsChildRelationMember(this GraphNode<Aggregate> aggregate) {
            var parentEdge = aggregate.GetParent()
                ?? throw new InvalidOperationException($"{aggregate}の親を取得できません。");

            return parentEdge.Initial
                .GetMembers()
                .OfType<AggregateMember.RelationMember>()
                .SingleOrDefault(rm => rm.MemberAggregate == aggregate)
                ?? throw new InvalidOperationException($"{parentEdge.Initial}のメンバーに{aggregate}がありません。");
        }
        /// <summary>
        /// このエッジを <see cref="AggregateMember.Ref"/> に変換します。
        /// このエッジが参照を表すエッジでない場合は例外になります。
        /// </summary>
        internal static AggregateMember.Ref AsRefMember(this GraphEdge<Aggregate> edge) {
            return edge.Initial
                .GetMembers()
                .OfType<AggregateMember.Ref>()
                .Single(rm => rm.Relation == edge);
        }

        /// <summary>
        /// この集約がDBに保存されるものかどうかを返します。
        /// </summary>
        internal static bool IsStored(this GraphNode<Aggregate> aggregate) {
            var handler = aggregate.GetRoot().Item.Options.Handler;
            return handler == NijoCodeGenerator.Models.WriteModel2.Key;
        }

        /// <summary>
        /// この集約が参照する集約、およびその参照先の祖先を列挙する
        /// </summary>
        internal static IEnumerable<GraphNode<Aggregate>> GetRefsAndTheirAncestorsRecursively(this GraphNode<Aggregate> aggregate) {
            IEnumerable<GraphNode<Aggregate>> Enumerate(GraphNode<Aggregate> agg) {
                foreach (var member in agg.GetMembers()) {
                    if (member is AggregateMember.Ref refMember) {
                        yield return refMember.RefTo;
                        foreach (var item in Enumerate(refMember.RefTo)) {
                            yield return item;
                        }
                    } else if (member is AggregateMember.Parent parent && !agg.IsInTreeOf(aggregate)) {
                        yield return parent.ParentAggregate;
                        foreach (var item in Enumerate(parent.ParentAggregate)) {
                            yield return item;
                        }
                    }
                }
            }
            return Enumerate(aggregate);
        }

        /// <summary>
        /// この集約に対するRefを持っている集約を列挙する。
        /// </summary>
        internal static IEnumerable<GraphEdge<Aggregate>> GetReferedEdges(this GraphNode<Aggregate> graphNode) {
            return graphNode.In
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type! == REL_ATTRVALUE_REFERENCE
                            && edge.Initial.Item is Aggregate)
                .Select(edge => edge.As<Aggregate>());
        }

        /// <summary>
        /// この集約のすべてのメンバーが2次元の表で表現できるかどうかを返します。
        /// </summary>
        internal static bool CanDisplayAllMembersAs2DGrid(this GraphNode<Aggregate> aggregate) {
            return aggregate
                .EnumerateDescendants()
                .All(agg => !agg.IsChildrenMember()
                         && !agg.IsVariationMember());
        }

        /// <summary>
        /// データの流れの順（=Refされている順）に列挙
        /// </summary>
        internal static IEnumerable<GraphNode<Aggregate>> OrderByDataFlow(this IEnumerable<GraphNode<Aggregate>> aggregates) {
            var rest = aggregates
                .Select(root => new {
                    root,
                    refTargets = root
                        .EnumerateThisAndDescendants()
                        .SelectMany(agg => agg.GetMembers())
                        .OfType<AggregateMember.Ref>()
                        .Select(@ref => @ref.RefTo.GetRoot())
                        .ToHashSet(),
                })
                .ToList();
            var index = 0;
            while (true) {
                if (rest.Count == 0) yield break;

                var next = rest[index];

                // 参照先集約が未処理ならば後回し
                var notEnumerated = rest.Where(agg => next.refTargets.Contains(agg.root));
                if (notEnumerated.Any()) {
                    // 集約間の循環参照が存在するなどの場合は無限ループが発生するので例外。
                    // なお循環参照はスキーマ作成時にエラーとする想定
                    if (index + 1 >= rest.Count) throw new InvalidOperationException("集約間のデータの流れを決定できません。");

                    index++;
                    continue;
                }

                // 参照先集約が無い == nextは現在のrestの中で再上流の集約
                yield return next.root;

                rest.Remove(next);
                index = 0;
            }
        }
    }
}
