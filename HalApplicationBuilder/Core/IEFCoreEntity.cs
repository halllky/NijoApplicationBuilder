using HalApplicationBuilder.Core.AggregateMembers;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal interface IEFCoreEntity : IGraphNode {
        internal string ClassName { get; }
        internal string DbSetName { get; }
        internal IList<DbColumn.BareColumn> SchalarMembersNotRelatedToAggregate { get; }


        internal const string KEYEQUALS = "KeyEquals";
    }
}
