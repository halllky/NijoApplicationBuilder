using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using System;

namespace Nijo.Models.DataModelModules {
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
