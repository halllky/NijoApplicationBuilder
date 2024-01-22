using Nijo.Core.AggregateMemberTypes;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core {

    internal static class DbColumnExtensions {

        internal static IEnumerable<DbColumn> GetColumns(this GraphNode<Aggregate> aggregate) {
            return aggregate
                .GetMembers()
                .OfType<AggregateMember.ValueMember>()
                .Select(member => member.GetDbColumn());
        }
    }

    internal class DbColumn {
        internal required GraphNode<Aggregate> Owner { get; init; }
        internal required IReadOnlyMemberOptions Options { get; init; }

        internal IEnumerable<string> GetFullPath(GraphNode<Aggregate>? since = null) {
            var skip = since != null;
            foreach (var edge in Owner.PathFromEntry()) {
                if (skip && edge.Source?.As<Aggregate>() == since) skip = false;
                if (skip) continue;

                if (edge.Source == edge.Terminal
                    && edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, out var type)
                    && (string)type == DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD) {
                    yield return AggregateMember.PARENT_PROPNAME; // 子から親に向かって辿る場合
                } else {
                    yield return edge.RelationName;
                }
            }
            yield return Options.MemberName;
        }

        public override string ToString() {
            return GetFullPath().Join(".");
        }
    }
}
