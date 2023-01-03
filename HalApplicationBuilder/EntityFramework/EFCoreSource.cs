using System;
using System.Collections.Generic;
using System.Linq;
using HalApplicationBuilder.Core;

namespace HalApplicationBuilder.EntityFramework {
    internal class EFCoreSource {

        internal string TransformText(IApplicationSchema appSchema, IDbSchema dbSchema, Config Config) {
            var aggregates = appSchema.AllAggregates();
            var template = new EFCoreSourceTemplate {
                DbContextName = Config.DbContextName,
                DbContextNamespace = Config.DbContextNamespace,
                EntityNamespace = Config.EntityNamespace,
                EntityClasses = aggregates.Select(a => dbSchema.GetDbEntity(a)),
            };
            return template.TransformText();
        }
    }

    partial class EFCoreSourceTemplate {
        internal string DbContextName { get; set; }
        internal string DbContextNamespace { get; set; }
        internal string EntityNamespace { get; set; }
        internal IEnumerable<EntityFramework.DbEntity> EntityClasses { get; set; }
    }
}
