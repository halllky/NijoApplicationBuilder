using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;

namespace Nijo.Ver1.Models.CommandModelModules {
    /// <summary>
    /// クライアント側新規オブジェクト作成関数
    /// </summary>
    internal class ObjectCreation {
        internal ObjectCreation(RootAggregate aggregate) {
            _aggregate = aggregate;
        }
        private readonly RootAggregate _aggregate;

        internal string RenderTypeScript(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
    }
} 