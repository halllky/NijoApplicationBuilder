using System;
using System.Collections.Generic;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.UIModel;

namespace HalApplicationBuilder.EntityFramework {
    internal class OnModelCreatingRenderer : IAggregateHandler {

        internal static string Render(IApplicationSchema appSchema, IDbSchema dbSchema, Config Config) {
            var template = new OnModelCreatingTemplate {
                DbContextName = Config.DbContextName,
                DbContextNamespace = Config.DbContextNamespace,
            };
            var renderer = new OnModelCreatingRenderer(template);

            foreach (var aggregate in appSchema.AllAggregates()) {
                renderer.HandleDbEntity(dbSchema.GetDbEntity(aggregate));
            }

            return template.TransformText();
        }

        private OnModelCreatingRenderer(OnModelCreatingTemplate template) {
            _template = template;
        }
        private readonly OnModelCreatingTemplate _template;

        public void HandleDbEntity(DbEntity dbEntity) {
            _template.EntityClasses.Add(dbEntity);
        }

        public void HandleAggregate(Aggregate aggregate) => throw new InvalidOperationException();
        public void HandleCreateCommand(InstanceModelClass instanceModel) => throw new InvalidOperationException();
        public void HandleInstance(InstanceModelClass instanceModel) => throw new InvalidOperationException();
        public void HandleSearchCondition(SearchConditionClass searchCondition) => throw new InvalidOperationException();
        public void HandleSearchResult(SearchResultClass searchResult) => throw new InvalidOperationException();
    }

    partial class OnModelCreatingTemplate {
        internal string DbContextName { get; set; }
        internal string DbContextNamespace { get; set; }
        public List<DbEntity> EntityClasses { get; } = new();
    }
}
