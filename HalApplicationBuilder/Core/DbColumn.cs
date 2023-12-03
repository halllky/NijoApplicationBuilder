using HalApplicationBuilder.Core.AggregateMemberTypes;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {

    internal static class DbColumnExtensions {

        internal static IEnumerable<DbColumn> GetColumns(this GraphNode<Aggregate> aggregate) {
            return aggregate.As<IEFCoreEntity>().GetColumns();
        }
        internal static IEnumerable<DbColumn> GetColumns(this GraphNode<IEFCoreEntity> dbEntity) {
            var nonAggregateColumns = dbEntity.Item
                .SchalarMembersNotRelatedToAggregate
                .Select(options => new DbColumn {
                    Owner = dbEntity,
                    Options = options,
                });
            var aggregateColumns = dbEntity.Item is Aggregate
                ? dbEntity.As<Aggregate>()
                          .GetMembers()
                          .OfType<AggregateMember.ValueMember>()
                          .Select(member => member.GetDbColumn())
                : Enumerable.Empty<DbColumn>();

            return nonAggregateColumns.Concat(aggregateColumns);
        }
    }

    internal class DbColumn {
        internal required GraphNode<IEFCoreEntity> Owner { get; init; }
        internal required IReadOnlyMemberOptions Options { get; init; }

        internal IEnumerable<string> GetFullPath(GraphNode<IEFCoreEntity>? since = null) {
            var skip = since != null;
            foreach (var edge in Owner.PathFromEntry()) {
                if (skip && edge.Source?.As<IEFCoreEntity>() == since) skip = false;
                if (skip) continue;

                if (edge.Source == edge.Terminal
                    && edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, out var type)
                    && (string)type == DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD) {
                    yield return NavigationProperty.PARENT; // 子から親に向かって辿る場合
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
