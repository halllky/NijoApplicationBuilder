using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// 更新処理
    /// </summary>
    internal class UpdateMethod {
        internal UpdateMethod(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;

        internal string Render(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
    }
}
