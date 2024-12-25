using Nijo.Core;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// 既存データ削除処理
    /// </summary>
    internal class DeleteMethod {
        internal DeleteMethod(GraphNode<Aggregate> rootAggregate) {
            _rootAggregate = rootAggregate;
        }

        private readonly GraphNode<Aggregate> _rootAggregate;

        internal object MethodName => $"Delete{_rootAggregate.Item.PhysicalName}";
        internal string BeforeMethodName => $"OnBeforeDelete{_rootAggregate.Item.PhysicalName}";
        internal string AfterMethodName => $"OnAfterDelete{_rootAggregate.Item.PhysicalName}";

        /// <summary>
        /// データ削除処理をレンダリングします。
        /// </summary>
        internal string Render(CodeRenderingContext context) {
            var appSrv = new ApplicationService();
            var efCoreEntity = new EFCoreEntity(_rootAggregate);
            var dataClass = new DataClassForSave(_rootAggregate, DataClassForSave.E_Type.UpdateOrDelete);
            var argType = $"{DataClassForSaveBase.DELETE_COMMAND}<{dataClass.CsClassName}>";

            var keys = _rootAggregate
                .GetKeys()
                .OfType<AggregateMember.ValueMember>()
                .Select((vm, i) => new {
                    TempVarName = $"searchKey{i + 1}",
                    PhysicalName = vm.MemberName,
                    vm.DisplayName,
                    DbEntityFullPath = vm.Declared.GetFullPathAsDbEntity().ToArray(),
                    SaveCommandFullPath = vm.Declared.GetFullPathAsForSave(),
                    ErrorFullPath = vm.Declared.Owner.IsOutOfEntryTree()
                        ? vm.Declared.Owner.GetRefEntryEdge().Terminal.GetFullPathAsForSave()
                        : vm.Declared.GetFullPathAsForSave(),
                })
                .ToArray();

            return $$"""
                /// <summary>
                /// 既存の{{_rootAggregate.Item.DisplayName}}を削除します。
                /// </summary>
                public virtual void {{MethodName}}({{argType}} after, {{dataClass.MessageDataCsInterfaceName}} messages, {{SaveContext.STATE_CLASS_NAME}} batchUpdateState) {

                    // 削除に必要な項目が空の場合は処理中断
                {{keys.SelectTextTemplate(k => $$"""
                    if (after.{{DataClassForSaveBase.VALUES_CS}}.{{k.SaveCommandFullPath.Join("?.")}} == null) {
                        messages.{{k.ErrorFullPath.Join(".")}}.AddError("{{k.PhysicalName}}が空です。");
                    }
                """)}}
                    if (messages.HasError()) {
                        return;
                    }

                    #pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
                    // 削除前データ取得
                {{keys.SelectTextTemplate(k => $$"""
                    var {{k.TempVarName}} = after.{{DataClassForSaveBase.VALUES_CS}}.{{k.SaveCommandFullPath.Join(".")}};
                """)}}

                    var beforeDbEntity = {{appSrv.DbContext}}.{{efCoreEntity.DbSetName}}
                        {{WithIndent(efCoreEntity.RenderInclude(true), "        ")}}
                        .SingleOrDefault(e {{WithIndent(keys.SelectTextTemplate((k, i) => $$"""
                                           {{(i == 0 ? "=>" : "&&")}} e.{{k.DbEntityFullPath.Join(".")}} == {{k.TempVarName}}
                                           """), "                           ")}});
                    #pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。

                    if (beforeDbEntity == null) {
                        messages.AddError("削除対象のデータが見つかりません。");
                        return;
                    }

                    var afterDbEntity = after.{{DataClassForSaveBase.VALUES_CS}}.{{DataClassForSave.TO_DBENTITY}}();
                    afterDbEntity.{{EFCoreEntity.VERSION}} = after.{{DataClassForSaveBase.VERSION_CS}};

                    // 楽観排他制御（なおこことは別に、SQL発行時にEntityFramework側でも重ねて楽観排他の確認がなされる）
                    if (beforeDbEntity.{{EFCoreEntity.VERSION}} != afterDbEntity.{{EFCoreEntity.VERSION}}) {
                        messages.AddError("ほかのユーザーが更新しました。");
                        return;
                    }

                    // 削除前処理。入力検証や自動補完項目の設定を行う。
                    var beforeSaveArgs = new {{SaveContext.BEFORE_SAVE}}<{{dataClass.MessageDataCsInterfaceName}}>(batchUpdateState, messages);
                    {{BeforeMethodName}}(beforeDbEntity, afterDbEntity, beforeSaveArgs);

                    // 一括更新データ全件のうち1件でもエラーやコンファームがある場合は処理中断
                    if (batchUpdateState.HasError()) return;
                    if (!batchUpdateState.Options.IgnoreConfirm && batchUpdateState.HasConfirm()) return;

                    // 削除実行
                    const string SAVE_POINT = "SAVE_POINT"; // 更新後処理でエラーが発生した場合はこのデータの更新のみロールバックする
                                                            // EntityのStateはUnchangedのままなので、
                                                            // 以降のデータの更新時にこのデータの更新SQLが再度発行されるといったことはない。
                    try {
                        {{appSrv.DbContext}}.Database.CurrentTransaction!.CreateSavepoint(SAVE_POINT);
                        {{appSrv.DbContext}}.Remove(beforeDbEntity);
                        {{appSrv.DbContext}}.SaveChanges();
                    } catch (DbUpdateException ex) {
                        messages.AddError(string.Join(Environment.NewLine, ex.GetMessagesRecursively()));
                        return;
                    }

                    // 削除後処理
                    try {
                        var afterSaveEventArgs = new {{SaveContext.AFTER_SAVE_EVENT_ARGS}}(batchUpdateState);
                        {{AfterMethodName}}(beforeDbEntity, afterDbEntity, afterSaveEventArgs);

                        // 後続処理に影響が出るのを防ぐためエンティティを解放
                        {{appSrv.DbContext}}.Entry(afterDbEntity).State = EntityState.Detached;
                        {{WithIndent(UpdateMethod.RenderDescendantDetaching(_rootAggregate, "afterDbEntity"), "        ")}}

                        // セーブポイント解放
                        {{appSrv.DbContext}}.Database.CurrentTransaction!.ReleaseSavepoint(SAVE_POINT);
                    } catch (Exception ex) {
                        messages.AddError($"更新後処理でエラーが発生しました: {string.Join(Environment.NewLine, ex.GetMessagesRecursively())}");
                        {{appSrv.DbContext}}.Database.CurrentTransaction!.RollbackToSavepoint(SAVE_POINT);
                        return;
                    }

                    Log.Info("{{_rootAggregate.Item.DisplayName.Replace("\"", "\\\"")}}データを物理削除しました。（{{keys.Select((x, i) => $"{x.DisplayName.Replace("\"", "\\\"")}: {{key{i}}}").Join(", ")}}）", {{keys.Select(x => $"afterDbEntity.{x.DbEntityFullPath.Join("?.")}").Join(", ")}});
                    Log.Debug("削除データ: {0}", after.ToJson());
                }

                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}の削除前に実行されます。
                /// エラーチェック、ワーニング、自動算出項目の設定などを行います。
                /// </summary>
                protected virtual void {{BeforeMethodName}}({{efCoreEntity.ClassName}} beforeDbEntity, {{efCoreEntity.ClassName}} afterDbEntity, {{SaveContext.BEFORE_SAVE}}<{{dataClass.MessageDataCsInterfaceName}}> e) {
                    // このメソッドをオーバーライドしてエラーチェック等を記述してください。
                }
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}の削除SQL発行後、コミット前に実行されます。
                /// </summary>
                protected virtual void {{AfterMethodName}}({{efCoreEntity.ClassName}} beforeDbEntity, {{efCoreEntity.ClassName}} afterDbEntity, {{SaveContext.AFTER_SAVE_EVENT_ARGS}} e) {
                    // このメソッドをオーバーライドして必要な削除後処理を記述してください。
                }
                """;
        }
    }
}
