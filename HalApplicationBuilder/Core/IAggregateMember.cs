using System;
using System.Collections.Generic;
using HalApplicationBuilder.EntityFramework;

namespace HalApplicationBuilder.Core {
    public interface IAggregateMember {
        string Name { get; }

        Aggregate Owner { get; }
        IEnumerable<Aggregate> GetChildAggregates();

        bool IsPrimaryKey { get; }
        bool IsCollection { get; }
        IEnumerable<DbColumn> ToDbColumnModel();
    }
}
