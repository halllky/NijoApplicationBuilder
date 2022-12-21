using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using haldoc.Core.Dto;
using haldoc.Schema;

namespace haldoc.Core.Props {
    public class ChildProperty : IAggregateProp {
        public ChildProperty(PropertyInfo propInfo, Aggregate owner, ProjectContext context) {
            _context = context;
            Owner = owner;
            UnderlyingPropInfo = propInfo;
        }

        private readonly ProjectContext _context;

        public Aggregate Owner { get; }
        public PropertyInfo UnderlyingPropInfo { get; }

        public Aggregate ChildAggregate => _context.GetOrCreateAggregate(
                UnderlyingPropInfo.PropertyType.GetGenericArguments()[0],
                Owner);
        public IEnumerable<Aggregate> GetChildAggregates() {
            yield return ChildAggregate;
        }

        public IEnumerable<EntityColumnDef> ToEFCoreColumn() {
            yield break;
        }

        public object CreateInstanceDefaultValue() {
            throw new NotImplementedException();
        }
    }
}
