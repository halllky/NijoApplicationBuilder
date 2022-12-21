using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using haldoc.Core.Props;
using haldoc.Schema;

namespace haldoc.Core {
    public class ProjectContext {
        public ProjectContext(string projectName, Assembly assembly) {
            ProjectName = projectName;
            _assembly = assembly;
        }

        public string ProjectName { get; }
        private readonly Assembly _assembly;

        private readonly Dictionary<Type, Aggregate> aggregates = new();
        public IEnumerable<Aggregate> BuildAll() {
            foreach (var type in _assembly.GetTypes()) {
                if (type.GetCustomAttribute<AggregateRootAttribute>() == null) continue;
                yield return GetOrCreateAggregate(type, null);
            }
        }


        internal Aggregate GetOrCreateAggregate(Type type, Aggregate parent, bool asChildren = false) {
            if (!aggregates.ContainsKey(type)) {
                aggregates.Add(type, new Aggregate(type, parent, this, asChildren: asChildren));
            }
            return aggregates[type];
        }
        internal Aggregate GetAggregate(Type type) {
            if (!aggregates.ContainsKey(type)) throw new InvalidOperationException($"{type.Name} のAggregateは未作成");
            return aggregates[type];
        }

        public  object CreateInstance(Type type) {
            var instance = Activator.CreateInstance(type);
            var props = GetAggregate(type).GetProperties();
            foreach (var prop in props) {
                prop.UnderlyingPropInfo.SetValue(instance, prop.CreateInstanceDefaultValue());
            }
            return instance;
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
