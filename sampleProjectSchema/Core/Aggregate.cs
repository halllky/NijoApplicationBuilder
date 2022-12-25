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
        private List<Dto.PropertyTemplate> _navigationProperties;
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
                _pk = props.Where(p => p.IsPrimaryKey).SelectMany(p => p.ToDbEntityProperty()).ToList();
                _notPk = props.Where(p => !p.IsPrimaryKey).SelectMany(p => p.ToDbEntityProperty()).ToList();

                if (Parent != null && !_pk.Any() && _hasIndexKey)
                    _pk.Insert(0, new Dto.PropertyTemplate { CSharpTypeName = "int", PropertyName = "Index" });

                if (Parent != null) {
                    _pk.InsertRange(0, Parent.Owner.GetDbTablePK());
                    // stack overflow
                    //_navigationProperties = new() {
                    //    new Dto.PropertyTemplate { CSharpTypeName = $"virtual {Parent.Owner.ToDbTableModel().ClassName}", PropertyName = "Parent" },
                    //};
                }

                _dbTableModel = new Dto.ClassTemplate {
                    ClassName = Name,
                    Properties = _pk.Union(_notPk).ToList(),
                };
            }
            return _dbTableModel;
        }

        public object TransformMvcModelToDbEntity(object mvcModel) {
            var dbEntityType = _context.RuntimeAssembly.GetType($"{_context.GetOutputNamespace(E_Namespace.MvcModel)}.{ToDbTableModel().ClassName}");
            var dbEntity = Activator.CreateInstance(dbEntityType);
            throw new NotImplementedException();
        }


        private Dto.ClassTemplate _searchconditionModel;
        public Dto.ClassTemplate ToSearchConditionModel() {
            if (_searchconditionModel == null) {
                _searchconditionModel = new Dto.ClassTemplate {
                    ClassName = Name + "__SearchCondition",
                    Properties = GetProperties().SelectMany(p => p.ToSearchConditionDtoProperty()).ToList(),
                };
            }
            return _searchconditionModel;
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
        private Dto.ClassTemplate _singleItemModel;
        public Dto.ClassTemplate ToSingleItemModel() {
            if (_singleItemModel == null) {
                _singleItemModel = new Dto.ClassTemplate {
                    ClassName = Name,
                    Properties = GetProperties()
                        .SelectMany(p => p.ToInstanceDtoProperty())
                        .ToList(),
                };
            }
            return _singleItemModel;
        }

        /// <summary>SingleViewレンダリングの入り口</summary>
        public string RenderSingleView(string boundObjectName, int initialIndent) {
            var renderingContext = new AggregateInstanceBuildContext(boundObjectName, initialIndent);
            var template = new AggregateInstance()
            { Aggregate = this,
                RenderingContext = renderingContext,
            };
            var code = string.Join(Environment.NewLine, template
                .TransformText()
                .Split(Environment.NewLine)
                .Select((line, index) => index == 0
                    ? line // 先頭行だけは呼び出し元ttファイル内のインデントがそのまま反映されるので
                    : renderingContext.CurrentIndent + line));

            return code;
        }
        /// <summary>集約ルート以外の場合</summary>
        public string RenderSingleView(AggregateInstanceBuildContext renderingContext) {
            var template = new AggregateInstance()
            {
                Aggregate = this,
                RenderingContext = renderingContext,
            };
            var code = string.Join(Environment.NewLine, template
                .TransformText()
                .Split(Environment.NewLine)
                .Select((line, index) => index == 0
                    ? line // 先頭行だけは呼び出し元ttファイル内のインデントがそのまま反映されるので
                    : renderingContext.CurrentIndent + line));
            //var code = template.TransformText();
            return code;
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

    partial class AggregateInstance {
        public Aggregate Aggregate { get; init; }
        public AggregateInstanceBuildContext RenderingContext { get; init; }
    }
}
