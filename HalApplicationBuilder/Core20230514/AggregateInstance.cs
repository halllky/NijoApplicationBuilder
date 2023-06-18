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
        internal const string BASE_CLASS_NAME = "AggregateInstanceBase";
        internal const string TO_DB_ENTITY_METHOD_NAME = "ToDbEntity";
        internal const string FROM_DB_ENTITY_METHOD_NAME = "FromDbEntity";

        internal IEnumerable<Member> GetMembers() {
            foreach (var column in _dbEntity.Item.GetColumns()) {
                yield return new Member {
                    CorrespondingDbColumn = column,
                    CSharpTypeName = column.CSharpTypeName,
                    PropertyName = column.PropertyName,
                };
            }
        }
        internal class Member {
            internal required EFCoreEntity.Member CorrespondingDbColumn { get; init; }
            internal required string CSharpTypeName { get; init; }
            internal required string PropertyName { get; init; }
        }
    }
}
