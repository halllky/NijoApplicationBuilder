using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Core {
    internal interface IApplicationSchema {
        IEnumerable<Aggregate> AllAggregates();
        IEnumerable<Aggregate> RootAggregates();
        Aggregate FindByType(Type type);
    }
}