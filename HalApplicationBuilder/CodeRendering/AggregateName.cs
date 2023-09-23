using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering {
    internal class AggregateName {
        internal AggregateName(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal IEnumerable<AggregateMember.ValueMember> GetMembers() {
            return _aggregate.GetInstanceNameMembers();
        }
    }
}
