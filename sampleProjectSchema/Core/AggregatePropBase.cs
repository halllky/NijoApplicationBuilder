using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using haldoc.Core.Dto;
using haldoc.Core.Props;
using haldoc.Schema;

namespace haldoc.Core {
    public abstract class AggregatePropBase {

        public ProjectContext Context { get; init; }
        public Aggregate Owner { get; init; }
        public PropertyInfo UnderlyingPropInfo { get; init; }

        public string Key => UnderlyingPropInfo.Name;
        public bool IsPrimaryKey => UnderlyingPropInfo.GetCustomAttribute<KeyAttribute>() != null;

        public abstract IEnumerable<Aggregate> GetChildAggregates();

        public abstract IEnumerable<PropertyTemplate> ToDbColumnModel();

        public abstract IEnumerable<PropertyTemplate> ToListItemMember();

        public object CreateInstanceDefaultValue() {
            throw new NotImplementedException();
        }
    }
}
