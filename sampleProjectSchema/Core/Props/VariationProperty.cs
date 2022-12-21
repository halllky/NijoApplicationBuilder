using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using haldoc.Schema;

namespace haldoc.Core.Props {
    public class VariationProperty : IAggregateProp {
        public VariationProperty(PropertyInfo propInfo, Aggregate owner, ProjectContext context) {
            var parent = context.GetAggregate(propInfo.DeclaringType);
            var variations = propInfo.GetCustomAttributes<VariationAttribute>();

            var cannotAssignable = variations.Where(x => !propInfo.PropertyType.IsAssignableFrom(x.Type)).ToArray();
            if (cannotAssignable.Any()) throw new InvalidOperationException($"{propInfo.PropertyType.Name} の派生型でない: {string.Join(", ", cannotAssignable.Select(x => x.Type.Name))}");

            _context = context;
            _variations = variations.ToDictionary(v => v.Key, v => context.CreateAggregate(v.Type, parent, multiple: false));

            Owner = owner;
            UnderlyingPropInfo = propInfo;
        }

        private readonly ProjectContext _context;
        private readonly Dictionary<int, Aggregate> _variations;

        public Aggregate Owner { get; }
        public PropertyInfo UnderlyingPropInfo { get; }

        public IEnumerable<Aggregate> GetChildAggregates() {
            foreach (var variation in _variations) {
                yield return variation.Value;
            }
        }

        public IEnumerable<EntityColumnDef> ToEFCoreColumn() {
            yield return new EntityColumnDef {
                CSharpTypeName = "int?",
                ColumnName = UnderlyingPropInfo.Name,
            };
        }

        public object CreateInstanceDefaultValue() {
            var type = _variations.OrderBy(x => x.Key).First().Value.UnderlyingType;
            var instance = Activator.CreateInstance(type);
            var props = _context.GetPropsOf(UnderlyingPropInfo.PropertyType);
            foreach (var prop in props) {
                if (prop.UnderlyingPropInfo.SetMethod == null) continue;
                prop.UnderlyingPropInfo.SetValue(instance, prop.CreateInstanceDefaultValue());
            }
            return instance;
        }
    }
}
