using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace haldoc.Schema {
    public abstract class ApplicationSchema {

        public abstract string ApplicationName { get; }

        private HashSet<Type> _cache;
        public IReadOnlySet<Type> CachedTypes {
            get {
                if (_cache == null) {
                    _cache = new();
                    foreach (var type in Assembly.GetExecutingAssembly().GetTypes()) {
                        if (type.GetCustomAttribute<AggregateRootAttribute>() != null) _cache.Add(type);
                        if (type.GetCustomAttribute<AggregateChildAttribute>() != null) _cache.Add(type);
                    }
                }
                return _cache;
            }
        }

        /// <summary>プロトタイプのためDBは適当な配列で代用している</summary>
        public HashSet<object> DB { get; }

        public ApplicationSchema(HashSet<object> db = null) {
            DB = db ?? new();
        }

        private readonly HashSet<Type> _nonEntityTypes = new();
        private readonly Dictionary<Type, EntityDef> _entityDefCache = new();
        public IEnumerable<EntityDef> GetEFCoreEntities() {
            foreach (var type in CachedTypes) {
                if (TryGetEFCoreEntityDef(type, out var d)) yield return d;
            }
        }
        public bool TryGetEFCoreEntityDef(Type aggregate, out EntityDef entityDef) {
            if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
            if (_nonEntityTypes.Contains(aggregate)) {
                entityDef = null;
                return false;
            }
            if (!_entityDefCache.ContainsKey(aggregate)) {
                var props = aggregate.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                var keys = props.Where(prop => prop.GetCustomAttribute<KeyAttribute>() != null).ToArray();
                var nonKeyProps = props.Where(prop => prop.GetCustomAttribute<KeyAttribute>() == null).ToArray();

                var isNonEntity = !keys.Any();
                if (isNonEntity) {
                    _nonEntityTypes.Add(aggregate);
                    entityDef = null;
                    return false;
                }

                _entityDefCache.Add(aggregate, new EntityDef {
                    TableName = aggregate.Name,
                    Keys = keys.SelectMany(prop => ToEntityProp(prop)).ToList(),
                    NonKeyProps = nonKeyProps.SelectMany(prop => ToEntityProp(prop)).ToList(),
                });
            }
            entityDef = _entityDefCache[aggregate];
            return true;
        }
        private IEnumerable<EntityPropDef> ToEntityProp(PropertyInfo prop) {
            if (prop.GetCustomAttribute<NotMappedAttribute>() != null) yield break;
            if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Relation.Children<>)) {
                yield break; // 配列型
            } else if (prop.GetCustomAttributes<Relation.VariationAttribute>().Any()) {
                yield break; // 多態型
            } else if (prop.PropertyType == typeof(string)) {
                yield return new EntityPropDef {
                    TypeName = "string",
                    ColumnName = prop.Name,
                };
            } else if (prop.PropertyType.IsEnum) {
                yield return new EntityPropDef {
                    TypeName = prop.PropertyType.FullName,
                    ColumnName = prop.Name,
                };
            } else if (prop.PropertyType.IsGenericType
                && prop.PropertyType.GetGenericArguments()[0].IsEnum) {
                yield return new EntityPropDef {
                    TypeName = prop.PropertyType.GetGenericArguments()[0].FullName + "?",
                    ColumnName = prop.Name,
                };
            } else if (TryGetEFCoreEntityDef(prop.PropertyType, out var propType)) {
                var foreignKeys = propType.Keys.Select(fk => new EntityPropDef {
                    TypeName = fk.TypeName,
                    ColumnName = $"{prop.Name}__{fk.ColumnName}",
                });
                foreach (var fk in foreignKeys) yield return fk;
            } else if (prop.PropertyType.IsClass
                && prop.PropertyType.Assembly == Assembly.GetExecutingAssembly()) {
                // complex type
                var nestedProps = prop.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                foreach (var nestedProp in nestedProps) {
                    if (nestedProp.PropertyType != typeof(string))
                        throw new InvalidOperationException($"今のところ入れ子型はstringのみサポート");
                    yield return new EntityPropDef {
                        TypeName = "string",
                        ColumnName = $"{prop.Name}__{nestedProp.Name}",
                    };
                }
            } else {
                throw new InvalidOperationException($"いまのところプロパティ {prop.Name} の型 {prop.PropertyType.Name} はサポートしていません。");
            }
        }
        public object CreateInstance(Type aggregateRoot) {
            var instance = Activator.CreateInstance(aggregateRoot);
            foreach (var prop in aggregateRoot.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Relation.Children<>)) {
                    var children = Activator.CreateInstance(prop.PropertyType);
                    prop.SetValue(instance, children);
                }
            }
            return instance;
        }
    }

    public class EntityDef {
        public string TableName { get; set; }
        public IList<EntityPropDef> Keys { get; set; }
        public IList<EntityPropDef> NonKeyProps { get; set; }
    }
    public class EntityPropDef {
        public string TypeName { get; set; }
        public string ColumnName { get; set; }
    }
}
