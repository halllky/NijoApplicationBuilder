using HalApplicationBuilder.Core.AggregateMembers;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal interface IEFCoreEntity : IGraphNode {
        internal string ClassName { get; }
        internal string DbSetName { get; }
        internal IList<BareColumn> SchalarMembersNotRelatedToAggregate { get; }


        internal const string KEYEQUALS = "KeyEquals";

        internal interface IMember {
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
        internal class BareColumnWithOwner : BareColumn, IMember {
            public required GraphNode<IEFCoreEntity> Owner { get; init; }
        }

        internal class SchalarColumnDefniedInAggregate : IMember {
            public required GraphNode<IEFCoreEntity> Owner { get; init; }
            public required string PropertyName { get; init; }
            public required IAggregateMemberType MemberType { get; init; }
            public required bool IsPrimary { get; init; }
            public required bool IsInstanceName { get; init; }
            public required bool RequiredAtDB { get; init; }
        }
        internal class ParentTablePrimaryKey : IMember {
            public required IMember CorrespondingParentColumn { get; init; }
            public required GraphNode<IEFCoreEntity> Owner { get; init; }

            public string PropertyName => CorrespondingParentColumn.PropertyName;
            public IAggregateMemberType MemberType => CorrespondingParentColumn.MemberType;
            public bool IsInstanceName => CorrespondingParentColumn.IsInstanceName;
            public bool IsPrimary => true;
            public bool RequiredAtDB => true;
        }
        internal class RefTargetTablePrimaryKey : IMember {
            public required GraphEdge<IEFCoreEntity> Relation { get; init; }
            public required IMember CorrespondingRefTargetColumn { get; init; }
            public required GraphNode<IEFCoreEntity> Owner { get; init; }

            public string PropertyName => $"{Relation.RelationName}_{CorrespondingRefTargetColumn.PropertyName}";
            public IAggregateMemberType MemberType => CorrespondingRefTargetColumn.MemberType;
            public bool IsPrimary => Relation.IsPrimary();
            public bool IsInstanceName => CorrespondingRefTargetColumn.IsInstanceName;
            public bool RequiredAtDB => IsPrimary; // TODO XMLでrequired属性を定義できるようにする
        }
        internal class VariationGroupTypeIdentifier : IMember {
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
