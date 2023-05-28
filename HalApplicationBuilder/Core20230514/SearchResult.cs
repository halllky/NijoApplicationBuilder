using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514 {
    internal class SearchResult {
        internal SearchResult(GraphNode<EFCoreEntity> efCoreEntity) {
            _efCoreEntity = efCoreEntity;
        }

        private readonly GraphNode<EFCoreEntity> _efCoreEntity;

        internal const string INSTANCE_KEY_PROP_NAME = "__halapp__InstanceKey";

        internal IEnumerable<Member> GetMembers() {
            yield return new Member {
                Owner = this,
                Name = INSTANCE_KEY_PROP_NAME,
                Type = new AggregateMembers.Id(), // TODO
            };
            foreach (var member in GetMembersRecursively(_efCoreEntity)) {
                yield return member;
            }
        }
        private IEnumerable<Member> GetMembersRecursively(GraphNode<EFCoreEntity> dbEntity) {
            foreach (var member in dbEntity.Item.Aggregate.Item.Members) {
                yield return new Member {
                    Owner = this,
                    Name = dbEntity
                        .PathFromEntry()
                        .Select(edge => edge.RelationName)
                        .Union(new[] { member.Name })
                        .Join("_"),
                    Type = member.Type,
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

        internal class Member {
            internal required SearchResult Owner { get; init; }
            internal required string Name { get; init; }
            internal required IAggregateMemberType Type { get; init; }
        }
    }
}
