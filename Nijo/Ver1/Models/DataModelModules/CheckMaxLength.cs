using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Common;
using System;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// 文字列最大長チェック
    /// </summary>
    internal static class CheckMaxLength {

        internal const string METHOD_NAME = "CheckMaxLength";

        internal static string Render(RootAggregate rootAggregate, CodeRenderingContext ctx) {
            var efCoreEntity = new EFCoreEntity(rootAggregate);
            var messages = new SaveCommandMessageContainer(rootAggregate);

            return $$"""
                /// <summary>
                /// 文字列最大長チェック処理。違反する項目があった場合はその旨が第2引数のオブジェクト内に追記されます。
                /// </summary>
                protected virtual void {{METHOD_NAME}}({{efCoreEntity.CsClassName}} dbEntity, {{messages.InterfaceName}} messages) {
                    // TODO ver.1
                }
                """;
        }
    }
}
