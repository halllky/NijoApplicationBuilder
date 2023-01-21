﻿using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Core.Runtime {
    public interface IInstanceConverter {
        // InstanceModel
        void MapUIToDB(object uiInstance, object dbInstance, RuntimeContext context);
        void MapDBToUI(object dbInstance, object uiInstance, RuntimeContext context);

        // SearchResult
        void BuildSelectStatement(Core.DBModel.SelectStatement selectStatement, object searchCondition, RuntimeContext context, string selectClausePrefix);
        void MapSearchResultToUI(System.Data.Common.DbDataReader reader, object searchResult, RuntimeContext context, string selectClausePrefix);

        // AutoComplete
        void BuildAutoCompleteSelectStatement(Core.DBModel.SelectStatement selectStatement, string inputText, RuntimeContext context, string selectClausePrefix);
    }
}