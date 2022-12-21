using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public IEnumerable<Aggregate> GetChildAggregates() {
            yield return _context.CreateAggregate(
                UnderlyingPropInfo.PropertyType.GetGenericArguments()[0],
                _context.GetAggregate(UnderlyingPropInfo.DeclaringType),
                multiple: false);
        }

        public IEnumerable<EntityColumnDef> ToEFCoreColumn() {
            yield break;
        }

        public object CreateInstanceDefaultValue() {
            var instance = Activator.CreateInstance(UnderlyingPropInfo.PropertyType);
            var props = _context.GetPropsOf(UnderlyingPropInfo.PropertyType);
            foreach (var prop in props) {
                prop.UnderlyingPropInfo.SetValue(instance, prop.CreateInstanceDefaultValue());
            }
            return instance;
        }
    }
}
