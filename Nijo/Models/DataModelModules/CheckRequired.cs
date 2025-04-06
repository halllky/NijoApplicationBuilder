using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.DataModelModules;
using Nijo.Parts.Common;
using System;

namespace Nijo.Models.DataModelModules {
    /// <summary>
    /// 必須入力チェック
    /// </summary>
    internal static class CheckRequired {

        internal const string METHOD_NAME = "CheckRequired";

        internal static string Render(RootAggregate rootAggregate, CodeRenderingContext ctx) {
            var efCoreEntity = new EFCoreEntity(rootAggregate);
            var messages = new SaveCommandMessageContainer(rootAggregate);

            return $$"""
                /// <summary>
                /// 必須チェック処理。空の項目があった場合はその旨が第2引数のオブジェクト内に追記されます。
                /// </summary>
                protected virtual void {{METHOD_NAME}}({{efCoreEntity.CsClassName}} dbEntity, {{messages.InterfaceName}} messages) {
                    // TODO ver.1
                }
                """;
        }
    }
}
