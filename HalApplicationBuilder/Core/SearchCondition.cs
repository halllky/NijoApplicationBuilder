using HalApplicationBuilder.DotnetEx;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal class SearchCondition {

        internal SearchCondition(GraphNode<EFCoreEntity> efCoreEntity) {
            EFCoreEntity = efCoreEntity;
        }

        internal GraphNode<EFCoreEntity> EFCoreEntity { get; }
        internal string ClassName => $"{EFCoreEntity.GetCorrespondingAggregate().Item.DisplayName.ToCSharpSafe()}SearchCondition";

        internal const string BASE_CLASS_NAME = "SearchConditionBase";
        internal const string PAGE_PROP_NAME = "__halapp__Page";

        internal IEnumerable<Member> GetMembers() {
            IEnumerable<Member> EnumerateRecursively(GraphNode<EFCoreEntity> dbEntity, bool asRef) {
                var path = dbEntity.PathFromEntry().Select(edge => edge.RelationName).ToArray();

                foreach (var member in dbEntity.GetColumns()) {
                    if (asRef && !member.IsPrimary && !member.IsInstanceName) continue;

                    // 参照先の主キーは全く異なるロジックで収集しているので省く
                    if (member.CorrespondingRefTargetColumn != null) continue;

                    yield return new Member {
                        Owner = this,
                        CorrespondingDbMember = member,
                        CorrespondingDbMemberOwner = dbEntity,
                        Name = string.Join("_", path.Union(new[] { member.PropertyName })),
                        Type = member.MemberType,
                    };
                }

                // 参照、子集約
                if (asRef) {
                    foreach (var member in dbEntity
                        .GetRefMembers()
                        .Where(edge => edge.IsPrimary())
                        .SelectMany(edge => EnumerateRecursively(edge.Terminal, asRef: true))) {
                        yield return member;
                    }
                }
                if (!asRef) {
                    foreach (var member in dbEntity
                        .GetRefMembers()
                        .SelectMany(edge => EnumerateRecursively(edge.Terminal, asRef: true))) {
                        yield return member;
                    }
                    foreach (var member in dbEntity
                        .GetChildMembers()
                        .SelectMany(edge => EnumerateRecursively(edge.Terminal, asRef: false))) {
                        yield return member;
                    }
                }
            }

            return EnumerateRecursively(EFCoreEntity, asRef: false);
        }


        internal class Member {
            internal required SearchCondition Owner { get; init; }
            internal required string Name { get; init; }
            internal required IAggregateMemberType Type { get; init; }
            internal required EFCoreEntity.Member CorrespondingDbMember { get; init; }
            internal required GraphNode<EFCoreEntity> CorrespondingDbMemberOwner { get; init; }
        }
    }
}
