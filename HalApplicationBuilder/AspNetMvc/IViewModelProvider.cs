using System;
using HalApplicationBuilder.Core;

namespace HalApplicationBuilder.AspNetMvc {
    public interface IViewModelProvider {
        MvcModel GetInstanceModel(Aggregate aggregate);
        MvcModel GetSearchConditionModel(Aggregate aggregate);
        MvcModel GetSearchResultModel(Aggregate aggregate);
    }
}
