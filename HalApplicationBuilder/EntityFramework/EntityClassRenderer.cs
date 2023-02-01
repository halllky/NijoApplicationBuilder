using System;
using System.Collections.Generic;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.UIModel;

namespace HalApplicationBuilder.EntityFramework {
    internal class EntityClassRenderer : IAggregateHandler {

        internal static string Render(IApplicationSchema applicationSchema, IDbSchema dbSchema, Config config) {
            var template = new EntityClassTemplate {
                EntityNamespace = config.EntityNamespace,
            };
            foreach (var aggregate in applicationSchema.AllAggregates()) {
                var dbEntity = dbSchema.GetDbEntity(aggregate);
                new EntityClassRenderer(template).HandleDbEntity(dbEntity);
            }
            return template.TransformText();
        }

        private EntityClassRenderer(EntityClassTemplate template) {
            _template = template;
        }

        private readonly EntityClassTemplate _template;

        public void HandleDbEntity(DbEntity dbEntity) {
            _template.EntityClasses.Add(dbEntity);
        }

        public void HandleAggregate(Aggregate aggregate) { }
        public void HandleCreateCommand(InstanceModelClass instanceModel) { }
        public void HandleInstance(InstanceModelClass instanceModel) { }
        public void HandleSearchCondition(SearchConditionClass searchCondition) { }
        public void HandleSearchResult(SearchResultClass searchResult) { }
    }

    partial class EntityClassTemplate {
        internal string EntityNamespace { get; set; }
        internal List<DbEntity> EntityClasses { get; } = new();
    }
}
