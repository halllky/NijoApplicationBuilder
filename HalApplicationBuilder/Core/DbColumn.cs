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
            // 親の主キー
            var parent = dbEntity.GetParent()?.Initial;
            if (parent != null) {
                foreach (var parentPkColumn in parent.GetColumns().Where(c => c.IsPrimary)) {
                    yield return new ParentTablePrimaryKey(dbEntity, parentPkColumn);
                }
            }
            // 集約で定義されていないカラム
            foreach (var member in dbEntity.Item.SchalarMembersNotRelatedToAggregate) {
                yield return new BareColumnWithOwner(dbEntity, member);
            }
            // 集約に紐づくカラム
            if (dbEntity.Item is Aggregate) {
                foreach (var prop in dbEntity.As<Aggregate>().GetProperties()) {
                    if (prop is AggregateMember.SchalarProperty schalarProp) {
                        yield return new SchalarColumnDefniedInAggregate(schalarProp);

                    } else if (prop is AggregateMember.RefProperty refProp) {
                        var relation = refProp.Relation.As<IEFCoreEntity>();
                        foreach (var refTargetPk in refProp.RefTarget.GetColumns().Where(c => c.IsPrimary)) {
                            yield return new RefTargetTablePrimaryKey(relation, refTargetPk);
                        }

                    } else if (prop is AggregateMember.VariationSwitchProperty switchProp) {
                        yield return new VariationGroupTypeIdentifier(switchProp);
                    }
                }
            }
        }


        internal abstract class DbColumnBase : ValueObject {
            internal abstract GraphNode<IEFCoreEntity> Owner { get; }
            internal abstract string PropertyName { get; }
            internal abstract IAggregateMemberType MemberType { get; }
            internal abstract bool IsPrimary { get; }
            internal abstract bool IsInstanceName { get; }
            internal abstract bool RequiredAtDB { get; }
            internal abstract AggregateMember.AggregateMemberBase? CorrespondingAggregateMember { get; }

            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return Owner;
                yield return PropertyName;
            }
        }

        /// <summary>
        /// 集約に関係しないスカラー値メンバー
        /// </summary>
        internal class BareColumn {
            public required string PropertyName { get; init; }
            public required bool IsPrimary { get; init; }
            public required bool IsInstanceName { get; init; }
            public required IAggregateMemberType MemberType { get; init; }
            public bool RequiredAtDB { get; init; }
        }
        internal class BareColumnWithOwner : DbColumnBase {
            internal BareColumnWithOwner(GraphNode<IEFCoreEntity> owner, BareColumn definition) {
                Owner = owner;
                _definition = definition;
            }
            private readonly BareColumn _definition;

            internal override GraphNode<IEFCoreEntity> Owner { get; }
            internal override string PropertyName => _definition.PropertyName;
            internal override IAggregateMemberType MemberType => _definition.MemberType;
            internal override bool IsPrimary => _definition.IsPrimary;
            internal override bool IsInstanceName => _definition.IsInstanceName;
            internal override bool RequiredAtDB => _definition.RequiredAtDB;
            internal override AggregateMember.AggregateMemberBase? CorrespondingAggregateMember => null;
        }

        internal class SchalarColumnDefniedInAggregate : DbColumnBase {
            internal SchalarColumnDefniedInAggregate(AggregateMember.SchalarProperty schalarProperty) {
                _schalarProperty = schalarProperty;
            }
            private readonly AggregateMember.SchalarProperty _schalarProperty;

            internal override GraphNode<IEFCoreEntity> Owner => _schalarProperty.Owner.As<IEFCoreEntity>();
            internal override string PropertyName => _schalarProperty.PropertyName;
            internal override IAggregateMemberType MemberType => _schalarProperty.MemberType;
            internal override bool IsPrimary => _schalarProperty.IsPrimary;
            internal override bool IsInstanceName => _schalarProperty.IsInstanceName;
            internal override bool RequiredAtDB => _schalarProperty.RequiredAtDB;
            internal override AggregateMember.AggregateMemberBase? CorrespondingAggregateMember => _schalarProperty;
        }
        internal class ParentTablePrimaryKey : DbColumnBase {
            internal ParentTablePrimaryKey(GraphNode<IEFCoreEntity> owner, DbColumnBase parentColumn) {
                Owner = owner;
                _parentColumn = parentColumn;
            }
            private readonly DbColumnBase _parentColumn;

            internal override GraphNode<IEFCoreEntity> Owner { get; }
            internal override string PropertyName => _parentColumn.PropertyName;
            internal override IAggregateMemberType MemberType => _parentColumn.MemberType;
            internal override bool IsPrimary => true;
            internal override bool IsInstanceName => _parentColumn.IsInstanceName;
            internal override bool RequiredAtDB => true;
            internal override AggregateMember.AggregateMemberBase? CorrespondingAggregateMember => _parentColumn.CorrespondingAggregateMember;
        }
        internal class RefTargetTablePrimaryKey : DbColumnBase {
            internal RefTargetTablePrimaryKey(GraphEdge<IEFCoreEntity> relation, DbColumnBase refTargetColumn) {
                Relation = relation;
                _refTargetColumn = refTargetColumn;
            }
            internal GraphEdge<IEFCoreEntity> Relation { get; }
            private readonly DbColumnBase _refTargetColumn;

            internal override GraphNode<IEFCoreEntity> Owner => Relation.Initial;
            internal override string PropertyName => $"{Relation.RelationName}_{_refTargetColumn.PropertyName}";
            internal override IAggregateMemberType MemberType => _refTargetColumn.MemberType;
            internal override bool IsPrimary => Relation.IsPrimary();
            internal override bool IsInstanceName => Relation.IsInstanceName();
            internal override bool RequiredAtDB => Relation.IsRequired();
            internal override AggregateMember.AggregateMemberBase? CorrespondingAggregateMember => _refTargetColumn.CorrespondingAggregateMember;
        }
        internal class VariationGroupTypeIdentifier : DbColumnBase {
            internal VariationGroupTypeIdentifier(AggregateMember.VariationSwitchProperty switchProperty) {
                _switchProperty = switchProperty;
            }
            private readonly AggregateMember.VariationSwitchProperty _switchProperty;

            internal override GraphNode<IEFCoreEntity> Owner => _switchProperty.Owner.As<IEFCoreEntity>();
            internal override string PropertyName => _switchProperty.PropertyName;
            internal override IAggregateMemberType MemberType { get; } = new VariationSwitch();
            internal override bool IsInstanceName => false;
            internal override bool IsPrimary => false; // TODO: variationを主キーに設定できるようにする
            internal override bool RequiredAtDB => true;
            internal override AggregateMember.AggregateMemberBase? CorrespondingAggregateMember => _switchProperty;
        }
    }
}
