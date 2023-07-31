using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal class SearchResult {
        internal SearchResult(GraphNode<EFCoreEntity> efCoreEntity) {
            _efCoreEntity = efCoreEntity;
        }

        private readonly GraphNode<EFCoreEntity> _efCoreEntity;
        internal string ClassName => $"{_efCoreEntity.GetCorrespondingAggregate().Item.DisplayName.ToCSharpSafe()}SearchResult";

        internal const string BASE_CLASS_NAME = "SearchResultBase";
        internal const string INSTANCE_KEY_PROP_NAME = "__halapp__InstanceKey";
        internal const string INSTANCE_NAME_PROP_NAME = "__halapp__InstanceName";

        internal IEnumerable<Member> GetMembers() {
            IEnumerable<Member> GetMembersRecursively(GraphNode<EFCoreEntity> dbEntity) {
                foreach (var column in dbEntity.GetColumns()) {
                    // 参照先の主キーは次のforeachで作成するので省く
                    if (column.CorrespondingRefTargetColumn != null) continue;

                    yield return new Member {
                        IsKey = dbEntity == _efCoreEntity && column.IsPrimary,
                        IsName = dbEntity == _efCoreEntity && column.IsInstanceName,
                        Owner = this,
                        Name = dbEntity
                            .PathFromEntry()
                            .Select(edge => edge.RelationName)
                            .Union(new[] { column.PropertyName })
                            .Join("_"),
                        Type = column.MemberType,
                        CorrespondingDbMember = column,
                        CorrespondingDbMemberOwner = dbEntity,
                    };
                }

                var refMembers = dbEntity
                    .GetRefMembers()
                    .SelectMany(edge => GetMembersRecursively(edge.Terminal));
                foreach (var member in refMembers) {
                    yield return member;
                }

                var childMembers = dbEntity
                    .GetChildMembers()
                    .SelectMany(edge => GetMembersRecursively(edge.Terminal));
                foreach (var member in childMembers) {
                    yield return member;
                }
            }

            return GetMembersRecursively(_efCoreEntity);
        }

        internal class Member {
            internal required bool IsKey { get; init; }
            internal required bool IsName { get; init; }
            internal required SearchResult Owner { get; init; }
            internal required string Name { get; init; }
            internal required IAggregateMemberType Type { get; init; }
            internal required EFCoreEntity.Member CorrespondingDbMember { get; init; }
            internal required GraphNode<EFCoreEntity> CorrespondingDbMemberOwner { get; init; }
        }
    }
}
