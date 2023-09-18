using HalApplicationBuilder.Core.AggregateMemberTypes;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.Core.IEFCoreEntity;

namespace HalApplicationBuilder.Core {
    internal class DbTable : IEFCoreEntity {
        internal DbTable(NodeId id, string name, IList<IReadOnlyMemberOptions>? schalarMembers = null) {
            Id = id;
            ClassName = name;
            SchalarMembersNotRelatedToAggregate = schalarMembers ?? new List<IReadOnlyMemberOptions>();
        }

        public NodeId Id { get; }
        public string ClassName { get; }
        public string DbSetName => ClassName;
        public IList<IReadOnlyMemberOptions> SchalarMembersNotRelatedToAggregate { get; }

        public override string ToString() {
            return Id.Value;
        }
    }
}
