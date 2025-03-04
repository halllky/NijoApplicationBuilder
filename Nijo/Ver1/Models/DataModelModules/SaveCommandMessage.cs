using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// SaveCommandメッセージ
    /// </summary>
    internal class SaveCommandMessage {
        internal SaveCommandMessage(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;

        internal string RenderInterfaceDeclaring(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }

        internal string RenderClassDeclaring(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
    }
}
