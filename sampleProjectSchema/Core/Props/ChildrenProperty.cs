using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using haldoc.Core.Dto;
using haldoc.Schema;

namespace haldoc.Core.Props {
    public class ChildrenProperty : IAggregateProp {
        public ChildrenProperty(PropertyInfo propInfo, Aggregate owner, ProjectContext context) {
            _context = context;
            Owner = owner;
            UnderlyingPropInfo = propInfo;
        }

        private readonly ProjectContext _context;

        public Aggregate Owner { get; }
        public PropertyInfo UnderlyingPropInfo { get; }

        public IEnumerable<Aggregate> GetChildAggregates() {
            yield return _context.GetOrCreateAggregate(
                UnderlyingPropInfo.PropertyType.GetGenericArguments()[0],
                Owner,
                asChildren: true);
        }

        public IEnumerable<EntityColumnDef> ToEFCoreColumn() {
            yield break;
        }

        public object CreateInstanceDefaultValue() {
            return Activator.CreateInstance(UnderlyingPropInfo.PropertyType);
        }
    }
}
