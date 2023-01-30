using System;
namespace HalApplicationBuilder.Core {
    public interface IApplicationModule {
        void HandleAggregate(Aggregate aggregate);
        void HandleSearchCondition(UIModel.SearchConditionClass searchCondition);
        void HandleSearchResult(UIModel.SearchResultClass searchResult);
        void HandleCreateCommand(UIModel.InstanceModelClass instanceModel);
        void HandleInstance(UIModel.InstanceModelClass instanceModel);
        void HandleDbEntity(DBModel.DbEntity dbEntity);
    }
}
