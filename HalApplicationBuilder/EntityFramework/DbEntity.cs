using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core;

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
    }

    public class DbColumn {
        public bool Virtual { get; init; }
        public string CSharpTypeName { get; init; }
        public string PropertyName { get; init; }
        public string Initializer { get; init; }
    }
}
