using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Core {
    public interface IApplicationSchema {
        IEnumerable<Aggregate> AllAggregates();
        IEnumerable<Aggregate> RootAggregates();
        Aggregate FindByType(Type type);
        Aggregate FindByPath(string aggregatePath);
    }
}