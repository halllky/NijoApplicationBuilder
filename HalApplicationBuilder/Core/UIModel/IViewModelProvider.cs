using System;

namespace HalApplicationBuilder.Core.UIModel {
    public interface IViewModelProvider {
        MvcModel GetInstanceModel(Aggregate aggregate);
        MvcModel GetSearchConditionModel(Aggregate aggregate);
        MvcModel GetSearchResultModel(Aggregate aggregate);
    }
}
