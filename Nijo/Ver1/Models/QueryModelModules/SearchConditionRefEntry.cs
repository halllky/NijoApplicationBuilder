using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;

namespace Nijo.Ver1.Models.QueryModelModules {
    internal class SearchConditionRefEntry {
        public SearchConditionRefEntry(AggregateBase aggregate) {
            _aggregate = aggregate;
        }

        private readonly AggregateBase _aggregate;

        public object TsFilterTypeName { get; internal set; }

        internal string RenderCsClass(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }

        internal string RenderTypeScriptObjectCreationFunction(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
    }
}
