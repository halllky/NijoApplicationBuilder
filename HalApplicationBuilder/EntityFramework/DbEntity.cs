using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.Runtime;

namespace HalApplicationBuilder.EntityFramework {
    public class DbEntity {
        internal DbEntity(Aggregate source, DbEntity parent, Config config) {
            Source = source;
            Parent = parent;
            _config = config;
        }

        public Aggregate Source { get; }
        public DbEntity Parent { get; }

        private readonly Config _config;

        public string ClassName
            => Source.UnderlyingType.GetCustomAttribute<TableAttribute>()?.Name
            ?? Source.UnderlyingType.Name;
        public string RuntimeFullName
            => _config.EntityNamespace + "." + ClassName;

        private List<DbColumn> _pk;
        private List<DbColumn> _notPk;
        public IReadOnlyList<DbColumn> PKColumns {
            get {
                if (_pk == null) BuildColumns();
                return _pk;
            }
        }
        public IReadOnlyList<DbColumn> NotPKColumns {
            get {
                if (_notPk == null) BuildColumns();
                return _notPk;
            }
        }
        private void BuildColumns() {
            // 集約で定義されているカラム
            _pk = Source.Members
                .Where(member => member.IsPrimaryKey)
                .SelectMany(member => member.ToDbColumnModel())
                .ToList();
            _notPk = Source.Members
                .Where(member => !member.IsPrimaryKey)
                .SelectMany(member => member.ToDbColumnModel())
                .ToList();
            // 連番
            if (Source.Parent != null && Source.Parent.IsCollection && _pk.Count == 0) {
                _pk.Insert(0, new DbColumn {
                    Virtual = false,
                    CSharpTypeName = "int",
                    PropertyName = $"{Source.Name}_連番",
                    Initializer = null,
                });
            }
            // 親の主キー
            if (Parent != null) {
                _pk.InsertRange(0, Parent.PKColumns);
                // stack overflow
                //_navigationProperties = new() {
                //    new Dto.PropertyTemplate { CSharpTypeName = $"virtual {Parent.Owner.ToDbTableModel().ClassName}", PropertyName = "Parent" },
                //};
            }
        }

        internal IEnumerable<object> ConvertUiInstanceToDbInstance(object instance, RuntimeContext context, object parentInstance) {
            var entity = context.RuntimeAssembly.CreateInstance(RuntimeFullName);

            // 親のPKをコピーする
            if (parentInstance != null) {
                var parentInstanceType = parentInstance.GetType();
                var parentAggregate = context.FindAggregateByRuntimeType(parentInstance.GetType());
                var parentEntityModel = context.DbSchema.GetDbEntity(parentAggregate);
                foreach (var pkColumn in parentEntityModel.PKColumns) {
                    var parentPk = parentInstanceType.GetProperty(pkColumn.PropertyName);
                    var childPk = entity.GetType().GetProperty(pkColumn.PropertyName);
                    var pkValue = parentPk.GetValue(parentInstance);
                    childPk.SetValue(entity, pkValue);
                }
            }

            // instacneModelの各プロパティの値をentityにマッピング
            var set = new HashSet<object> { entity };
            foreach (var member in Source.Members) {
                if (member is not IInstanceConverter converter) continue;
                converter.MapUIToDB(instance, entity, context, set);
            }

            return set;
        }
        internal object ConvertDbInstanceToUiInstance(object dbInstance, RuntimeContext context) {
            var uiModel = context.ViewModelProvider.GetInstanceModel(Source);
            var uiInstance = context.RuntimeAssembly.CreateInstance(uiModel.RuntimeFullName);
            foreach (var member in Source.Members) {
                if (member is not IInstanceConverter converter) continue;
                converter.MapDBToUI(dbInstance, uiInstance, context);
            }
            return uiInstance;
        }

        public override string ToString() {
            var path = new List<string>();
            var parent = Parent;
            while (parent != null) {
                path.Insert(0, parent.ClassName);
                parent = parent.Parent;
            }
            return $"{nameof(DbEntity)}[{string.Join(".", path)}]";
        }
    }

    public class DbColumn {
        public bool Virtual { get; init; }
        public string CSharpTypeName { get; init; }
        public string PropertyName { get; init; }
        public string Initializer { get; init; }
    }
}
