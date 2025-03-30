using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Common;
using Nijo.Ver1.Parts.CSharp;
using Nijo.Ver1.ValueMemberTypes;
using System;
using System.Linq;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// 新規登録処理
    /// </summary>
    internal class CreateMethod {
        internal CreateMethod(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
        }
        private readonly RootAggregate _rootAggregate;

        internal string MethodName => $"Create{_rootAggregate.PhysicalName}Async";
        internal string OnBeforeMethodName => $"OnBeforeCreate{_rootAggregate.PhysicalName}";
        internal string OnAfterMethodName => $"OnAfterCreate{_rootAggregate.PhysicalName}Async";

        internal string Render(CodeRenderingContext ctx) {
            var command = new SaveCommand(_rootAggregate);
            var dbEntity = new EFCoreEntity(_rootAggregate);
            var messages = new SaveCommandMessageContainer(_rootAggregate);

            var keys = _rootAggregate
                .GetKeyVMs()
                .Select((vm, i) => new {
                    LogTemplate = $"{vm.DisplayName.Replace("\"", "\\\"")}: {{key{i}}}",
                    DbEntityFullPath = vm.GetFullPath().AsDbEntity(),
                })
                .ToArray();

            var hasSequence = _rootAggregate
                .EnumerateThisAndDescendants()
                .SelectMany(agg => agg.GetMembers())
                .Any(member => member is ValueMember vm && vm.Type is ValueMemberTypes.SequenceMember);

            return $$"""
                #region 新規登録処理
                /// <summary>
                /// {{_rootAggregate.DisplayName}} の新規登録を実行します。
                /// </summary>
                public virtual async Task {{MethodName}}({{command.CsClassNameCreate}} command, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE}} context) {

                    var dbEntity = command.{{SaveCommand.TO_DBENTITY}}();

                    // 自動的に登録される項目
                    dbEntity.{{EFCoreEntity.VERSION}} = 0;
                    dbEntity.{{EFCoreEntity.CREATED_AT}} = {{ApplicationService.CURRENT_TIME}};
                    dbEntity.{{EFCoreEntity.UPDATED_AT}} = {{ApplicationService.CURRENT_TIME}};
                    dbEntity.{{EFCoreEntity.CREATE_USER}} = {{ApplicationService.CURRENT_USER}};
                    dbEntity.{{EFCoreEntity.UPDATE_USER}} = {{ApplicationService.CURRENT_USER}};

                    // 更新前処理。入力検証や自動補完項目の設定を行なう。
                    {{CheckRequired.METHOD_NAME}}(dbEntity, messages);
                    {{CheckMaxLength.METHOD_NAME}}(dbEntity, messages);
                    {{CheckCharacterType.METHOD_NAME}}(dbEntity, messages);
                    {{CheckDigitsAndScales.METHOD_NAME}}(dbEntity, messages);
                    {{DynamicEnum.METHOD_NAME}}(dbEntity, messages);
                    {{OnBeforeMethodName}}(command, messages, context);

                    // 単なる必須入力漏れなどでもエラーログが出過ぎてしまうのを防ぐため、
                    // IgnoreConfirmがtrueのとき（==更新を確定するつもりのとき）のみ内容をログ出力する
                    if (context.Options.IgnoreConfirm && messages.HasError()) {
                        Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}新規作成で入力エラーが発生した登録内容(JSON): {0}", command.ToJson());
                    }

                    // 「更新しますか？」の確認メッセージが承認される前の1巡目はエラーチェックのみで処理中断
                    if (!context.Options.IgnoreConfirm) return;

                {{If(hasSequence, () => $$"""
                    // シーケンス項目
                    {{SequenceMember.SET_METHOD}}(dbEntity);

                """)}}
                    if (DbContext.Database.CurrentTransaction == null) throw new InvalidOperationException("トランザクションが開始されていません。");

                    // 更新実行
                    const string SAVE_POINT = "SAVE_POINT"; // 更新後処理でエラーが発生した場合はこのデータの更新のみロールバックする
                    await DbContext.Database.CurrentTransaction.CreateSavepointAsync(SAVE_POINT);
                    try {
                        DbContext.Add(dbEntity);
                        await DbContext.SaveChangesAsync();
                    } catch (DbUpdateException ex) {
                        await DbContext.Database.CurrentTransaction.RollbackToSavepointAsync(SAVE_POINT);

                        // 後続処理に影響が出るのを防ぐためエンティティを解放
                        DbContext.Entry(dbEntity).State = EntityState.Detached;
                {{UpdateMethod.RenderDescendantDetaching(_rootAggregate, "dbEntity").SelectTextTemplate(source => $$"""
                        {{WithIndent(source, "        ")}}
                """)}}

                        messages.AddError({{MsgFactory.MSG}}.{{UpdateMethod.ERR_ID_SAVECHANGES}}(ex.Message));
                        Log.Error(ex);
                        Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}新規作成でSQL発行時エラーが発生した登録内容(JSON): {0}", command.ToJson());
                        return;
                    }

                    // 更新後処理
                    try {
                        await {{OnAfterMethodName}}(dbEntity, messages, context);

                        // 後続処理に影響が出るのを防ぐためエンティティを解放
                        DbContext.Entry(dbEntity).State = EntityState.Detached;
                {{UpdateMethod.RenderDescendantDetaching(_rootAggregate, "dbEntity").SelectTextTemplate(source => $$"""
                        {{WithIndent(source, "        ")}}
                """)}}

                        // セーブポイント解放
                        await DbContext.Database.CurrentTransaction.ReleaseSavepointAsync(SAVE_POINT);
                    } catch (Exception ex) {
                        messages.AddError({{MsgFactory.MSG}}.{{UpdateMethod.ERR_ID_SAVECHANGES}}(ex.Message));
                        Log.Error(ex);
                        Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}新規作成後エラーが発生した登録内容(JSON): {0}", command.ToJson());
                        await DbContext.Database.CurrentTransaction.RollbackToSavepointAsync(SAVE_POINT);
                        return;
                    }

                    Log.Info("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}データを新規登録しました。（{{keys.Select(x => x.LogTemplate).Join(", ")}}）", {{keys.Select(x => $"dbEntity.{x.DbEntityFullPath.Join("?.")}").Join(", ")}});
                    Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}} 新規登録パラメータ: {0}", command.ToJson());
                }
                /// <summary>
                /// {{_rootAggregate.DisplayName}} の新規登録の確定前に実行される処理。
                /// 自動生成されないエラーチェックはここで実装する。
                /// エラーがあった場合、第2引数のメッセージにエラー内容を格納する。
                /// </summary>
                public virtual void {{OnBeforeMethodName}}({{command.CsClassNameCreate}} command, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE}} context) {
                    // このメソッドをオーバーライドして処理を実装してください。
                }
                /// <summary>
                /// {{_rootAggregate.DisplayName}} の新規登録のSQL発行後、コミット前に実行される処理。
                /// このメソッドの中で例外が送出された場合、{{_rootAggregate.DisplayName}} の新規登録はロールバックされる。
                /// このメソッドで実装される想定としているものの例は以下。
                /// <list type="bullet">
                /// <item>{{_rootAggregate.DisplayName}}と常に同期していなければならないリードレプリカの更新</item>
                /// <item>{{_rootAggregate.DisplayName}}と常に同期していなければならない外部リソースの更新やメッセージング</item>
                /// </list>
                /// </summary>
                public virtual async Task {{OnAfterMethodName}}({{dbEntity.CsClassName}} newValue, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE}} context) {
                    // このメソッドをオーバーライドして処理を実装してください。
                }
                #endregion 新規登録処理
                """;
        }
    }
}
