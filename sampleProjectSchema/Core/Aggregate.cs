using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using haldoc.Core.Props;
using haldoc.Schema;

namespace haldoc.Core {
    public class Aggregate {
        public Aggregate(Type underlyingType, AggregatePropBase parent, ProjectContext context, bool asChildren = false) {
            UnderlyingType = underlyingType;
            Parent = parent;
            _hasIndexKey = asChildren;
            _context = context;
        }

        public Guid GUID => UnderlyingType.GUID;
        public string Name => UnderlyingType.Name;
        public Type UnderlyingType { get; }
        private readonly ProjectContext _context;


        public AggregatePropBase Parent { get; }
        public Aggregate GetRoot() {
            var aggregate = this;
            while (aggregate.Parent != null) {
                aggregate = aggregate.Parent.Owner;
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
        private List<Dto.PropertyTemplate> _notPk;
        private Dto.ClassTemplate _dbTableModel;
        public IEnumerable<Dto.PropertyTemplate> GetDbTablePK() {
            if (_pk == null) ToDbTableModel();
            return _pk;
        }
        public IEnumerable<Dto.PropertyTemplate> GetDbTableWithoutPK() {
            if (_notPk == null) ToDbTableModel();
            return _notPk;
        }
        public Dto.ClassTemplate ToDbTableModel() {
            if (_dbTableModel == null) {
                var props = GetProperties();
                _pk = props.Where(p => p.IsPrimaryKey).SelectMany(p => p.ToDbColumnModel()).ToList();
                _notPk = props.Where(p => !p.IsPrimaryKey).SelectMany(p => p.ToDbColumnModel()).ToList();

                if (Parent != null && !_pk.Any() && _hasIndexKey)
                    _pk.Insert(0, new Dto.PropertyTemplate { PropertyName = "Index", CSharpTypeName = "int" });

                if (Parent != null)
                    _pk.InsertRange(0, Parent.Owner.GetDbTablePK());

                _dbTableModel = new Dto.ClassTemplate {
                    ClassName = Name,
                    Properties = _pk.Union(_notPk).ToList(),
                };
            }
            return _dbTableModel;
        }


        private Dto.ClassTemplate _filterObjectModel;
        public Dto.ClassTemplate ToSearchConditionModel() {
            if (_filterObjectModel == null) {
                _filterObjectModel = new Dto.ClassTemplate {
                    ClassName = Name + "__SearchCondition",
                    Properties = GetProperties().SelectMany(p => p.ToSearchConditionModel()).ToList(),
                };
            }
            return _filterObjectModel;
        }

        private Dto.ClassTemplate _listViewModel;
        public Dto.ClassTemplate ToListItemModel() {
            if (_listViewModel == null) {
                _listViewModel = new Dto.ClassTemplate {
                    ClassName = Name + "__ListItem",
                    Properties = GetProperties().SelectMany(p => p.ToListItemModel()).ToList(),
                };
            }
            return _listViewModel;
        }
        public IEnumerable<Dto.PropertyLayoutTemplate> ToSearchConditionLayout(string modelPath) {
            foreach (var prop in GetProperties()) {
                var layout = prop.GenerateSearchConditionLayout(modelPath).ToList();
                if (layout.Any()) yield return new Dto.PropertyLayoutTemplate {
                    PropertyName = prop.Name,
                    Layout = layout,
                };
            }
        }


        public override string ToString() {
            var path = new List<string>();
            var parent = Parent;
            while (parent != null) {
                path.Insert(0, parent.Name);
                parent = parent.Owner.Parent;
            }
            if (path.Any()) {
                path.Insert(0, GetRoot().Name);
                return $"{Name}[{string.Join(".", path)}]";
            } else {
                return Name;
            }
        }
    }
}
