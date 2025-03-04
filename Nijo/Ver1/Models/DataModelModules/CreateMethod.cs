using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// 新規登録処理
    /// </summary>
    internal class CreateMethod {
        internal CreateMethod(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;

        internal string Render(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
    }
}
