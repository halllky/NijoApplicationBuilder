using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.AspNetMvc {
    public class MultiView {
        internal Core.Aggregate RootAggregate { get; init; }

        internal string FileName => $"{RootAggregate.Name}__MultiView.cshtml";

        internal string TransformText() {
            var template = new MultiViewTemplate {
                ModelTypeFullname = $"{GetType().FullName}.{nameof(Model<object, object>)}<{RootAggregate.SearchConditionModel.RuntimeFullName}, {RootAggregate.SearchResultModel.RuntimeFullName}>",
                PageTitle = RootAggregate.Name,
                SearchConditionClass = RootAggregate.SearchConditionModel,
                SearchResultClass = RootAggregate.SearchResultModel,
                ClearActionName = "Clear",
                SearchActionName = "Search",
                LinkToSingleViewActionName = "Detail",
                SearchConditionView = RootAggregate.SearchConditionModel.Render(new Core.ViewRenderingContext("Model", nameof(Model<object, object>.SearchCondition))),
                // SearchResultView = RootAggregate.SearchResultModel.Render(new Core.ViewRenderingContext("Model", nameof(Model<object, object>.SearchResult))),
            };
            return template.TransformText();
        }

        public class Model<TSearchCondition, TSearchResult> {
            public TSearchCondition SearchCondition { get; set; }
            public List<TSearchResult> SearchResult { get; set; } = new();
        }
    }

    partial class MultiViewTemplate {
        internal string ModelTypeFullname { get; set; }
        internal string PageTitle { get; set; }
        internal Core.UIClass SearchConditionClass { get; set; }
        internal Core.UIClass SearchResultClass { get; set; }
        internal string ClearActionName { get; set; }
        internal string SearchActionName { get; set; }
        internal string LinkToSingleViewActionName { get; set; }
        internal string SearchConditionView { get; set; }
        // internal string SearchResultView { get; set; }
    }
}
