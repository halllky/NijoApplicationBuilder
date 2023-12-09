using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal class DataView : ValueObject, IGraphNode {
        internal DataView(NodeId id, string displayName) {
            Id = id;
            DisplayName = displayName;
        }

        public NodeId Id { get; }
        internal string DisplayName { get; }

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return Id;
        }
    }

    internal static class DataViewExtensions {
        internal static IEnumerable<AggregateMember.AggregateMemberBase> GetMembers(this GraphNode<DataView> dataView) {
            var memberEdges = dataView.Out.Where(edge =>
                (string)edge.Attributes[DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE] == DirectedEdgeExtensions.REL_ATTRVALUE_HAVING);
            foreach (var edge in memberEdges) {
                yield return new AggregateMember.Schalar(edge.Terminal.As<AggregateMemberNode>());
            }

            var refEdges = dataView.Out.Where(edge =>
                edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == DirectedEdgeExtensions.REL_ATTRVALUE_REFERENCE);
            foreach (var edge in refEdges) {
                var refMember = new AggregateMember.Ref(edge.As<Aggregate>());
                yield return refMember;
                foreach (var refPK in refMember.GetForeignKeys()) yield return refPK;
            }
        }
    }
}
