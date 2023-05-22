using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514 {
    internal class SearchCondition {

        internal SearchCondition(GraphNode<EFCoreEntity> efCoreEntity) {
            _efCoreEntity = efCoreEntity;
        }

        private readonly GraphNode<EFCoreEntity> _efCoreEntity;

        internal IEnumerable<Member> GetMembers() {
            IEnumerable<Member> EnumerateRecursively(GraphNode<EFCoreEntity> dbEntity) {
                var dbEntityAsNeighbor = dbEntity as NeighborNode<EFCoreEntity>;
                var thisIsRef = dbEntityAsNeighbor != null
                    && (string)dbEntityAsNeighbor.Source.Attributes[AppSchema.REL_ATTR_RELATION_TYPE] == AppSchema.REL_ATTRVALUE_REFERENCE;
                var path = dbEntityAsNeighbor != null
                    ? dbEntityAsNeighbor.PathFromEntry().Select(edge => edge.RelationName).ToArray()
                    : Array.Empty<string>();

                foreach (var member in dbEntity.Item.GetMembers()) {
                    if (thisIsRef
                        && !member.CorrespondingAggregateMember.IsPrimary
                        && !member.CorrespondingAggregateMember.IsInstanceName) continue;

                    yield return new Member {
                        Owner = this,
                        CorrespondingDbMember = member,
                        Name = string.Join("_", path.Union(new[] { member.PropertyName })),
                        Type = member.CorrespondingAggregateMember.Type,
                    };
                }

                // 参照、子集約
                foreach (var edge in dbEntity.Out) {
                    // 隣の集約まで辿る条件
                    var enumerate = false;
                    if (thisIsRef) {
                        if ((string)edge.Attributes[AppSchema.REL_ATTR_RELATION_TYPE] == AppSchema.REL_ATTRVALUE_REFERENCE
                            && (bool)edge.Attributes[AppSchema.REL_ATTR_IS_PRIMARY])
                            enumerate = true;
                    } else {
                        if ((string)edge.Attributes[AppSchema.REL_ATTR_RELATION_TYPE] == AppSchema.REL_ATTRVALUE_REFERENCE)
                            enumerate = true;
                        if ((string)edge.Attributes[AppSchema.REL_ATTR_RELATION_TYPE] == AppSchema.REL_ATTRVALUE_PARENT_CHILD
                            && !edge.Attributes.ContainsKey(AppSchema.REL_ATTR_MULTIPLE))
                            enumerate = true;
                    }
                    if (!enumerate) continue;

                    // 再帰処理
                    foreach (var member in EnumerateRecursively(edge.Terminal)) {
                        yield return member;
                    }
                }
            }

            return EnumerateRecursively(_efCoreEntity);
        }


        internal class Member : ValueObject {
            internal required SearchCondition Owner { get; init; }
            internal required string Name { get; init; }
            internal required IAggregateMemberType Type { get; init; }
            internal required EFCoreEntity.Member CorrespondingDbMember { get; init; }

            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return Owner;
                yield return Name;
            }
        }
    }
}
