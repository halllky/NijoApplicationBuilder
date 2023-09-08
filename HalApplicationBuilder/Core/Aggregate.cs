using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal class Aggregate : ValueObject, IAggregateInstance, IEFCoreEntity {
        internal Aggregate(AggregatePath path) {
            Id = new NodeId(path.Value);
            Path = path;
        }

        public NodeId Id { get; }
        internal AggregatePath Path { get; }
        internal string DisplayName => Path.BaseName;
        internal string UniqueId => new HashedString(Path.Value).Guid.ToString().Replace("-", "");

        public string ClassName => DisplayName.ToCSharpSafe();
        public string TypeScriptTypeName => DisplayName.ToCSharpSafe();

        public string EFCoreEntityClassName => $"{DisplayName.ToCSharpSafe()}DbEntity";
        string IEFCoreEntity.ClassName => EFCoreEntityClassName;
        public string DbSetName => EFCoreEntityClassName;
        public IList<IEFCoreEntity.BareColumn> SchalarMembersNotRelatedToAggregate { get; } = new List<IEFCoreEntity.BareColumn>();

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return Id;
        }

        public override string ToString() => $"Aggregate[{Path.Value}]";

        internal class Member : IGraphNode {
            public required NodeId Id { get; init; }
            internal required string Name { get; init; }
            internal required IAggregateMemberType Type { get; init; }
            internal required bool IsPrimary { get; init; }
            internal required bool IsInstanceName { get; init; }
            internal required bool Optional { get; init; }

            public override string ToString() => Id.Value;
        }
    }
}
