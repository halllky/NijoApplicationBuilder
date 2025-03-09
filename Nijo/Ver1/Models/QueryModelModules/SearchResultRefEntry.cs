using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;

namespace Nijo.Ver1.Models.QueryModelModules {
    internal class SearchResultRefEntry {
        private RootAggregate rootAggregate;

        public SearchResultRefEntry(RootAggregate rootAggregate) {
            this.rootAggregate = rootAggregate;
        }

        internal string RenderCsClass(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
    }
}