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

        internal string InterfaceName => $"I{_aggregate.PhysicalName}SaveCommandMessages";
        internal string ClassName => $"{_aggregate.PhysicalName}SaveCommandMessagesImpl";

        internal string RenderInterfaceDeclaring(CodeRenderingContext ctx) {
            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} の保存時に発生したメッセージの入れ物
                /// </summary>
                public interface {{InterfaceName}} {
                    // TODO ver.1
                }
                """;
        }

        internal string RenderClassDeclaring(CodeRenderingContext ctx) {
            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} の保存時に発生したメッセージの入れ物
                /// </summary>
                public interface {{ClassName}} : {{InterfaceName}} {
                    // TODO ver.1
                }
                """;
        }
    }
}
