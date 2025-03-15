using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// 動的列挙型
    /// </summary>
    internal static class DynamicEnum {

        internal const string METHOD_NAME = "CheckDynamicEnumType";

        internal static string RenderAppSrvCheckMethod(RootAggregate rootAggregate, CodeRenderingContext ctx) {
            var efCoreEntity = new EFCoreEntity(rootAggregate);
            var messages = new SaveCommandMessage(rootAggregate);

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
