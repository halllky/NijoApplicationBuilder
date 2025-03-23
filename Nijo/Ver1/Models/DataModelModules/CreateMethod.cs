using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Common;
using Nijo.Ver1.Parts.CSharp;
using System;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// 新規登録処理
    /// </summary>
    internal class CreateMethod {
        internal CreateMethod(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;

        internal string MethodName => $"Create{_aggregate.PhysicalName}Async";
        internal string OnBeforeMethodName => $"OnBeforeCreate{_aggregate.PhysicalName}";
        internal string OnAfterMethodName => $"OnAfterCreate{_aggregate.PhysicalName}Async";

        internal string Render(CodeRenderingContext ctx) {
            var command = new SaveCommand(_aggregate);
            var dbEntity = new EFCoreEntity(_aggregate);
            var messages = new MessageContainer(_aggregate);

            return $$"""
                #region 新規登録処理
                /// <summary>
                /// {{_aggregate.DisplayName}} の新規登録を実行します。
                /// </summary>
                public virtual async Task {{MethodName}}({{command.CsClassNameCreate}} command, {{messages.InterfaceName}} messages, {{PresentationContext.CLASS_NAME}} context) {
                    // TODO ver.1
                    throw new NotImplementedException();
                }
                /// <summary>
                /// {{_aggregate.DisplayName}} の新規登録の確定前に実行される処理。
                /// 自動生成されないエラーチェックはここで実装する。
                /// エラーがあった場合、第2引数のメッセージにエラー内容を格納する。
                /// </summary>
                public virtual void {{OnBeforeMethodName}}({{command.CsClassNameCreate}} command, {{messages.InterfaceName}} messages, {{PresentationContext.CLASS_NAME}} context) {
                    // このメソッドをオーバーライドして処理を実装してください。
                }
                /// <summary>
                /// {{_aggregate.DisplayName}} の新規登録のSQL発行後、コミット前に実行される処理。
                /// このメソッドの中で例外が送出された場合、{{_aggregate.DisplayName}} の新規登録はロールバックされる。
                /// このメソッドで実装される想定としているものの例は以下。
                /// <list>
                /// <item>{{_aggregate.DisplayName}}と常に同期していなければならないリードレプリカの更新</item>
                /// <item>{{_aggregate.DisplayName}}と常に同期していなければならない外部リソースの更新やメッセージング</item>
                /// </list>
                /// </summary>
                public virtual async Task {{OnAfterMethodName}}({{dbEntity.CsClassName}} newValue, {{messages.InterfaceName}} messages, {{PresentationContext.CLASS_NAME}} context) {
                    // このメソッドをオーバーライドして処理を実装してください。
                }
                #endregion 新規登録処理
                """;
        }
    }
}
