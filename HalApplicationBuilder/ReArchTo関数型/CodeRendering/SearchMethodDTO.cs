using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.ReArchTo関数型.CodeRendering
{
    internal class SearchMethodDTO
    {
        internal required string ParamVarName { get; init; }
        internal required string QueryVarName { get; init; }

        internal required string SearchResultClassName { get; init; }
        internal required string SearchConditionClassName { get; init; }
        internal required string MethodName { get; init; }
        internal required string DbSetName { get; init; }

        internal List<string> SelectClause { get; } = new();
        internal List<string> WhereClause { get; } = new();
    }
}

