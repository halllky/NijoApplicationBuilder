using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514 {
    internal class AggregateInstance {
        internal AggregateInstance(GraphNode<EFCoreEntity> dbEntity) {
            _dbEntity = dbEntity;
        }

        private readonly GraphNode<EFCoreEntity> _dbEntity;

        internal string ClassName => $"{_dbEntity.Item.Aggregate.Item.DisplayName.ToCSharpSafe()}Instance";

        internal IEnumerable<Member> GetMembers() {
            foreach (var member in _dbEntity.Item.GetColumns()) {
                yield return new Member {
                    CSharpTypeName = member.CSharpTypeName,
                    PropertyName = member.PropertyName,
                };
            }
        }
        internal class Member {
            internal required string CSharpTypeName { get; init; }
            internal required string PropertyName { get; init; }
        }
    }
}
