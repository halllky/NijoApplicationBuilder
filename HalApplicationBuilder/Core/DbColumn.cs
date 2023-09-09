using HalApplicationBuilder.Core.AggregateMemberTypes;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal static class DbColumn {

        internal static IEnumerable<IDbColumn> GetColumns(this GraphNode<Aggregate> aggregate) {
            return aggregate.As<IEFCoreEntity>().GetColumns();
        }
        internal static IEnumerable<IDbColumn> GetColumns(this GraphNode<IEFCoreEntity> dbEntity) {
            // 親の主キー
            var parent = dbEntity.GetParent()?.Initial;
            if (parent != null) {
                foreach (var parentPkColumn in parent.GetColumns().Where(c => c.IsPrimary)) {
                    yield return new ParentTablePrimaryKey {
                        Owner = dbEntity,
                        CorrespondingParentColumn = parentPkColumn,
                    };
                }
            }
            // スカラー値
            foreach (var member in dbEntity.Item.SchalarMembersNotRelatedToAggregate) {
                yield return new BareColumnWithOwner {
                    Owner = dbEntity,
                    PropertyName = member.PropertyName,
                    IsPrimary = member.IsPrimary,
                    IsInstanceName = member.IsInstanceName,
                    MemberType = member.MemberType,
                    RequiredAtDB = member.RequiredAtDB,
                };
            }
            if (dbEntity.Item is Aggregate) {
                foreach (var member in dbEntity.As<Aggregate>().GetMemberNodes()) {
                    yield return new SchalarColumnDefniedInAggregate {
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
                    yield return new RefTargetTablePrimaryKey {
                        Owner = dbEntity,
                        Relation = edge,
                        CorrespondingRefTargetColumn = refTargetPk,
                    };
                }
            }
            // リレーション
            foreach (var group in dbEntity.GetVariationGroups()) {
                // variationの型番号
                yield return new VariationGroupTypeIdentifier {
                    Group = group,
                    Owner = dbEntity,
                };
            }
        }


        internal interface IDbColumn {
            GraphNode<IEFCoreEntity> Owner { get; }
            string PropertyName { get; }
            IAggregateMemberType MemberType { get; }
            bool IsPrimary { get; }
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
        internal class BareColumnWithOwner : BareColumn, IDbColumn {
            public required GraphNode<IEFCoreEntity> Owner { get; init; }
        }

        internal class SchalarColumnDefniedInAggregate : IDbColumn {
            public required GraphNode<IEFCoreEntity> Owner { get; init; }
            public required string PropertyName { get; init; }
            public required IAggregateMemberType MemberType { get; init; }
            public required bool IsPrimary { get; init; }
            public required bool IsInstanceName { get; init; }
            public required bool RequiredAtDB { get; init; }
        }
        internal class ParentTablePrimaryKey : IDbColumn {
            public required IDbColumn CorrespondingParentColumn { get; init; }
            public required GraphNode<IEFCoreEntity> Owner { get; init; }

            public string PropertyName => CorrespondingParentColumn.PropertyName;
            public IAggregateMemberType MemberType => CorrespondingParentColumn.MemberType;
            public bool IsInstanceName => CorrespondingParentColumn.IsInstanceName;
            public bool IsPrimary => true;
            public bool RequiredAtDB => true;
        }
        internal class RefTargetTablePrimaryKey : IDbColumn {
            public required GraphEdge<IEFCoreEntity> Relation { get; init; }
            public required IDbColumn CorrespondingRefTargetColumn { get; init; }
            public required GraphNode<IEFCoreEntity> Owner { get; init; }

            public string PropertyName => $"{Relation.RelationName}_{CorrespondingRefTargetColumn.PropertyName}";
            public IAggregateMemberType MemberType => CorrespondingRefTargetColumn.MemberType;
            public bool IsPrimary => Relation.IsPrimary();
            public bool IsInstanceName => CorrespondingRefTargetColumn.IsInstanceName;
            public bool RequiredAtDB => IsPrimary; // TODO XMLでrequired属性を定義できるようにする
        }
        internal class VariationGroupTypeIdentifier : IDbColumn {
            public required VariationGroup<IEFCoreEntity> Group { get; init; }
            public required GraphNode<IEFCoreEntity> Owner { get; init; }

            public string PropertyName => Group.GroupName;
            public IAggregateMemberType MemberType { get; } = new VariationSwitch();
            public bool IsInstanceName => false;
            public bool IsPrimary => false; // TODO: variationを主キーに設定できるようにする
            public bool RequiredAtDB => true;
        }
    }
}
