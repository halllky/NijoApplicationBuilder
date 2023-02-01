using System;
using System.Collections.Generic;
using System.Linq;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.UIModel;

namespace HalApplicationBuilder.EntityFramework {
    public class AutoCompleteSourceMethodRenderer {

        public static string Render(IApplicationSchema applicationSchema, IDbSchema dbSchema, IViewModelProvider viewModelProvider, Config config) {
            var rootAggregates = applicationSchema.RootAggregates();
            var loadAutoCompleteData = rootAggregates
                .Select(a => new LoadAutoComplele(dbSchema.GetDbEntity(a), config));
            var template = new AutoCompleteSourceMethodTemplate {
                DbContextNamespace = config.DbContextNamespace,
                DbContextName = config.DbContextName,
                LoadAutoCompleteMethod = loadAutoCompleteData,
            };
            return template.TransformText();
        }

        public class LoadAutoComplele {
            public LoadAutoComplele(DbEntity dbEntity, Config config) {
                _dbEntity = dbEntity;
                _config = config;
            }
            private readonly DbEntity _dbEntity;
            private readonly Config _config;

            public string DbSetName => _dbEntity.DbSetName;
            public string EntityClassName => $"{_config.EntityNamespace}.{_dbEntity.ClassName}";
            public string MethodName => $"LoadAutoCompleteSource_{_dbEntity.ClassName}";
            public string NameColumnName => null; // TODO
        }
    }

    partial class AutoCompleteSourceMethodTemplate {
        public object DbContextNamespace { get; set; }
        public object DbContextName { get; set; }
        public IEnumerable<AutoCompleteSourceMethodRenderer.LoadAutoComplele> LoadAutoCompleteMethod { get; set; }
    }
}
