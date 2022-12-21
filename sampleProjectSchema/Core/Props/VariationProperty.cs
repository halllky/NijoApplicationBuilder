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

            // 型の妥当性チェック
            var childType = propInfo.PropertyType.GetGenericArguments()[0];
            var cannotAssignable = variations.Where(x => !childType.IsAssignableFrom(x.Type)).ToArray();
            if (cannotAssignable.Any()) throw new InvalidOperationException($"{propInfo.PropertyType.Name} の派生型でない: {string.Join(", ", cannotAssignable.Select(x => x.Type.Name))}");

            _context = context;
            _variations = variations.ToDictionary(v => v.Key, v => context.GetOrCreateAggregate(v.Type, parent));

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
            throw new NotImplementedException();
        }
    }
}
