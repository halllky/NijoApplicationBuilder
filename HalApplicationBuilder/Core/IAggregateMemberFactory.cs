using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Core {
    public interface IAggregateMemberFactory {
        IEnumerable<IAggregateMember> CreateMembers(Aggregate aggregate);
    }
}
