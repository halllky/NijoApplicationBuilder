using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using haldoc.Core.Props;
using haldoc.Schema;

namespace haldoc.Core {
    public class Aggregate {
        public Aggregate(Type underlyingType, Aggregate parent, bool hasIndexKey, ProjectContext context) {
            UnderlyingType = underlyingType;
            Parent = parent;
            _hasIndexKey = hasIndexKey;
            _context = context;
        }

        public Type UnderlyingType { get; }
        public Aggregate Parent { get; }
        private readonly bool _hasIndexKey;
        private readonly ProjectContext _context;

        public bool IsRoot => UnderlyingType.GetCustomAttribute<AggregateRootAttribute>() != null;

        public IReadOnlyList<IAggregateProp> GetProperties() {
            return _context.GetPropsOf(UnderlyingType);
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
                if (Parent != null) keys.AddRange(Parent.ToEFCoreEntity().Keys);
                if (_hasIndexKey && !definedKeys.Any()) keys.Add(new EntityColumnDef { ColumnName = "Index", CSharpTypeName = "int" });
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
