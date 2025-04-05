using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Common;
using Nijo.Ver1.Parts.CSharp;
using System;
using System.Linq;

namespace Nijo.Ver1.Models.DataModelModules {
    /// <summary>
    /// 削除処理
    /// </summary>
    internal class DeleteMethod {
        internal DeleteMethod(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
        }
        private readonly RootAggregate _rootAggregate;

        internal string MethodName => $"Delete{_rootAggregate.PhysicalName}Async";
        internal string OnBeforeMethodName => $"OnBeforeDelete{_rootAggregate.PhysicalName}";
        internal string OnAfterMethodName => $"OnAfterDelete{_rootAggregate.PhysicalName}Async";

        internal string Render(CodeRenderingContext ctx) {
            var command = new SaveCommand(_rootAggregate);
            var dbEntity = new EFCoreEntity(_rootAggregate);
            var messages = new SaveCommandMessageContainer(_rootAggregate);

            var keyClass = new KeyClass.KeyClassEntry(_rootAggregate);
            var keys = _rootAggregate
                .GetKeyVMs()
                .Select((vm, i) => {
                    var fullpath = vm.GetPathFromEntry().ToArray();
                    return new {
                        TempVarName = $"searchKey{i + 1}",
                        vm.PhysicalName,
                        vm.DisplayName,
                        LogTemplate = $"{vm.DisplayName.Replace("\"", "\\\"")}: {{key{i}}}",
                        SaveCommandFullPath = fullpath.AsSaveCommand().ToArray(),
                        SaveCommandMessageFullPath = fullpath.AsSaveCommandMessage().ToArray(),
                        DbEntityFullPath = fullpath.AsDbEntity().ToArray(),
                    };
                })
                .ToArray();

            return $$"""
                #region 物理削除処理
                /// <summary>
                /// {{_rootAggregate.DisplayName}} の物理削除を実行します。
                /// </summary>
                public virtual async Task {{MethodName}}({{command.CsClassNameDelete}} command, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE}} context) {

                    // 削除に必要な項目が空の場合は処理中断
                    var keyIsEmpty = false;
                {{keys.SelectTextTemplate(vm => $$"""
                    if (command.{{vm.SaveCommandFullPath.Join("?.")}} == null) {
                        keyIsEmpty = true;
                        messages.{{vm.SaveCommandMessageFullPath.Join(".")}}.AddError({{MsgFactory.MSG}}.{{UpdateMethod.ERR_KEY_IS_EMPTY}}("{{vm.DisplayName.Replace("\"", "\\\"")}}"));
                    }
                """)}}
                    if (keyIsEmpty) {
                        Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}削除で主キー空エラーが発生したデータ: {0}", command.ToJson());
                        return;
                    }

                    // 削除前データ取得
                {{keys.SelectTextTemplate(vm => $$"""
                    var {{vm.TempVarName}} = command.{{vm.SaveCommandFullPath.Join("!.")}};
                """)}}

                    var dbEntity = DbContext.{{dbEntity.DbSetName}}
                        .AsTracking()
                {{dbEntity.RenderInclude().SelectTextTemplate(source => $$"""
                        {{source}}
                """)}}
                        .SingleOrDefault(e {{WithIndent(keys.SelectTextTemplate((vm, i) => $$"""
                                           {{(i == 0 ? "=>" : "&&")}} e.{{vm.DbEntityFullPath.Join("!.")}} == {{vm.TempVarName}}
                                           """), "                        ")}});

                    if (dbEntity == null) {
                        messages.AddError({{MsgFactory.MSG}}.{{UpdateMethod.ERR_DATA_NOT_FOUND}}());
                        Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}削除で削除対象が見つからないエラーが発生したデータ: {0}", command.ToJson());
                        return;
                    }

                    // 更新前処理。入力検証を行なう。
                    {{OnBeforeMethodName}}(command, dbEntity, messages, context);

                    // エラーがある場合は処理中断
                    if (messages.HasError()) {
                        // 単なる必須入力漏れなどでもエラーログが出過ぎてしまうのを防ぐため、
                        // IgnoreConfirmがtrueのとき（==更新を確定するつもりのとき）のみ内容をログ出力する
                        if (context.Options.IgnoreConfirm) {
                            Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}削除で入力エラーが発生した登録内容(JSON): {0}", command.ToJson());
                        }
                        return;
                    }

                    // 「更新しますか？」の確認メッセージが承認される前の1巡目はエラーチェックのみで処理中断
                    if (!context.Options.IgnoreConfirm) return;

                    if (DbContext.Database.CurrentTransaction == null) throw new InvalidOperationException("トランザクションが開始されていません。");

                    // 削除実行
                    const string SAVE_POINT = "SAVE_POINT"; // 更新後処理でエラーが発生した場合はこのデータの更新のみロールバックする
                    try {
                        var entry = DbContext.Entry(dbEntity);
                        entry.State = EntityState.Deleted;
                        entry.Property(e => e.{{EFCoreEntity.VERSION}}).OriginalValue = command.{{SaveCommand.VERSION}};
                        entry.Property(e => e.{{EFCoreEntity.VERSION}}).CurrentValue = command.{{SaveCommand.VERSION}};

                        await DbContext.Database.CurrentTransaction.CreateSavepointAsync(SAVE_POINT);
                        await DbContext.SaveChangesAsync();

                    } catch (DbUpdateException ex) {
                        await DbContext.Database.CurrentTransaction.RollbackToSavepointAsync(SAVE_POINT);

                        // 後続処理に影響が出るのを防ぐためエンティティを解放
                        DbContext.Entry(dbEntity).State = EntityState.Detached;
                {{UpdateMethod.RenderDescendantDetaching(_rootAggregate, "dbEntity").SelectTextTemplate(source => $$"""
                        {{WithIndent(source, "        ")}}
                """)}}

                        if (ex is DbUpdateConcurrencyException) {
                            messages.AddError({{MsgFactory.MSG}}.{{UpdateMethod.ERR_CONCURRENCY}}());
                            Log.Warn("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}削除で楽観排他エラー: {0}", command.ToJson());

                        } else {
                            messages.AddError({{MsgFactory.MSG}}.{{UpdateMethod.ERR_ID_UNKNOWN}}(ex.Message));
                            Log.Error(ex);
                            Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}削除でSQL発行時エラーが発生した登録内容(JSON): {0}", command.ToJson());
                        }
                        return;
                    }

                    // 削除後処理
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
                        messages.AddError({{MsgFactory.MSG}}.{{UpdateMethod.ERR_ID_UNKNOWN}}(ex.Message));
                        Log.Error(ex);
                        Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}削除後エラーが発生した登録内容(JSON): {0}", command.ToJson());
                        await DbContext.Database.CurrentTransaction.RollbackToSavepointAsync(SAVE_POINT);
                        return;
                    }

                    Log.Info("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}}データを物理削除しました。（{{keys.Select(x => x.LogTemplate).Join(", ")}}）", {{keys.Select(x => $"dbEntity.{x.DbEntityFullPath.Join("?.")}").Join(", ")}});
                    Log.Debug("{{_rootAggregate.DisplayName.Replace("\"", "\\\"")}} 削除パラメータ: {0}", command.ToJson());
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
                public virtual Task {{OnAfterMethodName}}({{dbEntity.CsClassName}} oldValue, {{messages.InterfaceName}} messages, {{PresentationContext.INTERFACE}} context) {
                    // このメソッドをオーバーライドして処理を実装してください。
                    return Task.CompletedTask;
                }
                #endregion 物理削除処理
                """;
        }
    }
}
