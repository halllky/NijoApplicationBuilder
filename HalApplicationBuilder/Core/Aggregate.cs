using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal class Aggregate : ValueObject, IEFCoreEntity {
        internal Aggregate(TreePath path, AggregateBuildOption options) {
            Id = path.ToGraphNodeId();
            _path = path;
            Options = options;
        }

        public NodeId Id { get; }
        private readonly TreePath _path;
        internal string DisplayName => _path.BaseName;
        internal string UniqueId => new HashedString(_path.ToString()).Guid.ToString().Replace("-", "");

        public string ClassName => DisplayName.ToCSharpSafe();
        public string TypeScriptTypeName => DisplayName.ToCSharpSafe();

        public string EFCoreEntityClassName => $"{DisplayName.ToCSharpSafe()}DbEntity";
        string IEFCoreEntity.ClassName => EFCoreEntityClassName;
        public string DbSetName => EFCoreEntityClassName;
        public IList<DbColumnWithoutOwner> SchalarMembersNotRelatedToAggregate { get; } = new List<DbColumnWithoutOwner>();

        internal AggregateBuildOption Options { get; }

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return Id;
        }

        public override string ToString() => $"Aggregate[{_path}]";
    }
}
