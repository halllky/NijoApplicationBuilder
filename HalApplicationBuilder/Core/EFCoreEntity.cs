using HalApplicationBuilder.Core.AggregateMembers;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.Core.IEFCoreEntity;

namespace HalApplicationBuilder.Core {
    internal class EFCoreEntity : IEFCoreEntity {
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
        public string ClassName { get; }
        public string DbSetName => ClassName;
        public IList<BareColumn> SchalarMembersNotRelatedToAggregate { get; }

        public override string ToString() {
            return Id.Value;
        }


    }


    /// <summary>
    /// ナビゲーションプロパティ
    /// </summary>
    internal class NavigationProperty : ValueObject {
        internal NavigationProperty(GraphEdge<IEFCoreEntity> relation, Config config) {
            _graphEdge = relation;

            Item CreateItem(GraphNode<IEFCoreEntity> owner, bool oppositeIsMany) {
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
                        .Where(col => col is IEFCoreEntity.RefTargetTablePrimaryKey refTargetPk
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
            internal required GraphNode<IEFCoreEntity> Owner { get; init; }
            internal required string CSharpTypeName { get; init; }
            internal required string PropertyName { get; init; }
            internal required bool OppositeIsMany { get; init; }
            internal string? Initializer { get; init; }
            internal required IEnumerable<IEFCoreEntity.IMember> ForeignKeys { get; init; }

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
        internal static IEnumerable<IEFCoreEntity.IMember> GetColumns(this GraphNode<IEFCoreEntity> dbEntity) {
            // 親の主キー
            var parent = dbEntity.GetParent()?.Initial;
            if (parent != null) {
                foreach (var parentPkColumn in parent.GetColumns().Where(c => c.IsPrimary)) {
                    yield return new IEFCoreEntity.ParentTablePrimaryKey {
                        Owner = dbEntity,
                        CorrespondingParentColumn = parentPkColumn,
                    };
                }
            }
            // スカラー値
            foreach (var member in dbEntity.Item.SchalarMembersNotRelatedToAggregate) {
                yield return new IEFCoreEntity.BareColumnWithOwner {
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
                    yield return new IEFCoreEntity.SchalarColumnDefniedInAggregate {
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
                    yield return new IEFCoreEntity.RefTargetTablePrimaryKey {
                        Owner = dbEntity,
                        Relation = edge,
                        CorrespondingRefTargetColumn = refTargetPk,
                    };
                }
            }
            // リレーション
            foreach (var group in dbEntity.GetVariationGroups()) {
                // variationの型番号
                yield return new IEFCoreEntity.VariationGroupTypeIdentifier {
                    Group = group,
                    Owner = dbEntity,
                };
            }
        }
        internal static IEnumerable<NavigationProperty> GetNavigationProperties(this GraphNode<IEFCoreEntity> efCoreEntity, Config config) {
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
