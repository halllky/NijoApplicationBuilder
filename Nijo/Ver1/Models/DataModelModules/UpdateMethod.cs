using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Core;
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

        internal string MethodName => $"Update{_aggregate.PhysicalName}";

        internal string Render(CodeRenderingContext ctx) {
            var command = new SaveCommand(_aggregate);
            var messages = new SaveCommandMessage(_aggregate);

            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} の更新を実行します。
                /// </summary>
                public virtual void {{MethodName}}({{command.CsClassNameUpdate}} command, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE_NAME}} context) {
                    // TODO ver.1
                    throw new NotImplementedException();
                }
                """;
        }
    }
}
