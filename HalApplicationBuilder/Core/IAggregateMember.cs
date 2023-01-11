using System;
using System.Collections.Generic;
using System.Reflection;
using HalApplicationBuilder.EntityFramework;

namespace HalApplicationBuilder.Core {
    public interface IAggregateMember {
        string Name { get; }

        Aggregate Owner { get; }
        IEnumerable<Aggregate> GetChildAggregates();

        bool IsPrimaryKey { get; }
        bool IsInstanceName { get; }
        int? InstanceNameOrder { get; }

        bool IsCollection { get; }
        IEnumerable<DbColumn> ToDbColumnModel();
    }
}
