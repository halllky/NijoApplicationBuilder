using HalApplicationBuilder.Core.AggregateMemberTypes;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal static class DbColumn {

        internal static IEnumerable<DbColumnBase> GetColumns(this GraphNode<Aggregate> aggregate) {
            return aggregate.As<IEFCoreEntity>().GetColumns();
        }
        internal static IEnumerable<DbColumnBase> GetColumns(this GraphNode<IEFCoreEntity> dbEntity) {
            var nonAggregateColumns = dbEntity.Item
                .SchalarMembersNotRelatedToAggregate
                .Select(member => new NonAggregateMemberColumn(dbEntity, member));

            var aggregateColumns = dbEntity.Item is not Aggregate
                ? Enumerable.Empty<DbColumnBase>()
                : dbEntity.As<Aggregate>()
                          .GetMembers()
                          .OfType<AggregateMember.ValueMember>()
                          .Select(member => member.GetDbColumn());

            return nonAggregateColumns.Concat(aggregateColumns);
        }


        internal abstract class DbColumnBase : ValueObject {
            internal abstract GraphNode<IEFCoreEntity> Owner { get; }
            internal abstract string PropertyName { get; }
            internal abstract IAggregateMemberType MemberType { get; }
            internal abstract bool IsPrimary { get; }
            internal abstract bool IsInstanceName { get; }
            internal abstract bool RequiredAtDB { get; }

            internal IEnumerable<string> GetFullPath(GraphNode<IEFCoreEntity>? since = null) {
                var skip = since != null;
                foreach (var edge in Owner.PathFromEntry()) {
                    if (skip && edge.Source?.As<IEFCoreEntity>() == since) skip = false;
                    if (skip) continue;
                    yield return edge.RelationName;
                }
                yield return PropertyName;
            }
            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return Owner;
                yield return PropertyName;
            }
        }

        /// <summary>
        /// 集約に関係しないスカラー値メンバー
        /// </summary>
        internal class ColumnInfo {
            public required string PropertyName { get; init; }
            public required bool IsPrimary { get; init; }
            public required bool IsInstanceName { get; init; }
            public required IAggregateMemberType MemberType { get; init; }
            public bool RequiredAtDB { get; init; }
        }
        internal class NonAggregateMemberColumn : DbColumnBase {
            internal NonAggregateMemberColumn(GraphNode<IEFCoreEntity> owner, ColumnInfo definition) {
                Owner = owner;
                _definition = definition;
            }
            private readonly ColumnInfo _definition;

            internal override GraphNode<IEFCoreEntity> Owner { get; }
            internal override string PropertyName => _definition.PropertyName;
            internal override IAggregateMemberType MemberType => _definition.MemberType;
            internal override bool IsPrimary => _definition.IsPrimary;
            internal override bool IsInstanceName => _definition.IsInstanceName;
            internal override bool RequiredAtDB => _definition.RequiredAtDB;
        }

        internal class AggregateMemberColumn : DbColumnBase {
            internal AggregateMemberColumn(AggregateMember.Schalar schalarProperty) {
                _schalarProperty = schalarProperty;
            }
            private readonly AggregateMember.Schalar _schalarProperty;

            internal override GraphNode<IEFCoreEntity> Owner => _schalarProperty.Owner.As<IEFCoreEntity>();
            internal override string PropertyName => _schalarProperty.PropertyName;
            internal override IAggregateMemberType MemberType => _schalarProperty.MemberType;
            internal override bool IsPrimary => _schalarProperty.IsPrimary;
            internal override bool IsInstanceName => _schalarProperty.IsInstanceName;
            internal override bool RequiredAtDB => _schalarProperty.RequiredAtDB;
        }
        internal class ParentTablePKColumn : DbColumnBase {
            internal ParentTablePKColumn(GraphNode<IEFCoreEntity> owner, DbColumnBase parentColumn) {
                Owner = owner;
                Original = parentColumn;
            }
            internal DbColumnBase Original { get; }

            internal override GraphNode<IEFCoreEntity> Owner { get; }
            internal override string PropertyName => Original.PropertyName;
            internal override IAggregateMemberType MemberType => Original.MemberType;
            internal override bool IsPrimary => true;
            internal override bool IsInstanceName => Original.IsInstanceName;
            internal override bool RequiredAtDB => true;
        }
        internal class RefTargetTablePKColumn : DbColumnBase {
            internal RefTargetTablePKColumn(AggregateMember.Ref refMember, DbColumnBase refTargetColumn) {
                _refMember = refMember;
                Original = refTargetColumn;
            }
            private AggregateMember.Ref _refMember;
            internal DbColumnBase Original { get; }

            internal override GraphNode<IEFCoreEntity> Owner => _refMember.Owner.As<IEFCoreEntity>();
            internal override string PropertyName => $"{_refMember.PropertyName}_{Original.PropertyName}";
            internal override IAggregateMemberType MemberType => Original.MemberType;
            internal override bool IsPrimary => _refMember.IsPrimary;
            internal override bool IsInstanceName => _refMember.IsInstanceName;
            internal override bool RequiredAtDB => _refMember.RequiredAtDB;
        }
        internal class VariationTypeColumn : DbColumnBase {
            internal VariationTypeColumn(AggregateMember.Variation variationGroup) {
                _variationGroup = variationGroup;
            }
            private readonly AggregateMember.Variation _variationGroup;

            internal override GraphNode<IEFCoreEntity> Owner => _variationGroup.Owner.As<IEFCoreEntity>();
            internal override string PropertyName => _variationGroup.PropertyName;
            internal override IAggregateMemberType MemberType => _variationGroup.MemberType;
            internal override bool IsInstanceName => _variationGroup.IsInstanceName;
            internal override bool IsPrimary => _variationGroup.IsPrimary;
            internal override bool RequiredAtDB => _variationGroup.RequiredAtDB;
        }
    }
}
