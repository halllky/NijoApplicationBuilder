using System;
using System.Collections.Generic;

namespace haldoc.SqlQuery {
    public interface ISearchConditionHandler {

        object Deserialize(string serialized);
        string Serialize(object searchCondition);

        IEnumerable<string> GenerateSearchConditionLayout(string modelName);

        void AppendWhereClause(string tableAlias, object searchCondition, QueryBuilderContext context);
    }
}
