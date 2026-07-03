using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Parts.CSharp;
using Nijo.Util.DotnetEx;
using Nijo.ValueMemberTypes;
using Nijo.Parts.Common;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Nijo.Models.DataModelModules {
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

        internal string Render(CodeRenderingContext ctx, IReadOnlyDictionary<SchemaNodeIdentity, InstancePropertyPath> variablePathInfo) {
            var command = new SaveCommand(_rootAggregate, SaveCommand.E_Type.Create);
            var dbEntity = new EFCoreEntity(_rootAggregate);
            var messages = new SaveCommandMessageContainer(_rootAggregate);

            var pkValueCandidates = new Variable("dbEntity", dbEntity)
                .CreateProperties()
                .ToArray();
            var keys = _rootAggregate
                .GetKeyVMs()
                .Select((vm, i) => new {
                    LogTemplate = $"{vm.DisplayName.Replace("\"", "\\\"")}: {{key{i}}}",
                    DbEntityFullPath = pkValueCandidates
                        .Single(x => x.Metadata.SchemaPathNode.ToMappingKey() == vm.ToMappingKey())
                        .GetJoinedPathFromInstance(E_CsTs.CSharp, "?."),
                })
                .ToArray();

            return $$"""
                #region 新規登録処理
                /// <summary>
                /// {{_rootAggregate.DisplayName}} の新規登録を実行します。
                /// </summary>
                /// <param name="command">新規登録するデータ</param>
                /// <param name="context">コンテキスト</param>
                /// <param name="messageOwner">
                /// エラーメッセージを特定の位置に付加したい場合は指定する。
                /// nullの場合はコンテキストのルートに付加される。
                /// </param>
                /// <returns>エラーがあった場合やエラーチェックのみの場合はfalseを、正常終了した場合はtrueと新規登録後のデータを返す。</returns>
                public virtual async Task<DataModelSaveResult<{{dbEntity.CsClassName}}>> {{MethodName}}({{command.CsClassNameCreate}} command, {{PresentationContext.INTERFACE}} context, {{MessageContainer.SETTER_INTERFACE}}? messageOwner = null) {
                    var dbEntity = command.{{SaveCommand.TO_DBENTITY}}();
                    var messages = messageOwner?.As<{{messages.InterfaceName}}>() ?? context.As<{{messages.InterfaceName}}>().Messages;

                    // 自動的に登録される項目
                    dbEntity.{{EFCoreEntity.VERSION}} = 0;
                    dbEntity.{{EFCoreEntity.CREATED_AT}} = {{ApplicationService.CURRENT_TIME}};
                    dbEntity.{{EFCoreEntity.UPDATED_AT}} = {{ApplicationService.CURRENT_TIME}};
                    dbEntity.{{EFCoreEntity.CREATE_USER}} = {{ApplicationService.CURRENT_USER}};
                    dbEntity.{{EFCoreEntity.UPDATE_USER}} = {{ApplicationService.CURRENT_USER}};

                    // 更新前処理。入力検証や自動補完項目の設定を行なう。
                {{DataModel.GetValidators(ctx).SelectTextTemplate(validator => $$"""
                    {{validator.RenderCaller(this, _rootAggregate, "dbEntity", "messages")}};
                """)}}
                    {{OnBeforeMethodName}}(command, messages, context);

                    // エラーがある場合は処理中断
                    if (messages.GetState()?.DescendantsAndSelf().Any(c => c.Errors.Count > 0) == true) {
                        // 単なる必須入力漏れなどでもエラーログが出過ぎてしまうのを防ぐため、
                        // 更新を確定するつもりのときのみ内容をログ出力する
                        if (!context.ValidationOnly) {
                            Log.LogDebug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}新規作成で入力エラーが発生した登録内容(JSON): {data}", {{ApplicationService.SERIALIZE_FOR_LOG}}(command));
                        }
                        return new(DataModelSaveErrorReason.ValidationError);
                    }

                    // 「更新しますか？」の確認メッセージが承認される前の1巡目はエラーチェックのみで処理中断
                    if (context.ValidationOnly) return new(true);
                    if (DbContext.Database.CurrentTransaction == null) throw new InvalidOperationException("トランザクションが開始されていません。");

                    // 更新実行
                    const string SAVE_POINT = "SAVE_POINT"; // 更新後処理でエラーが発生した場合はこのデータの更新のみロールバックする
                    await DbContext.Database.CurrentTransaction.CreateSavepointAsync(SAVE_POINT).ConfigureAwait(false);
                    try {
                        DbContext.Add(dbEntity);
                        await DbContext.SaveChangesAsync().ConfigureAwait(false);

                    } catch (DbUpdateException ex) {
                        await DbContext.Database.CurrentTransaction.RollbackToSavepointAsync(SAVE_POINT).ConfigureAwait(false);

                        // 後続処理に影響が出るのを防ぐためエンティティを解放
                        DbContext.Entry(dbEntity).State = EntityState.Detached;
                {{UpdateMethod.RenderDescendantDetaching(_rootAggregate, variablePathInfo, "dbEntity").SelectTextTemplate(source => $$"""
                        {{WithIndent(source)}}
                """)}}

                        messages.AddError({{MsgFactory.MSG}}.{{UpdateMethod.ERR_ID_UNKNOWN}}(ex.Message));
                        Log.LogError(ex, "新規作成中にエラーが発生しました。");
                        Log.LogDebug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}新規作成でSQL発行時エラーが発生した登録内容(JSON): {data}", {{ApplicationService.SERIALIZE_FOR_LOG}}(command));

                        return new(DataModelSaveErrorReason.ValidationError);
                    }

                    // 更新後処理
                    try {
                        await {{OnAfterMethodName}}(dbEntity, messages, context);

                        // 後続処理に影響が出るのを防ぐためエンティティを解放
                        DbContext.Entry(dbEntity).State = EntityState.Detached;
                {{UpdateMethod.RenderDescendantDetaching(_rootAggregate, variablePathInfo, "dbEntity").SelectTextTemplate(source => $$"""
                        {{WithIndent(source)}}
                """)}}

                        // セーブポイント解放
                        await DbContext.Database.CurrentTransaction.ReleaseSavepointAsync(SAVE_POINT).ConfigureAwait(false);

                    } catch (Exception ex) {
                        messages.AddError({{MsgFactory.MSG}}.{{UpdateMethod.ERR_ID_UNKNOWN}}(ex.Message));
                        Log.LogError(ex, "新規作成後の処理中にエラーが発生しました。");
                        Log.LogDebug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}新規作成後エラーが発生した登録内容(JSON): {data}", {{ApplicationService.SERIALIZE_FOR_LOG}}(command));
                        await DbContext.Database.CurrentTransaction.RollbackToSavepointAsync(SAVE_POINT).ConfigureAwait(false);
                        return new(DataModelSaveErrorReason.AfterSaveError);
                    }

                    Log.LogInformation("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}データを新規登録しました。（{{keys.Select(x => x.LogTemplate).Join(", ")}}）", {{keys.Select(x => x.DbEntityFullPath).Join(", ")}});
                    Log.LogDebug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}} 新規登録パラメータ: {data}", {{ApplicationService.SERIALIZE_FOR_LOG}}(command));

                    return new(dbEntity);
                }
                /// <summary>
                /// {{_rootAggregate.DisplayName}} の新規登録の確定前に実行される処理。
                /// このメソッドの中でエラーが追加された場合、{{_rootAggregate.DisplayName}} の新規登録は中断される。
                /// どの画面・バッチから更新された場合であっても必ず {{_rootAggregate.DisplayName}} が満たしていなければならない整合性はここで実装する。
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
                public virtual Task {{OnAfterMethodName}}({{dbEntity.CsClassName}} newValue, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE}} context) {
                    // このメソッドをオーバーライドして処理を実装してください。
                    return Task.CompletedTask;
                }
                #endregion 新規登録処理
                """;
        }
    }
}
