using System;
using System.Collections.Generic;
using System.Linq;

namespace HalApplicationBuilder.AspNetMvc {
    public class MvcModels {
        internal Core.ApplicationSchema Schema { get; init; }

        internal string TransformText() {
            var rootAggregates = Schema.RootAggregates();
            var allAggregates = Schema.AllAggregates();
            var template = new MvcModelsTemplate {
                Namespace = Schema.Config.MvcModelNamespace,
                SearchConditionClasses = allAggregates.Select(a => Schema.GetSearchConditionModel(a)),
                SearchResultClasses = rootAggregates.Select(a => Schema.GetSearchResultModel(a)),
                InstanceClasses = allAggregates.Select(a => Schema.GetInstanceModel(a)),
            };
            return template.TransformText();
        }
    }

    partial class MvcModelsTemplate {
        internal string Namespace { get; set; }
        internal IEnumerable<MvcModel> SearchConditionClasses { get; set; }
        internal IEnumerable<MvcModel> SearchResultClasses { get; set; }
        internal IEnumerable<MvcModel> InstanceClasses { get; set; }
    }
}
