using HalApplicationBuilder.Core20230514.AggregateMembers;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514 {
    internal class EFCoreEntity : IGraphNode {
        internal EFCoreEntity(GraphNode<Aggregate> aggregate) {
            Aggregate = aggregate;
        }

        public NodeId Id => Aggregate.Item.Id;
        internal GraphNode<Aggregate> Aggregate { get; }

        internal string ClassName => Aggregate.Item.DisplayName.ToCSharpSafe();
        internal string DbSetName => ClassName;

        internal IEnumerable<Member> GetColumns() {
            // スカラー値
            foreach (var member in Aggregate.Item.Members) {
                yield return new Member {
                    Owner = this,
                    PropertyName = member.Name,
                    IsPrimary = member.IsPrimary,
                    IsInstanceName = member.IsInstanceName,
                    MemberType = member.Type,
                    CSharpTypeName = member.Type.GetCSharpTypeName(),
                    RequiredAtDB = member.IsPrimary, // TODO XMLでrequired属性を定義できるようにする
                };
            }
            // リレーション
            foreach (var edge in Aggregate.GetVariationMembers()) {
                // variationの型番号
                yield return new Member {
                    Owner = this,
                    PropertyName = edge.RelationName.ToCSharpSafe(),
                    IsPrimary = edge.IsPrimary(),
                    IsInstanceName = edge.IsInstanceName(),
                    MemberType = new EnumList(),
                    CSharpTypeName = "int",
                    Initializer = "default",
                    RequiredAtDB = true,
                };
            }
        }

        internal class Member : ValueObject {
            internal required EFCoreEntity Owner { get; init; }
            internal required bool IsPrimary { get; init; }
            internal required bool IsInstanceName { get; init; }
            internal required IAggregateMemberType MemberType { get; init; }
            internal required string CSharpTypeName { get; init; }
            internal required string PropertyName { get; init; }
            internal string? Initializer { get; init; }
            internal bool RequiredAtDB { get; init; }

            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return Owner;
                yield return PropertyName;
            }
        }
    }


    /// <summary>
    /// ナビゲーションプロパティ
    /// </summary>
    internal class NavigationProperty : ValueObject {
        internal NavigationProperty(GraphEdge<EFCoreEntity> graphEdge, Config config) {
            _graphEdge = graphEdge;

            Item CreateItem(GraphNode<EFCoreEntity> owner, bool oppositeIsMany) {
                var opposite = owner == graphEdge.Initial ? graphEdge.Terminal : graphEdge.Initial;
                var entityClass = $"{config.EntityNamespace}.{opposite.Item.ClassName}";
                return new Item {
                    Owner = owner,
                    CSharpTypeName = oppositeIsMany ? $"ICollection<{entityClass}>" : entityClass,
                    Initializer = oppositeIsMany ? $"new HashSet<{entityClass}>()" : null,
                    PropertyName = (graphEdge.IsChild() || graphEdge.IsChildren() || graphEdge.IsVariation()) && owner == graphEdge.Terminal
                        ? "Parent"
                        : graphEdge.RelationName,
                    OppositeIsMany = oppositeIsMany,
                    ForeignKeys = opposite.Item
                        .GetColumns()
                        .Where(m => m.IsPrimary),
                };
            }

            if (graphEdge.IsChild()) {
                Principal = CreateItem(graphEdge.Initial, oppositeIsMany: false);
                Relevant = CreateItem(graphEdge.Terminal, oppositeIsMany: false);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade;

            } else if (graphEdge.IsVariation()) {
                Principal = CreateItem(graphEdge.Initial, oppositeIsMany: false);
                Relevant = CreateItem(graphEdge.Terminal, oppositeIsMany: false);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade;

            } else if (graphEdge.IsChildren()) {
                Principal = CreateItem(graphEdge.Initial, oppositeIsMany: true);
                Relevant = CreateItem(graphEdge.Terminal, oppositeIsMany: false);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade;

            } else if (graphEdge.IsRef()) {
                Principal = CreateItem(graphEdge.Initial, oppositeIsMany: false);
                Relevant = CreateItem(graphEdge.Terminal, oppositeIsMany: true);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade;

            } else {
                throw new ArgumentException("Graph edge can not be converted to navigation property.", nameof(graphEdge));
            }
        }
        private readonly GraphEdge<EFCoreEntity> _graphEdge;

        /// <summary>
        /// 主たるエンティティ側のナビゲーションプロパティ
        /// </summary>
        internal Item Principal { get; }
        /// <summary>
        /// 従たるエンティティ側のナビゲーションプロパティ
        /// </summary>
        internal Item Relevant { get; }
        internal class Item {
            internal required GraphNode<EFCoreEntity> Owner { get; init; }
            internal required string CSharpTypeName { get; init; }
            internal required string PropertyName { get; init; }
            internal required bool OppositeIsMany { get; init; }
            internal string? Initializer { get; init; }
            internal required IEnumerable<EFCoreEntity.Member> ForeignKeys { get; init; }
        }

        internal Microsoft.EntityFrameworkCore.DeleteBehavior OnPrincipalDeleted { get; init; }

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return _graphEdge;
        }
    }

    internal static class EFCoreEntityExtensions {
        internal static IEnumerable<NavigationProperty> GetNavigationProperties(this GraphNode<EFCoreEntity> efCoreEntity, Config config) {
            foreach (var edge in efCoreEntity.InAndOut) {
                yield return new NavigationProperty(edge, config);
            }
        }
    }
}
