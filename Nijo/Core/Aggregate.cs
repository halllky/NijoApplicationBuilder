using Nijo.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nijo.Core.DirectedEdgeExtensions;

namespace Nijo.Core {
    internal class Aggregate : ValueObject, IEFCoreEntity {
        internal Aggregate(NodeId id, string displayName, bool useKeyInsteadOfName, AggregateBuildOption options) {
            Id = id;
            DisplayName = displayName;
            UseKeyInsteadOfName = useKeyInsteadOfName;
            Options = options;
        }

        public NodeId Id { get; }
        internal string DisplayName { get; }
        internal string UniqueId => new HashedString(Id.ToString()).Guid.ToString().Replace("-", "");

        public string ClassName => DisplayName.ToCSharpSafe();
        public string TypeScriptTypeName => DisplayName.ToCSharpSafe();
        public string EFCoreEntityClassName => $"{DisplayName.ToCSharpSafe()}DbEntity";
        string IEFCoreEntity.ClassName => EFCoreEntityClassName;
        public string DbSetName => EFCoreEntityClassName;

        public IList<IReadOnlyMemberOptions> SchalarMembersNotRelatedToAggregate { get; } = new List<IReadOnlyMemberOptions>();
        internal bool UseKeyInsteadOfName { get; }
        internal AggregateBuildOption Options { get; }

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return Id;
        }

        public override string ToString() => $"Aggregate[{Id}]";
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
                    .SingleOrDefault(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                                          && (string)type == REL_ATTRVALUE_PARENT_CHILD)?
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
            return graphNode.SelectNeighbors(node => node.Out
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var value)
                            && (string)value == REL_ATTRVALUE_PARENT_CHILD)
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

        internal static bool IsChildrenMember(this GraphNode<Aggregate> graphNode) {
            var parent = graphNode.GetParent();
            return parent != null
                && parent.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                && parent.Attributes.TryGetValue(REL_ATTR_MULTIPLE, out var isArray)
                && (bool)isArray;
        }
        internal static bool IsChildMember(this GraphNode<Aggregate> graphNode) {
            var parent = graphNode.GetParent();
            return parent != null
                && parent.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                && (!parent.Attributes.TryGetValue(REL_ATTR_MULTIPLE, out var isArray) || (bool)isArray == false)
                && (!parent.Attributes.TryGetValue(REL_ATTR_VARIATIONGROUPNAME, out var groupName) || (string)groupName == string.Empty);
        }
        internal static bool IsVariationMember(this GraphNode<Aggregate> graphNode) {
            var parent = graphNode.GetParent();
            return parent != null
                && parent.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                && (!parent.Attributes.TryGetValue(REL_ATTR_MULTIPLE, out var isArray) || (bool)isArray == false)
                && parent.Attributes.TryGetValue(REL_ATTR_VARIATIONGROUPNAME, out var groupName)
                && (string)groupName != string.Empty;
        }

        internal static bool IsStored(this GraphNode<Aggregate> aggregate) {
            return aggregate.GetRoot().Item.Options.Type == E_AggreateType.MasterData;
        }
        internal static bool IsCreatable(this GraphNode<Aggregate> aggregate) {
            return aggregate.GetRoot().Item.Options.Type == E_AggreateType.MasterData;
        }
        internal static bool IsEditable(this GraphNode<Aggregate> aggregate) {
            return aggregate.GetRoot().Item.Options.Type == E_AggreateType.MasterData;
        }
        internal static bool IsDeletable(this GraphNode<Aggregate> aggregate) {
            return aggregate.IsCreatable() && aggregate.IsEditable();
        }

        internal static IEnumerable<GraphEdge<Aggregate>> GetReferedEdges(this GraphNode<Aggregate> graphNode) {
            return graphNode.In
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_REFERENCE
                            && edge.Initial.Item is Aggregate)
                .Select(edge => edge.As<Aggregate>());
        }
    }
}
