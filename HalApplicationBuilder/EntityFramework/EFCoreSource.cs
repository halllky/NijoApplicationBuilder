using System;
using System.Collections.Generic;
using System.Linq;
using HalApplicationBuilder.Core;

namespace HalApplicationBuilder.EntityFramework {
    internal class EFCoreSource {
        internal Core.ApplicationSchema Schema { get; init; }

        internal string TransformText() {
            var aggregates = Schema.AllAggregates();
            var template = new EFCoreSourceTemplate {
                DbContextName = Schema.Config.DbContextName,
                DbContextNamespace = Schema.Config.DbContextNamespace,
                EntityNamespace = Schema.Config.EntityNamespace,
                EntityClasses = aggregates.Select(a => Schema.GetDbEntity(a)),
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
