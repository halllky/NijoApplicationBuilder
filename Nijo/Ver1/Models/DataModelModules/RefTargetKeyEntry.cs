using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// ほかの集約から参照されるときのキー
    /// </summary>
    internal class RefTargetKeyEntry {
        internal RefTargetKeyEntry(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;

        internal string RenderClassDeclaring(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
    }
}
