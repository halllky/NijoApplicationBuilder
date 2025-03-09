using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;

namespace Nijo.Ver1.Models.QueryModelModules {
    internal class SearchProcessingRefs {
        private RootAggregate rootAggregate;

        public SearchProcessingRefs(RootAggregate rootAggregate) {
            this.rootAggregate = rootAggregate;
        }

        internal string RenderAppSrvMethod(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }

        internal string RenderAspNetCoreControllerAction(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }

        internal string RenderReactHook(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
    }
}
