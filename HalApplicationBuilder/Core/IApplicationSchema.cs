using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Core {
    public interface IApplicationSchema {
        IEnumerable<Aggregate> AllAggregates();
        IEnumerable<Aggregate> RootAggregates();
        Aggregate FindByTypeOrAggregateId(Type type, RefTargetIdAttribute aggregateId);
        Aggregate FindByPath(string aggregatePath);
    }
}