using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;

namespace Nijo.Ver1.Models.CommandModelModules {
    /// <summary>
    /// コマンド処理
    /// </summary>
    internal class CommandProcessing {
        internal CommandProcessing(RootAggregate aggregate) {
            _aggregate = aggregate;
        }
        private readonly RootAggregate _aggregate;

        internal string RenderReactHook(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }

        internal string RenderWebEndpoint(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }

        internal string RenderAbstractMethod(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
    }
} 