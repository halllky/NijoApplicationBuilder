using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// 集約のキー部分のみの情報
    /// </summary>
    internal class KeyClass {
        internal KeyClass(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;

        internal string ClassName => $"{_aggregate.PhysicalName}Key";

        internal string RenderClassDeclaring(CodeRenderingContext ctx) {
            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} のキー
                /// </summary>
                public sealed class {{ClassName}} {
                    // TODO ver.1
                }
                """;
        }
    }
}
