using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Core;
using System;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// 削除処理
    /// </summary>
    internal class DeleteMethod {
        internal DeleteMethod(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;

        internal string MethodName => $"Delete{_aggregate.PhysicalName}";

        internal string Render(CodeRenderingContext ctx) {
            var command = new SaveCommand(_aggregate);
            var messages = new SaveCommandMessage(_aggregate);

            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} の物理削除を実行します。
                /// </summary>
                public virtual void {{MethodName}}({{command.CsClassNameDelete}} command, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE_NAME}} context) {
                    // TODO ver.1
                    throw new NotImplementedException();
                }
                """;
        }
    }
}
