using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.DataModelModules;
using Nijo.Parts.Common;
using System;

namespace Nijo.Models.DataModelModules {
    /// <summary>
    /// 動的列挙型
    /// </summary>
    internal static class DynamicEnum {

        internal const string METHOD_NAME = "CheckDynamicEnumType";

        internal static string RenderAppSrvCheckMethod(RootAggregate rootAggregate, CodeRenderingContext ctx) {
            var efCoreEntity = new EFCoreEntity(rootAggregate);
            var messages = new SaveCommandMessageContainer(rootAggregate);

            return $$"""
                /// <summary>
                /// 異なる種類の区分値が登録されないかのチェック処理。違反する項目があった場合はその旨が第2引数のオブジェクト内に追記されます。
                /// </summary>
                protected virtual void {{METHOD_NAME}}({{efCoreEntity.CsClassName}} dbEntity, {{messages.InterfaceName}} messages) {
                    // TODO ver.1
                }
                """;
        }
    }
}
