using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core.Runtime;

namespace HalApplicationBuilder.Core.DBModel {
    public class DbEntity {
        internal DbEntity(Aggregate source, DbEntity parent, Config config) {
            Source = source;
            Parent = parent;
            _config = config;
        }

        public Aggregate Source { get; }

        public DbEntity Parent { get; }
        public IEnumerable<DbEntity> GetAncestors() {
            var parent = Parent;
            while (parent != null) {
                yield return parent;
                parent = parent.Parent;
            }
        }

        internal HashSet<DbEntity> children = new();
        public IReadOnlySet<DbEntity> Children => children;
        public IEnumerable<DbEntity> GetDescendants() {
            foreach (var child in Children) {
                yield return child;
                foreach (var descendant in child.GetDescendants()) {
                    yield return descendant;
                }
            }
        }

        private readonly Config _config;

        public string ClassName
            => Source.UnderlyingType.GetCustomAttribute<TableAttribute>()?.Name
            ?? Source.UnderlyingType.Name;
        public string RuntimeFullName
            => _config.EntityNamespace + "." + ClassName;
        public string DbSetName => ClassName;

        private List<DbColumn> _pk;
        private List<DbColumn> _notPk;
        private List<DbColumn> _navigation;
        public IReadOnlyList<DbColumn> PKColumns {
            get {
                if (_pk == null) BuildValueColumns();
                return _pk;
            }
        }
        public IReadOnlyList<DbColumn> NotPKColumns {
            get {
                if (_notPk == null) BuildValueColumns();
                return _notPk;
            }
        }
        public IReadOnlyList<DbColumn> NavigationProperties {
            get {
                if (_navigation == null) BuildValueColumns();
                return _navigation;
            }
        }
        private void BuildValueColumns() {
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
                _pk.Insert(0, new IndexColumn {
                    Virtual = false,
                    CSharpTypeName = "string",
                    PropertyName = $"{Source.Name}_連番",
                    Initializer = "Guid.NewGuid().ToString()",
                });
            }
            // 親
            if (Parent != null) {
                // 親の主キー
                _pk.InsertRange(0, Parent.PKColumns);
                // ナビゲーションプロパティ
                _notPk.Add(new DbColumn {
                    Virtual = true,
                    CSharpTypeName = Parent.RuntimeFullName,
                    PropertyName = Parent.ClassName,
                });
            }
            // TODO リファクタリング
            _navigation = _pk.Concat(_notPk)
                .Where(col => col.Virtual)
                .ToList();
            _pk.RemoveAll(col => col.Virtual);
            _notPk.RemoveAll(col => col.Virtual);
            // TODO リファクタリング
            foreach (var col in _pk) col.Owner = this;
            foreach (var col in _notPk) col.Owner = this;
            foreach (var col in _navigation) col.Owner = this;
        }

        internal IEnumerable<DbColumn> GetAllDbProperties() {
            foreach (var col in PKColumns) yield return col;
            foreach (var col in NotPKColumns) yield return col;
            foreach (var col in NavigationProperties) yield return col;
        }
        internal IEnumerable<string> GetManyToOne() {
            if (Parent != null) {
                // TODO
                var parent = Parent.ClassName;
                var children = Parent.NavigationProperties
                    .SingleOrDefault(col => col.CSharpTypeName == $"ICollection<{RuntimeFullName}>")?
                    .PropertyName;
                if (children != null)
                    yield return $"entity.HasOne(e => e.{parent}).WithMany(e => e.{children});";
            }
        }
        internal IEnumerable<string> GetOneToOne() {
            if (Parent != null) {
                // TODO
                var parent = Parent.ClassName;
                var child = Parent.NavigationProperties
                    .SingleOrDefault(col => col.CSharpTypeName == RuntimeFullName)?
                    .PropertyName;
                if (child != null)
                    yield return $"entity.HasOne(e => e.{parent}).WithOne(e => e.{child});";
            }
        }

        internal object ConvertUiInstanceToDbInstance(object uiInstance, RuntimeContext context) {
            var dbInstance = context.RuntimeAssembly.CreateInstance(RuntimeFullName);

            MapUiInstanceToDbInsntace(uiInstance, dbInstance, context);

            return dbInstance;
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
        internal void MapUiInstanceToDbInsntace(object uiInstance, object dbInstance, RuntimeContext context) {
            foreach (var member in Source.Members) {
                if (member is not IInstanceConverter converter) continue;
                converter.MapUIToDB(uiInstance, dbInstance, context);
            }
        }

        public override string ToString() {
            var path = new List<string>();
            var dbEntity = this;
            while (dbEntity != null) {
                path.Insert(0, dbEntity.ClassName);
                dbEntity = dbEntity.Parent;
            }
            return $"{nameof(DbEntity)}[{string.Join(".", path)}]";
        }
    }

    public class DbColumn {
        public DbEntity Owner { get; internal set; }

        public bool Virtual { get; init; }
        public string CSharpTypeName { get; init; }
        public string PropertyName { get; init; }
        public string Initializer { get; init; }
    }
    /// <summary>連番</summary>
    public class IndexColumn : DbColumn{

    }
}
