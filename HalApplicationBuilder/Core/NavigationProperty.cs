using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    /// <summary>
    /// ナビゲーションプロパティ
    /// </summary>
    internal class NavigationProperty : ValueObject {
        internal NavigationProperty(GraphEdge<IEFCoreEntity> relation) {
            _graphEdge = relation;

            Item CreateItem(GraphNode<IEFCoreEntity> owner, bool oppositeIsMany) {
                var opposite = owner == relation.Initial ? relation.Terminal : relation.Initial;

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
                    CSharpTypeName = oppositeIsMany ? $"ICollection<{opposite.Item.ClassName}>" : opposite.Item.ClassName,
                    Initializer = oppositeIsMany ? $"new HashSet<{opposite.Item.ClassName}>()" : null,
                    PropertyName = propertyName,
                    OppositeIsMany = oppositeIsMany,
                    ForeignKeys = owner
                        .GetColumns()
                        .Where(col => col is DbColumn.RefTargetTablePrimaryKey refTargetPk
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
            internal required IEnumerable<DbColumn.IDbColumn> ForeignKeys { get; init; }

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
        internal static IEnumerable<NavigationProperty> GetNavigationProperties(this GraphNode<IEFCoreEntity> efCoreEntity) {
            var parent = efCoreEntity.GetParent();
            if (parent != null)
                yield return new NavigationProperty(parent);

            foreach (var edge in efCoreEntity.GetChildMembers()) {
                yield return new NavigationProperty(edge);
            }
            foreach (var edge in efCoreEntity.GetVariationGroups().SelectMany(group => group.VariationAggregates.Values)) {
                yield return new NavigationProperty(edge);
            }
            foreach (var edge in efCoreEntity.GetChildrenMembers()) {
                yield return new NavigationProperty(edge);
            }
            foreach (var edge in efCoreEntity.GetRefMembers()) {
                yield return new NavigationProperty(edge);
            }
            foreach (var edge in efCoreEntity.GetReferrings()) {
                yield return new NavigationProperty(edge);
            }
        }

        internal static IEnumerable<NavigationProperty> GetNavigationProperties(this GraphNode<Aggregate> aggregate) => aggregate.As<IEFCoreEntity>().GetNavigationProperties();
    }
}
