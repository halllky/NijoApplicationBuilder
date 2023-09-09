using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal class Aggregate : ValueObject, IEFCoreEntity {
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
        public IList<DbColumn.BareColumn> SchalarMembersNotRelatedToAggregate { get; } = new List<DbColumn.BareColumn>();

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return Id;
        }

        public override string ToString() => $"Aggregate[{Path.Value}]";
    }
}
