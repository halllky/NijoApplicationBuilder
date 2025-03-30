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

        internal const string TO_DBENTITY = "ToDbEntity";
        internal const string FROM_DBENTITY = "FromDbEntity";

        internal string RenderCreateCommandDeclaring(CodeRenderingContext ctx) {
            var efCoreEntity = new EFCoreEntity(_aggregate);

            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} の新規登録コマンド引数
                /// </summary>
                public partial class {{CsClassNameCreate}} {
                    // TODO ver.1
                {{If(_aggregate is RootAggregate, () => $$"""

                    /// <summary>
                    /// このインスタンスを Entity Framework Core のエンティティに変換します。
                    /// </summary>
                    public {{efCoreEntity.CsClassName}} {{TO_DBENTITY}}() {
                        throw new NotImplementedException(); // TODO ver.1
                    }
                """)}}
                }
                """;
        }

        internal string RenderUpdateCommandDeclaring(CodeRenderingContext ctx) {
            var efCoreEntity = new EFCoreEntity(_aggregate);

            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} の更新コマンド引数
                /// </summary>
                public partial class {{CsClassNameUpdate}} {
                    // TODO ver.1
                {{If(_aggregate is RootAggregate, () => $$"""

                    /// <summary>
                    /// このインスタンスを Entity Framework Core のエンティティに変換します。
                    /// </summary>
                    public {{efCoreEntity.CsClassName}} {{TO_DBENTITY}}() {
                        throw new NotImplementedException(); // TODO ver.1
                    }

                    /// <summary>
                    /// Entity Framework Core のエンティティからこのクラスのインスタンスを作成します。
                    /// </summary>
                    public static {{CsClassNameUpdate}} {{FROM_DBENTITY}}({{efCoreEntity.CsClassName}} dbEntity) {
                        throw new NotImplementedException(); // TODO ver.1
                    }
                """)}}
                }
                """;
        }

        internal string RenderDeleteCommandDeclaring(CodeRenderingContext ctx) {
            var efCoreEntity = new EFCoreEntity(_aggregate);

            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} の物理削除コマンド引数。キーのみを持つ。
                /// </summary>
                public partial class {{CsClassNameDelete}} {
                    // TODO ver.1
                }
                """;
        }
    }
}
