using System;
using System.Collections.Generic;
using System.Linq;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.UIModel;

namespace HalApplicationBuilder.EntityFramework {
    internal class DbSetRenderer {

        internal static string Render(IApplicationSchema appSchema, IDbSchema dbSchema, Config Config) {
            var template = new DbSetTemplate {
                DbContextName = Config.DbContextName,
                DbContextNamespace = Config.DbContextNamespace,
            };

            foreach (var aggregate in appSchema.AllAggregates()) {
                template.EntityClasses.Add(dbSchema.GetDbEntity(aggregate));
            }

            return template.TransformText();
        }
    }

    partial class DbSetTemplate {
        internal string DbContextName { get; set; }
        internal string DbContextNamespace { get; set; }
        public List<DbEntity> EntityClasses { get; } = new();
    }
}
