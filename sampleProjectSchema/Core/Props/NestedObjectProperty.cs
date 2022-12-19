using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using haldoc.Schema;

namespace haldoc.Core.Props {
    public class NestedObjectProperty : IAggregateProp {
        public NestedObjectProperty(PropertyInfo propInfo, ProjectContext context) {
            _context = context;
            UnderlyingPropInfo = propInfo;
            NestedObjectType = propInfo.PropertyType.GetGenericArguments()[0];
        }

        private readonly ProjectContext _context;

        public PropertyInfo UnderlyingPropInfo { get; }
        private Type NestedObjectType { get; }

        public IEnumerable<Aggregate> GetChildAggregates() {
            yield break;
        }

        public IEnumerable<EntityColumnDef> ToEFCoreColumn() {
            var props = _context.GetPropsOf(NestedObjectType);
            foreach (var column in props.SelectMany(p => p.ToEFCoreColumn())) {
                yield return new EntityColumnDef {
                    CSharpTypeName = column.CSharpTypeName,
                    ColumnName = $"{UnderlyingPropInfo.Name}__{column.ColumnName}",
                };
            }
        }

        public object CreateInstanceDefaultValue() {
            return Activator.CreateInstance(UnderlyingPropInfo.PropertyType);
        }
    }
}
