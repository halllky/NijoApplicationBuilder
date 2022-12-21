﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace haldoc.Core {
    public interface IAggregateProp {

        Aggregate Owner { get; }
        PropertyInfo UnderlyingPropInfo { get; }

        bool IsKey => UnderlyingPropInfo.GetCustomAttribute<KeyAttribute>() != null;

        IEnumerable<Aggregate> GetChildAggregates();

        IEnumerable<Dto.EntityColumnDef> ToEFCoreColumn();



        object CreateInstanceDefaultValue();
    }
}
