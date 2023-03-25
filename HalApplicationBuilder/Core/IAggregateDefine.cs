using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Core {
    internal interface IAggregateDefine {
        string DisplayName { get; }
        IEnumerable<AggregateMember> GetMembers(Aggregate owner);
    }
}
