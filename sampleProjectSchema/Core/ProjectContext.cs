using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using haldoc.Core.Props;
using haldoc.Schema;

namespace haldoc.Core {
    public class ProjectContext {
        public ProjectContext(Assembly assembly) {
            _assembly = assembly;
        }

        private readonly Assembly _assembly;

        public IEnumerable<Aggregate> BuildAll() {
            foreach (var type in _assembly.GetTypes()) {
                if (type.GetCustomAttribute<AggregateRootAttribute>() == null) continue;
                yield return CreateAggregate(type, null, multiple: false);
            }
        }

        private readonly Dictionary<Type, Aggregate> aggregates = new();
        internal Aggregate CreateAggregate(Type type, Aggregate parent, bool multiple) {
            if (!aggregates.ContainsKey(type)) {
                aggregates.Add(type, new Aggregate(type, parent, multiple, this));
            }
            return aggregates[type];
        }
        internal Aggregate GetAggregate(Type type) {
            if (!aggregates.ContainsKey(type)) throw new InvalidOperationException($"{type.Name} のAggregateは未作成");
            return aggregates[type];
        }

        private readonly Dictionary<Type, IReadOnlyList<IAggregateProp>> properties = new();
        internal IReadOnlyList<IAggregateProp> GetPropsOf(Type type) {
            if (!properties.ContainsKey(type)) {
                var list = new List<IAggregateProp>();
                foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                    if (prop.GetCustomAttribute<NotMappedAttribute>() != null) continue;

                    if (IsSchalarType(prop.PropertyType)) {
                        list.Add(new PrimitiveProperty(prop));
                    } else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Children<>)) {
                        list.Add(new ChildrenProperty(prop, this));
                    } else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nested<>)) {
                        list.Add(new NestedObjectProperty(prop, this));
                    } else if (prop.GetCustomAttributes<VariationAttribute>().Any()) {
                        list.Add(new VariationProperty(prop, this));
                    } else if (prop.PropertyType.IsClass && IsUserDefinedType(prop.PropertyType)) {
                        list.Add(new ReferenceProperty(prop, this));
                    }
                }
                properties.Add(type, list);
            }
            return properties[type];
        }

        internal bool IsUserDefinedType(Type type) {
            return type.Assembly == _assembly;
        }
        internal bool IsSchalarType(Type type) {
            if (type == typeof(string)) return true;
            if (type == typeof(bool)) return true;
            if (type == typeof(int)) return true;
            if (type == typeof(float)) return true;
            if (type == typeof(decimal)) return true;
            if (type.IsEnum) return true;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                var generic = type.GetGenericArguments()[0];
                if (generic == typeof(string)) return true;
                if (generic == typeof(bool)) return true;
                if (generic == typeof(int)) return true;
                if (generic == typeof(float)) return true;
                if (generic == typeof(decimal)) return true;
                if (generic.IsEnum) return true;
            }

            return false;
        }
    }
}
