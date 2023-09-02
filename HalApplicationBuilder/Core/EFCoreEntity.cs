using HalApplicationBuilder.Core.AggregateMembers;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal class EFCoreEntity : IGraphNode {
        internal EFCoreEntity(Aggregate aggregate) {
            // TODO 集約IDにコロンが含まれるケースの対策
            Id = new NodeId($"DBENTITY::{aggregate.Id}");
            Aggregate = aggregate;
        }

        public NodeId Id { get; }
        private Aggregate Aggregate { get; }

        internal string ClassName => Aggregate.DisplayName.ToCSharpSafe();
        internal string DbSetName => ClassName;

        internal const string KEYEQUALS = "KeyEquals";

        internal class Member : ValueObject {
            internal required GraphNode<EFCoreEntity> Owner { get; init; }
            internal required bool IsPrimary { get; init; }
            internal required bool IsInstanceName { get; init; }
            internal required IAggregateMemberType MemberType { get; init; }
            internal required string CSharpTypeName { get; init; }
            internal required string TypeScriptTypename { get; init; }
            internal required string PropertyName { get; init; }
            internal string? Initializer { get; init; }
            internal bool RequiredAtDB { get; init; }
            internal required Member? CorrespondingParentColumn { get; init; }
            internal required Member? CorrespondingRefTargetColumn { get; init; }

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
        internal NavigationProperty(GraphEdge graphEdge, Config config) {
            _graphEdge = graphEdge;

            var initial = graphEdge.Initial.As<EFCoreEntity>();
            var terminal = graphEdge.Terminal.As<EFCoreEntity>();
            Item CreateItem(GraphNode<EFCoreEntity> owner, bool oppositeIsMany) {
                var opposite = owner == initial ? terminal : initial;
                var entityClass = $"{config.EntityNamespace}.{opposite.As<EFCoreEntity>().Item.ClassName}";

                string propertyName;
                if (owner == terminal && (owner.IsChildMember() || owner.IsChildrenMember() || owner.IsVariationMember())) {
                    propertyName = "Parent";
                } else if (owner == terminal && graphEdge.IsRef()) {
                    propertyName = $"RefferedBy_{initial.Item.ClassName}_{graphEdge.RelationName}";
                } else {
                    propertyName = graphEdge.RelationName;
                }

                return new Item {
                    Owner = owner,
                    CSharpTypeName = oppositeIsMany ? $"ICollection<{entityClass}>" : entityClass,
                    Initializer = oppositeIsMany ? $"new HashSet<{entityClass}>()" : null,
                    PropertyName = propertyName,
                    OppositeIsMany = oppositeIsMany,
                    ForeignKeys = owner
                        .GetColumns()
                        .Where(m => m.IsPrimary && m.CorrespondingParentColumn?.Owner == opposite
                                 || m.CorrespondingRefTargetColumn?.Owner == opposite),
                };
            }

            if (terminal.IsChildMember()) {
                Principal = CreateItem(initial, oppositeIsMany: false);
                Relevant = CreateItem(terminal, oppositeIsMany: false);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade;

            } else if (terminal.IsVariationMember()) {
                Principal = CreateItem(initial, oppositeIsMany: false);
                Relevant = CreateItem(terminal, oppositeIsMany: false);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade;

            } else if (terminal.IsChildrenMember()) {
                Principal = CreateItem(initial, oppositeIsMany: true);
                Relevant = CreateItem(terminal, oppositeIsMany: false);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade;

            } else if (graphEdge.IsRef()) {
                Principal = CreateItem(terminal, oppositeIsMany: true);
                Relevant = CreateItem(initial, oppositeIsMany: false);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.NoAction;

            } else {
                throw new ArgumentException("Graph edge can not be converted to navigation property.", nameof(graphEdge));
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
            internal required IEnumerable<EFCoreEntity.Member> ForeignKeys { get; init; }

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
        internal static IEnumerable<EFCoreEntity.Member> GetColumns(this GraphNode<EFCoreEntity> dbEntity) {
            // 親の主キー
            var parent = dbEntity.GetParent()?.Initial;
            if (parent != null) {
                foreach (var parentPkColumn in parent.GetColumns().Where(c => c.IsPrimary)) {
                    yield return new EFCoreEntity.Member {
                        Owner = dbEntity,
                        PropertyName = parentPkColumn.PropertyName,
                        IsPrimary = true,
                        IsInstanceName = false,
                        MemberType = parentPkColumn.MemberType,
                        CSharpTypeName = parentPkColumn.CSharpTypeName,
                        TypeScriptTypename = parentPkColumn.TypeScriptTypename,
                        RequiredAtDB = true,
                        CorrespondingParentColumn = parentPkColumn,
                        CorrespondingRefTargetColumn = null,
                    };
                }
            }
            // スカラー値
            var aggregate = dbEntity.GetCorrespondingAggregate();
            if (aggregate != null) {
                foreach (var member in aggregate.Item.Members) {
                    yield return new EFCoreEntity.Member {
                        Owner = dbEntity,
                        PropertyName = member.Name,
                        IsPrimary = member.IsPrimary,
                        IsInstanceName = member.IsInstanceName,
                        MemberType = member.Type,
                        CSharpTypeName = member.Type.GetCSharpTypeName(),
                        TypeScriptTypename = member.Type.GetTypeScriptTypeName(),
                        RequiredAtDB = member.IsPrimary, // TODO XMLでrequired属性を定義できるようにする
                        CorrespondingParentColumn = null,
                        CorrespondingRefTargetColumn = null,
                    };
                }
            }
            // Ref
            foreach (var edge in dbEntity.GetRefMembers()) {
                foreach (var refTargetPk in edge.Terminal.GetColumns().Where(c => c.IsPrimary)) {
                    yield return new EFCoreEntity.Member {
                        Owner = dbEntity,
                        PropertyName = $"{edge.RelationName}_{refTargetPk.PropertyName}",
                        IsPrimary = edge.IsPrimary(),
                        IsInstanceName = edge.IsInstanceName(),
                        MemberType = refTargetPk.MemberType,
                        CSharpTypeName = refTargetPk.CSharpTypeName,
                        TypeScriptTypename = refTargetPk.TypeScriptTypename,
                        RequiredAtDB = edge.IsPrimary(), // TODO XMLでrequired属性を定義できるようにする
                        CorrespondingParentColumn = null,
                        CorrespondingRefTargetColumn = refTargetPk,
                    };
                }
            }
            // リレーション
            foreach (var group in dbEntity.GetVariationGroups()) {
                // variationの型番号
                yield return new EFCoreEntity.Member {
                    Owner = dbEntity,
                    PropertyName = group.GroupName.ToCSharpSafe(),
                    IsPrimary = false, // TODO Variationを主キーに設定できないの不便では？
                    IsInstanceName = false,
                    MemberType = new VariationSwitch(),
                    CSharpTypeName = "string",
                    TypeScriptTypename = "string",
                    Initializer = "default",
                    RequiredAtDB = true,
                    CorrespondingParentColumn = null,
                    CorrespondingRefTargetColumn = null,
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
