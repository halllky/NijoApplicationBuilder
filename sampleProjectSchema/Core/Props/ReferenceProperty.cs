using System;
using System.Collections.Generic;
using System.Reflection;
using haldoc.Schema;

namespace haldoc.Core.Props {
    public class ReferenceProperty : IAggregateProp {
        public ReferenceProperty(PropertyInfo prop, Aggregate owner, ProjectContext context) {
            _context = context;
            Owner = owner;
            UnderlyingPropInfo = prop;
        }

        private readonly ProjectContext _context;

        public Aggregate Owner { get; }
        public PropertyInfo UnderlyingPropInfo { get; }

        public IEnumerable<Aggregate> GetChildAggregates() {
            yield break;
        }

        public IEnumerable<EntityColumnDef> ToEFCoreColumn() {
            var aggregate = _context.GetAggregate(UnderlyingPropInfo.PropertyType);
            foreach (var foreignKey in aggregate.ToEFCoreEntity().Keys) {
                yield return new EntityColumnDef {
                    CSharpTypeName = foreignKey.CSharpTypeName,
                    ColumnName = $"{UnderlyingPropInfo.Name}__{foreignKey.ColumnName}",
                };
            }
        }

        public object CreateInstanceDefaultValue() {
            return null;
        }
    }
}
