using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;

namespace Nijo.Ver1.Models.DataModelModules {
    internal class BatchUpdate {
        public BatchUpdate(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
        }

        private readonly RootAggregate _rootAggregate;

        internal string RenderAppSrvMethod(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }

        internal string RenderControllerAction(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
    }
}
