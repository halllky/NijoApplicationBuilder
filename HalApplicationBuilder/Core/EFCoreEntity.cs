using HalApplicationBuilder.Core.AggregateMembers;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal class EFCoreEntity : IGraphNode {
        internal EFCoreEntity(Aggregate aggregate) : this(
            new NodeId($"DBENTITY::{aggregate.Id}"),
            aggregate.DisplayName.ToCSharpSafe()) {
        }
        internal EFCoreEntity(NodeId id, string name, IList<BareColumn>? schalarMembers = null) {
            Id = id;
            ClassName = name;
            SchalarMembersNotRelatedToAggregate = schalarMembers ?? new List<BareColumn>();
        }

        public NodeId Id { get; }
        internal string ClassName { get; }
        internal string DbSetName => ClassName;
        internal IList<BareColumn> SchalarMembersNotRelatedToAggregate;

        internal const string KEYEQUALS = "KeyEquals";

        public override string ToString() {
            return Id.Value;
        }

        internal interface IMember {
            GraphNode<EFCoreEntity> Owner { get; }
            string PropertyName { get; }
            IAggregateMemberType MemberType { get; }
            bool IsPrimary { get;  }
            bool IsInstanceName { get; }
            bool RequiredAtDB { get; }
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
        internal class BareColumnWithOwner : BareColumn, IMember {
            public required GraphNode<EFCoreEntity> Owner { get; init; }
        }

        internal class SchalarColumnDefniedInAggregate : IMember {
            public required GraphNode<EFCoreEntity> Owner { get; init; }
            public required string PropertyName { get; init; }
            public required IAggregateMemberType MemberType { get; init; }
            public required bool IsPrimary { get; init; }
            public required bool IsInstanceName { get; init; }
            public required bool RequiredAtDB { get; init; }
        }
        internal class ParentTablePrimaryKey : IMember {
            public required IMember CorrespondingParentColumn { get; init; }
            public required GraphNode<EFCoreEntity> Owner { get; init; }

            public string PropertyName => CorrespondingParentColumn.PropertyName;
            public IAggregateMemberType MemberType => CorrespondingParentColumn.MemberType;
            public bool IsInstanceName => CorrespondingParentColumn.IsInstanceName;
            public bool IsPrimary => true;
            public bool RequiredAtDB => true;
        }
        internal class RefTargetTablePrimaryKey : IMember {
            public required GraphEdge<EFCoreEntity> Relation { get; init; }
            public required IMember CorrespondingRefTargetColumn { get; init; }
            public required GraphNode<EFCoreEntity> Owner { get; init; }

            public string PropertyName => $"{Relation.RelationName}_{CorrespondingRefTargetColumn.PropertyName}";
            public IAggregateMemberType MemberType => CorrespondingRefTargetColumn.MemberType;
            public bool IsPrimary => Relation.IsPrimary();
            public bool IsInstanceName => CorrespondingRefTargetColumn.IsInstanceName;
            public bool RequiredAtDB => IsPrimary; // TODO XMLでrequired属性を定義できるようにする
        }
        internal class VariationGroupTypeIdentifier : IMember {
            public required VariationGroup<EFCoreEntity> Group { get; init; }
            public required GraphNode<EFCoreEntity> Owner { get; init; }

            public string PropertyName => Group.GroupName;
            public IAggregateMemberType MemberType { get; } = new VariationSwitch();
            public bool IsInstanceName => false;
            public bool IsPrimary => false; // TODO: variationを主キーに設定できるようにする
            public bool RequiredAtDB => true;
        }
    }


    /// <summary>
    /// ナビゲーションプロパティ
    /// </summary>
    internal class NavigationProperty : ValueObject {
        internal NavigationProperty(GraphEdge<EFCoreEntity> relation, Config config) {
            _graphEdge = relation;

            Item CreateItem(GraphNode<EFCoreEntity> owner, bool oppositeIsMany) {
                var opposite = owner == relation.Initial ? relation.Terminal : relation.Initial;
                var entityClass = $"{config.EntityNamespace}.{opposite.Item.ClassName}";

                string propertyName;
                if (owner == relation.Terminal
                    && (owner.IsChildMember() || owner.IsChildrenMember() || owner.IsVariationMember())
                    && owner.GetParent() == relation) {
                    propertyName = "Parent";
                } else if (owner == relation.Terminal && relation.IsRef()) {
                    propertyName = $"RefferedBy_{relation.Initial.Item.ClassName}_{relation.RelationName}";
                } else {
                    propertyName = relation.RelationName;
                }

                return new Item {
                    Owner = owner,
                    CSharpTypeName = oppositeIsMany ? $"ICollection<{entityClass}>" : entityClass,
                    Initializer = oppositeIsMany ? $"new HashSet<{entityClass}>()" : null,
                    PropertyName = propertyName,
                    OppositeIsMany = oppositeIsMany,
                    ForeignKeys = owner
                        .GetColumns()
                        .Where(col => col is EFCoreEntity.RefTargetTablePrimaryKey refTargetPk
                                   && refTargetPk.Relation.Terminal == opposite),
                };
            }

            var parent = relation.Terminal.GetParent()?.Initial;
            if (relation.Terminal.IsChildMember() && relation.Initial == parent) {
                Principal = CreateItem(relation.Initial, oppositeIsMany: false);
                Relevant = CreateItem(relation.Terminal, oppositeIsMany: false);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade;

            } else if (relation.Terminal.IsVariationMember() && relation.Initial == parent) {
                Principal = CreateItem(relation.Initial, oppositeIsMany: false);
                Relevant = CreateItem(relation.Terminal, oppositeIsMany: false);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade;

            } else if (relation.Terminal.IsChildrenMember() && relation.Initial == parent) {
                Principal = CreateItem(relation.Initial, oppositeIsMany: true);
                Relevant = CreateItem(relation.Terminal, oppositeIsMany: false);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade;

            } else if (relation.IsRef()) {
                Principal = CreateItem(relation.Terminal, oppositeIsMany: true);
                Relevant = CreateItem(relation.Initial, oppositeIsMany: false);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.NoAction;

            } else {
                throw new ArgumentException("Graph edge can not be converted to navigation property.", nameof(relation));
            }
        }
        private readonly GraphEdge _graphEdge;

        /// <summary>
        /// 主たるエンティティ側のナビゲーションプロパティ
        /// </summary>
        internal Item Principal { get; }
        /// <summary>
        /// 従たるエンティティ側のナビゲーションプロパティ
        /// </summary>
        internal Item Relevant { get; }
        internal class Item : ValueObject {
            internal required GraphNode<EFCoreEntity> Owner { get; init; }
            internal required string CSharpTypeName { get; init; }
            internal required string PropertyName { get; init; }
            internal required bool OppositeIsMany { get; init; }
            internal string? Initializer { get; init; }
            internal required IEnumerable<EFCoreEntity.IMember> ForeignKeys { get; init; }

            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return Owner;
                yield return PropertyName;
            }
        }

        internal Microsoft.EntityFrameworkCore.DeleteBehavior OnPrincipalDeleted { get; init; }

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return _graphEdge;
        }
    }

    internal static class EFCoreEntityExtensions {
        internal static IEnumerable<EFCoreEntity.IMember> GetColumns(this GraphNode<EFCoreEntity> dbEntity) {
            // 親の主キー
            var parent = dbEntity.GetParent()?.Initial;
            if (parent != null) {
                foreach (var parentPkColumn in parent.GetColumns().Where(c => c.IsPrimary)) {
                    yield return new EFCoreEntity.ParentTablePrimaryKey {
                        Owner = dbEntity,
                        CorrespondingParentColumn = parentPkColumn,
                    };
                }
            }
            // スカラー値
            foreach (var member in dbEntity.Item.SchalarMembersNotRelatedToAggregate) {
                yield return new EFCoreEntity.BareColumnWithOwner {
                    Owner = dbEntity,
                    PropertyName = member.PropertyName,
                    IsPrimary = member.IsPrimary,
                    IsInstanceName = member.IsInstanceName,
                    MemberType = member.MemberType,
                    RequiredAtDB = member.RequiredAtDB,
                };
            }
            var aggregate = dbEntity.GetCorrespondingAggregate();
            if (aggregate != null) {
                foreach (var member in aggregate.GetSchalarMembers()) {
                    yield return new EFCoreEntity.SchalarColumnDefniedInAggregate {
                        Owner = dbEntity,
                        PropertyName = member.Item.Name,
                        IsPrimary = member.Item.IsPrimary,
                        IsInstanceName = member.Item.IsInstanceName,
                        MemberType = member.Item.Type,
                        RequiredAtDB = member.Item.IsPrimary, // TODO XMLでrequired属性を定義できるようにする
                    };
                }
            }
            // Ref
            foreach (var edge in dbEntity.GetRefMembers()) {
                foreach (var refTargetPk in edge.Terminal.GetColumns().Where(c => c.IsPrimary)) {
                    yield return new EFCoreEntity.RefTargetTablePrimaryKey {
                        Owner = dbEntity,
                        Relation = edge,
                        CorrespondingRefTargetColumn = refTargetPk,
                    };
                }
            }
            // リレーション
            foreach (var group in dbEntity.GetVariationGroups()) {
                // variationの型番号
                yield return new EFCoreEntity.VariationGroupTypeIdentifier {
                    Group = group,
                    Owner = dbEntity,
                };
            }
        }
        internal static IEnumerable<NavigationProperty> GetNavigationProperties(this GraphNode<EFCoreEntity> efCoreEntity, Config config) {
            var parent = efCoreEntity.GetParent();
            if (parent != null)
                yield return new NavigationProperty(parent, config);

            foreach (var edge in efCoreEntity.GetChildMembers()) {
                yield return new NavigationProperty(edge, config);
            }
            foreach (var edge in efCoreEntity.GetVariationGroups().SelectMany(group => group.VariationAggregates.Values)) {
                yield return new NavigationProperty(edge, config);
            }
            foreach (var edge in efCoreEntity.GetChildrenMembers()) {
                yield return new NavigationProperty(edge, config);
            }
            foreach (var edge in efCoreEntity.GetRefMembers()) {
                yield return new NavigationProperty(edge, config);
            }
            foreach (var edge in efCoreEntity.GetReferrings()) {
                yield return new NavigationProperty(edge, config);
            }
        }
    }
}
