using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Common;
using Nijo.Ver1.Parts.CSharp;
using System;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// 削除処理
    /// </summary>
    internal class DeleteMethod {
        internal DeleteMethod(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
        }
        private readonly RootAggregate _rootAggregate;

        internal string MethodName => $"Delete{_rootAggregate.PhysicalName}";
        internal string OnBeforeMethodName => $"OnBeforeDelete{_rootAggregate.PhysicalName}";
        internal string OnAfterMethodName => $"OnAfterDelete{_rootAggregate.PhysicalName}Async";

        internal string Render(CodeRenderingContext ctx) {
            var command = new SaveCommand(_rootAggregate);
            var dbEntity = new EFCoreEntity(_rootAggregate);
            var messages = new SaveCommandMessageContainer(_rootAggregate);

            return $$"""
                #region 物理削除処理
                /// <summary>
                /// {{_rootAggregate.DisplayName}} の物理削除を実行します。
                /// </summary>
                public virtual void {{MethodName}}({{command.CsClassNameDelete}} command, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE}} context) {
                    // TODO ver.1
                    throw new NotImplementedException();
                }
                /// <summary>
                /// {{_rootAggregate.DisplayName}} の物理削除の確定前に実行される処理。
                /// 自動生成されないエラーチェックはここで実装する。
                /// エラーがあった場合、第3引数のメッセージにエラー内容を格納する。
                /// </summary>
                public virtual void {{OnBeforeMethodName}}({{command.CsClassNameDelete}} command, {{dbEntity.CsClassName}} oldValue, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE}} context) {
                    // このメソッドをオーバーライドして処理を実装してください。
                }
                /// <summary>
                /// {{_rootAggregate.DisplayName}} の物理削除のSQL発行後、コミット前に実行される処理。
                /// このメソッドの中で例外が送出された場合、{{_rootAggregate.DisplayName}} の物理削除はロールバックされる。
                /// このメソッドで実装される想定としているものの例は以下。
                /// <list>
                /// <item>{{_rootAggregate.DisplayName}}と常に同期していなければならないリードレプリカの更新</item>
                /// <item>{{_rootAggregate.DisplayName}}と常に同期していなければならない外部リソースの更新やメッセージング</item>
                /// </list>
                /// </summary>
                public virtual async Task {{OnAfterMethodName}}({{dbEntity.CsClassName}} newValue, {{dbEntity.CsClassName}} oldValue, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE}} context) {
                    // このメソッドをオーバーライドして処理を実装してください。
                }
                #endregion 物理削除処理
                """;
        }
    }
}
