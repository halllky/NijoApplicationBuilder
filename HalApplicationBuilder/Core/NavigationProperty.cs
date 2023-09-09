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
        internal NavigationProperty(GraphEdge<Aggregate> relation) {
            _graphEdge = relation;

            Item CreateItem(GraphNode<Aggregate> owner, bool oppositeIsMany) {
                var opposite = owner == relation.Initial ? relation.Terminal : relation.Initial;

                string propertyName;
                if (owner == relation.Terminal
                    && (owner.IsChildMember() || owner.IsChildrenMember() || owner.IsVariationMember())
                    && owner.GetParent() == relation) {
                    propertyName = "Parent";
                } else if (owner == relation.Terminal && relation.IsRef()) {
                    propertyName = $"RefferedBy_{relation.Initial.Item.EFCoreEntityClassName}_{relation.RelationName}";
                } else {
                    propertyName = relation.RelationName;
                }

                return new Item {
                    Owner = owner,
                    CSharpTypeName = oppositeIsMany ? $"ICollection<{opposite.Item.EFCoreEntityClassName}>" : opposite.Item.EFCoreEntityClassName,
                    Initializer = oppositeIsMany ? $"new HashSet<{opposite.Item.EFCoreEntityClassName}>()" : null,
                    PropertyName = propertyName,
                    OppositeIsMany = oppositeIsMany,
                    ForeignKeys = owner
                        .GetColumns()
                        .Where(col => col.IsPrimary && col.Owner == opposite),
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
            internal required GraphNode<Aggregate> Owner { get; init; }
            internal required string CSharpTypeName { get; init; }
            internal required string PropertyName { get; init; }
            internal required bool OppositeIsMany { get; init; }
            internal string? Initializer { get; init; }
            internal required IEnumerable<DbColumn.DbColumnBase> ForeignKeys { get; init; }

            internal IEnumerable<string> GetFullPath() {
                return Owner
                    .PathFromEntry()
                    .Select(edge => edge.RelationName)
                    .Concat(new[] { PropertyName });
            }
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
}
