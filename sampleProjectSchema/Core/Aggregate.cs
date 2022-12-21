using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using haldoc.Core.Props;
using haldoc.Schema;

namespace haldoc.Core {
    public class Aggregate {
        public Aggregate(Type underlyingType, Aggregate parent, ProjectContext context, bool asChildren = false) {
            UnderlyingType = underlyingType;
            Parent = parent;
            _hasIndexKey = asChildren;
            _context = context;
        }

        public Type UnderlyingType { get; }
        public Aggregate Parent { get; }
        private readonly bool _hasIndexKey;
        private readonly ProjectContext _context;

        public bool IsRoot => UnderlyingType.GetCustomAttribute<AggregateRootAttribute>() != null;

        private List<IAggregateProp> _properties;
        public IReadOnlyList<IAggregateProp> GetProperties() {
            if (_properties == null) {
                _properties = new List<IAggregateProp>();
                foreach (var prop in UnderlyingType.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                    if (prop.GetCustomAttribute<NotMappedAttribute>() != null) continue;

                    if (_context.IsSchalarType(prop.PropertyType)) {
                        _properties.Add(new PrimitiveProperty(prop, this));

                    } else if (prop.PropertyType.IsGenericType
                        && prop.PropertyType.GetGenericTypeDefinition() == typeof(Children<>)) {
                        _properties.Add(new ChildrenProperty(prop, this, _context));

                    } else if (prop.PropertyType.IsGenericType
                        && prop.PropertyType.GetGenericTypeDefinition() == typeof(Child<>)) {

                        if (prop.PropertyType.GetGenericArguments()[0].IsAbstract
                            && prop.GetCustomAttributes<VariationAttribute>().Any())
                            _properties.Add(new VariationProperty(prop, this, _context));
                        else
                            _properties.Add(new ChildProperty(prop, this, _context));

                    } else if (prop.PropertyType.IsClass && _context.IsUserDefinedType(prop.PropertyType)) {
                        _properties.Add(new ReferenceProperty(prop, this, _context));
                    }
                }
            }
            return _properties;
        }

        public IEnumerable<Aggregate> GetDescendantAggregates() {
            var childAggregates = GetProperties().SelectMany(p => p.GetChildAggregates());
            foreach (var child in childAggregates) {
                yield return child;
                foreach (var grandChild in child.GetDescendantAggregates()) {
                    yield return grandChild;
                }
            }
        }

        private EntityDef _entityDef;
        public EntityDef ToEFCoreEntity() {
            if (_entityDef == null) {
                var props = GetProperties();
                var keys = new List<EntityColumnDef>();
                var definedKeys = props.Where(p => p.IsKey).SelectMany(p => p.ToEFCoreColumn()).ToArray();
                if (Parent != null) {
                    keys.AddRange(Parent.ToEFCoreEntity().Keys);
                    if (!definedKeys.Any() && _hasIndexKey)
                        keys.Add(new EntityColumnDef { ColumnName = "Index", CSharpTypeName = "int" });
                }
                keys.AddRange(definedKeys);

                _entityDef = new EntityDef {
                    TableName = UnderlyingType.Name,
                    Keys = keys.ToList(),
                    NonKeyProps = props
                        .Where(p => !p.IsKey)
                        .SelectMany(p => p.ToEFCoreColumn())
                        .ToList(),
                };
            }
            return _entityDef;
        }
    }
}
