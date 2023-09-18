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
                .Select(col => new DbColumn {
                    Owner = dbEntity,
                    InvisibleInGui = col.InvisibleInGui,
                    IsInstanceName = col.IsInstanceName,
                    IsPrimary = col.IsPrimary,
                    MemberType = col.MemberType,
                    PropertyName = col.PropertyName,
                    RequiredAtDB = col.RequiredAtDB,
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

    internal class DbColumnWithoutOwner {
        public required string PropertyName { get; init; }
        public required bool IsPrimary { get; init; }
        public required bool IsInstanceName { get; init; }
        public required IAggregateMemberType MemberType { get; init; }
        public required bool RequiredAtDB { get; init; }
        public required bool InvisibleInGui { get; init; }
    }

    internal class DbColumn : DbColumnWithoutOwner {
        internal required GraphNode<IEFCoreEntity> Owner { get; init; }

        internal IEnumerable<string> GetFullPath(GraphNode<IEFCoreEntity>? since = null) {
            var skip = since != null;
            foreach (var edge in Owner.PathFromEntry()) {
                if (skip && edge.Source?.As<IEFCoreEntity>() == since) skip = false;
                if (skip) continue;
                yield return edge.RelationName;
            }
            yield return PropertyName;
        }

        public override string ToString() {
            return GetFullPath().Join(".");
        }
    }
}
