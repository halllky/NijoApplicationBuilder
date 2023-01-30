using System;

namespace HalApplicationBuilder.Core.UIModel {
    public interface IViewModelProvider {
        MvcModel GetInstanceModel(Aggregate aggregate);
        SearchConditionClass GetSearchConditionModel(Aggregate aggregate);
        SearchResultClass GetSearchResultModel(Aggregate aggregate);
    }
}
