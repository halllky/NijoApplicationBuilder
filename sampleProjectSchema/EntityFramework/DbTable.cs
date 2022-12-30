using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using haldoc.Core;
using haldoc.Core.Dto;

namespace haldoc.EntityFramework {
    public class DbTable : haldoc.Core.Dto.IAutoGenerateClassMetadata {
        public Aggregate Aggregate { get; internal init; }
        public DbSchema Schema { get; internal init; }

        public string RuntimeClassFullName => Aggregate.Context.GetOutputNamespace(E_Namespace.DbEntity) + "." + RuntimeClassName;
        public string RuntimeClassName => Aggregate.Name;
        IReadOnlyList<IAutoGeneratePropertyMetadata> IAutoGenerateClassMetadata.Properties => Columns.Cast<IAutoGeneratePropertyMetadata>().ToList();

        private List<IDbColumn> _columns;
        private List<IDbColumn> _pk;
        private List<IDbColumn> _notPk;
        public IReadOnlyList<IDbColumn> Columns { get { if (_columns == null) BuildColumns(); return _columns; } }
        public IReadOnlyList<IDbColumn> PrimaryKeys { get { if (_pk == null) BuildColumns(); return _pk; } }
        public IReadOnlyList<IDbColumn> ColumnsWithoutPK { get { if (_notPk == null) BuildColumns(); return _notPk; } }
        private void BuildColumns() {
            // 集約で定義されているカラム
            var props = Aggregate.GetProperties();
            _pk = props
                .Where(prop => prop is IDbColumn && prop.IsPrimaryKey)
                .Cast<IDbColumn>()
                .ToList();
            _notPk = props
                .Where(prop => prop is IDbColumn && !prop.IsPrimaryKey)
                .Cast<IDbColumn>()
                .ToList();
            // 連番
            if (Aggregate.Parent != null
                && Aggregate.Parent.IsListProperty
                && !_pk.Any())
                _pk.Insert(0, new IndexColumn());
            // 親の主キー
            if (Aggregate.Parent != null) {
                _pk.InsertRange(0, Schema.FindTable(Aggregate.Parent.Owner).PrimaryKeys);
                // stack overflow
                //_navigationProperties = new() {
                //    new Dto.PropertyTemplate { CSharpTypeName = $"virtual {Parent.Owner.ToDbTableModel().ClassName}", PropertyName = "Parent" },
                //};
            }

            _columns = _pk.Union(_notPk).ToList();
        }


        public object CreateRuntimeInstance(Assembly runtimeAssembly) {
            var instanceType = runtimeAssembly.GetType(RuntimeClassFullName);
            var instance = Activator.CreateInstance(instanceType);
            return instance;
        }


        /// <summary>
        /// 連番
        /// </summary>
        private class IndexColumn : IDbColumn {
            public bool Virtual => false;
            public string CSharpTypeName => "int";
            public string RuntimePropertyName => "Index";
            public string Initializer => null;
        }
    }
}
