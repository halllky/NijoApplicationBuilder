using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// データモデルの登録更新処理の引数
    /// </summary>
    internal class SaveCommand {

        internal SaveCommand(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;

        internal string CsClassNameCreate => $"{_aggregate.PhysicalName}CreateCommand";
        internal string CsClassNameUpdate => $"{_aggregate.PhysicalName}UpdateCommand";
        internal string CsClassNameDelete => $"{_aggregate.PhysicalName}DeleteCommand";

        internal string RenderCreateCommandDeclaring(CodeRenderingContext ctx) {
            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} の新規登録コマンド引数
                /// </summary>
                public partial class {{CsClassNameCreate}} {
                    // TODO ver.1
                }
                """;
        }

        internal string RenderUpdateCommandDeclaring(CodeRenderingContext ctx) {
            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} の更新コマンド引数
                /// </summary>
                public partial class {{CsClassNameUpdate}} {
                    // TODO ver.1
                }
                """;
        }

        internal string RenderDeleteCommandDeclaring(CodeRenderingContext ctx) {
            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} の物理削除コマンド引数
                /// </summary>
                public partial class {{CsClassNameDelete}} {
                    // TODO ver.1
                }
                """;
        }
    }
}
