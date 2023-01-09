using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.AspNetMvc {
    public class MultiView {
        internal MultiView(Core.Aggregate aggregate) {
            if (aggregate.Parent != null) throw new ArgumentException($"集約ルートのみ");
            RootAggregate = aggregate;
        }

        internal Core.Aggregate RootAggregate { get; }

        internal string FileName => $"{RootAggregate.Name}__MultiView.cshtml";

        internal string TransformText(IViewModelProvider viewModelProvider) {
            var searchConditionModel = viewModelProvider.GetSearchConditionModel(RootAggregate);
            var searchResultModel = viewModelProvider.GetSearchResultModel(RootAggregate);
            var template = new MultiViewTemplate {
                ModelTypeFullname = $"{GetType().FullName}.{nameof(Model<object, object>)}<{searchConditionModel.RuntimeFullName}, {searchResultModel.RuntimeFullName}>",
                PageTitle = RootAggregate.Name,
                SearchResultClass = searchResultModel,
                ClearActionName = "Clear",
                SearchActionName = "Search",
                LinkToSingleViewActionName = "Detail",
                //SearchConditionClass = searchConditionModel,
                SearchConditionView = searchConditionModel.Render(new ViewRenderingContext("Model", nameof(Model<object, object>.SearchCondition))),
                // SearchResultView = searchResultModel.Render(new Core.ViewRenderingContext("Model", nameof(Model<object, object>.SearchResult))),
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
        internal string ClearActionName { get; set; }
        internal string SearchActionName { get; set; }
        internal string LinkToSingleViewActionName { get; set; }

        internal string SearchConditionView { get; set; }

        // internal string SearchResultView { get; set; }
        internal MvcModel SearchResultClass { get; set; }

        internal static string BoundIdPropertyPathName => $"@Model.SearchResult[i].{nameof(Runtime.SearchResultBase.__halapp__InstanceKey)}";
    }
}
