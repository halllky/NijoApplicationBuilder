using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.CSharp;
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
        internal string OnBeforeMethodName => $"OnBeforeDelete{_aggregate.PhysicalName}";
        internal string OnAfterMethodName => $"OnAfterDelete{_aggregate.PhysicalName}Async";

        internal string Render(CodeRenderingContext ctx) {
            var command = new SaveCommand(_aggregate);
            var dbEntity = new EFCoreEntity(_aggregate);
            var messages = new SaveCommandMessage(_aggregate);

            return $$"""
                #region 物理削除処理
                /// <summary>
                /// {{_aggregate.DisplayName}} の物理削除を実行します。
                /// </summary>
                public virtual void {{MethodName}}({{command.CsClassNameDelete}} command, {{messages.InterfaceName}} messages, {{PresentationContext.CLASS_NAME}} context) {
                    // TODO ver.1
                    throw new NotImplementedException();
                }
                /// <summary>
                /// {{_aggregate.DisplayName}} の物理削除の確定前に実行される処理。
                /// 自動生成されないエラーチェックはここで実装する。
                /// エラーがあった場合、第3引数のメッセージにエラー内容を格納する。
                /// </summary>
                public virtual void {{OnBeforeMethodName}}({{command.CsClassNameDelete}} command, {{dbEntity.CsClassName}} oldValue, {{messages.InterfaceName}} messages, {{PresentationContext.CLASS_NAME}} context) {
                    // このメソッドをオーバーライドして処理を実装してください。
                }
                /// <summary>
                /// {{_aggregate.DisplayName}} の物理削除のSQL発行後、コミット前に実行される処理。
                /// このメソッドの中で例外が送出された場合、{{_aggregate.DisplayName}} の物理削除はロールバックされる。
                /// このメソッドで実装される想定としているものの例は以下。
                /// <list>
                /// <item>{{_aggregate.DisplayName}}と常に同期していなければならないリードレプリカの更新</item>
                /// <item>{{_aggregate.DisplayName}}と常に同期していなければならない外部リソースの更新やメッセージング</item>
                /// </list>
                /// </summary>
                public virtual async Task {{OnAfterMethodName}}({{dbEntity.CsClassName}} newValue, {{dbEntity.CsClassName}} oldValue, {{messages.InterfaceName}} messages, {{PresentationContext.CLASS_NAME}} context) {
                    // このメソッドをオーバーライドして処理を実装してください。
                }
                #endregion 物理削除処理
                """;
        }
    }
}
