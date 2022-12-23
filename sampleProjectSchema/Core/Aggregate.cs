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

        public Guid GUID => UnderlyingType.GUID;
        public string Name => UnderlyingType.Name;
        public Type UnderlyingType { get; }
        private readonly ProjectContext _context;


        public Aggregate Parent { get; }
        public Aggregate GetRoot() {
            var aggregate = this;
            while (aggregate.Parent != null) {
                aggregate = aggregate.Parent;
            }
            return aggregate;
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


        private List<AggregatePropBase> _properties;
        public IReadOnlyList<AggregatePropBase> GetProperties() {
            if (_properties == null) _properties = _context.GenerateProperties(UnderlyingType, this).ToList();
            return _properties;
        }


        private readonly bool _hasIndexKey;
        private List<Dto.PropertyTemplate> _pk;
        private Dto.ClassTemplate _dbTableModel;
        public IEnumerable<Dto.PropertyTemplate> GetDbTablePK() {
            if (_pk == null) ToDbTableModel();
            return _pk;
        }
        public Dto.ClassTemplate ToDbTableModel() {
            if (_dbTableModel == null) {
                _pk = new List<Dto.PropertyTemplate>();

                if (Parent != null)
                    _pk.AddRange(Parent.GetDbTablePK());

                var props = GetProperties()
                    .Select(p => new { p.IsPrimaryKey, models = p.ToDbColumnModel().ToArray() })
                    .ToArray();
                if (Parent != null && !props.Any(p => p.IsPrimaryKey) && _hasIndexKey)
                    _pk.Add(new Dto.PropertyTemplate { PropertyName = "Index", CSharpTypeName = "int" });
                
                _pk.AddRange(props.Where(p => p.IsPrimaryKey).SelectMany(p => p.models));

                _dbTableModel = new Dto.ClassTemplate {
                    ClassName = UnderlyingType.Name,
                    Properties = props.SelectMany(p => p.models).ToList(),
                };
            }
            return _dbTableModel;
        }


        private Dto.ClassTemplate _filterObjectModel;
        public Dto.ClassTemplate ToSearchConditionModel() {
            throw new NotImplementedException();
        }

        private Dto.ClassTemplate _listViewModel;
        public Dto.ClassTemplate ToListItemModel() {
            if (_listViewModel == null) {
                _listViewModel = new Dto.ClassTemplate {
                    ClassName = UnderlyingType.Name,
                };
            }
            return _listViewModel;
        }
    }
}
