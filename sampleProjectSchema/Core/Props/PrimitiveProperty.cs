using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using haldoc.Schema;

namespace haldoc.Core.Props {
    public class PrimitiveProperty : IAggregateProp {
        public PrimitiveProperty(PropertyInfo propInfo, Aggregate owner) {
            Owner = owner;
            UnderlyingPropInfo = propInfo;
        }

        public Aggregate Owner { get; }
        public PropertyInfo UnderlyingPropInfo { get; }

        public IEnumerable<Aggregate> GetChildAggregates() {
            yield break;
        }

        public IEnumerable<EntityColumnDef> ToEFCoreColumn() {
            var name = UnderlyingPropInfo.Name;

            if (UnderlyingPropInfo.PropertyType == typeof(string)) yield return new EntityColumnDef { ColumnName = name, CSharpTypeName = "string" };
            else if (UnderlyingPropInfo.PropertyType == typeof(bool)) yield return new EntityColumnDef { ColumnName = name, CSharpTypeName = "bool" };
            else if (UnderlyingPropInfo.PropertyType == typeof(int)) yield return new EntityColumnDef { ColumnName = name, CSharpTypeName = "int" };
            else if (UnderlyingPropInfo.PropertyType == typeof(float)) yield return new EntityColumnDef { ColumnName = name, CSharpTypeName = "float" };
            else if (UnderlyingPropInfo.PropertyType == typeof(decimal)) yield return new EntityColumnDef { ColumnName = name, CSharpTypeName = "decimal" };
            else if (UnderlyingPropInfo.PropertyType.IsEnum) yield return new EntityColumnDef { ColumnName = name, CSharpTypeName = UnderlyingPropInfo.PropertyType.FullName };

            else if (UnderlyingPropInfo.PropertyType.IsGenericType
                && UnderlyingPropInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                var generic = UnderlyingPropInfo.PropertyType.GetGenericArguments()[0];
                if (generic == typeof(string)) yield return new EntityColumnDef { ColumnName = name, CSharpTypeName = "string?" };
                else if (generic == typeof(bool)) yield return new EntityColumnDef { ColumnName = name, CSharpTypeName = "bool?" };
                else if (generic == typeof(int)) yield return new EntityColumnDef { ColumnName = name, CSharpTypeName = "int?" };
                else if (generic == typeof(float)) yield return new EntityColumnDef { ColumnName = name, CSharpTypeName = "float?" };
                else if (generic == typeof(decimal)) yield return new EntityColumnDef { ColumnName = name, CSharpTypeName = "decimal?" };
                else if (generic.IsEnum) yield return new EntityColumnDef { ColumnName = name, CSharpTypeName = UnderlyingPropInfo.PropertyType.GetGenericArguments()[0].FullName + "?" };
            }
        }

        public object CreateInstanceDefaultValue() {
            return UnderlyingPropInfo.PropertyType.IsValueType
                ? Activator.CreateInstance(UnderlyingPropInfo.PropertyType)
                : null;
        }
    }
}
