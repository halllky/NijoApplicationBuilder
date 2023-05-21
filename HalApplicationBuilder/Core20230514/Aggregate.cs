using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514 {
    internal class Aggregate : ValueObject, IGraphNode {
        internal Aggregate(AggregatePath path, IReadOnlyCollection<IAggregateMember> members) {
            Id = new NodeId(path.Value);
            Path = path;
            Members = members;
        }

        public NodeId Id { get; }
        internal AggregatePath Path { get; }
        internal string DisplayName => Path.BaseName;
        internal string UniqueId => new HashedString(Path.Value).Guid.ToString().Replace("-", "");
        internal IReadOnlyCollection<IAggregateMember> Members { get; }

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return Id;
        }
    }
}
