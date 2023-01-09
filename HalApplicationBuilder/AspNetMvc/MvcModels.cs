using System;
using System.Collections.Generic;
using System.Linq;

namespace HalApplicationBuilder.AspNetMvc {
    public class MvcModels {

        internal string TransformText(Core.IApplicationSchema schema, IViewModelProvider viewModelProvider, Core.Config config) {
            var rootAggregates = schema.RootAggregates();
            var allAggregates = schema.AllAggregates();
            var template = new MvcModelsTemplate {
                Namespace = config.MvcModelNamespace,
                SearchConditionClasses = allAggregates.Select(a => viewModelProvider.GetSearchConditionModel(a)),
                SearchResultClasses = rootAggregates.Select(a => viewModelProvider.GetSearchResultModel(a)),
                InstanceClasses = allAggregates.Select(a => viewModelProvider.GetInstanceModel(a)),
            };
            return template.TransformText();
        }
    }

    partial class MvcModelsTemplate {
        internal string Namespace { get; set; }
        internal IEnumerable<MvcModel> SearchConditionClasses { get; set; }
        internal IEnumerable<MvcModel> SearchResultClasses { get; set; }
        internal IEnumerable<MvcModel> InstanceClasses { get; set; }

        internal static string UIInstanceBase => typeof(Runtime.UIInstanceBase).FullName;
        internal static string SearchResultBase => typeof(Runtime.SearchResultBase).FullName;
    }
}
