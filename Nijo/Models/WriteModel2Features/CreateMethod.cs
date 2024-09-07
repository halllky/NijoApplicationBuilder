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
    /// 新規データ登録処理
    /// </summary>
    internal class CreateMethod {
        internal CreateMethod(GraphNode<Aggregate> rootAggregate) {
            _rootAggregate = rootAggregate;
        }

        private readonly GraphNode<Aggregate> _rootAggregate;

        internal string MethodName => $"Create{_rootAggregate.Item.PhysicalName}";
        internal string BeforeMethodName => $"OnBeforeCreate{_rootAggregate.Item.PhysicalName}";
        internal string AfterMethodName => $"OnAfterCreate{_rootAggregate.Item.PhysicalName}";

        /// <summary>
        /// データ新規登録処理をレンダリングします。
        /// </summary>
        internal string Render(CodeRenderingContext context) {
            var appSrv = new ApplicationService();
            var efCoreEntity = new EFCoreEntity(_rootAggregate);
            var dataClass = new DataClassForSave(_rootAggregate, DataClassForSave.E_Type.Create);
            var argType = $"{DataClassForSaveBase.CREATE_COMMAND}<{dataClass.CsClassName}>";

            return $$"""
                /// <summary>
                /// 新しい{{_rootAggregate.Item.DisplayName}}を作成する情報を受け取って登録します。
                /// </summary>
                public virtual void {{MethodName}}({{argType}} command, {{SaveContext.STATE_CLASS_NAME}} batchUpdateState) {
                    var errors = ({{dataClass.ErrorDataCsClassName}})batchUpdateState.{{SaveContext.GET_ERR_MSG_CONTAINER}}(command);

                    var dbEntity = command.{{DataClassForSaveBase.VALUES_CS}}.{{DataClassForSave.TO_DBENTITY}}();

                    // 自動的に設定される項目
                    dbEntity.{{EFCoreEntity.VERSION}} = 0;
                    dbEntity.{{EFCoreEntity.CREATED_AT}} = {{ApplicationService.CURRENT_TIME}};
                    dbEntity.{{EFCoreEntity.UPDATED_AT}} = {{ApplicationService.CURRENT_TIME}};
                    dbEntity.{{EFCoreEntity.CREATE_USER}} = {{ApplicationService.CURRENT_USER}};
                    dbEntity.{{EFCoreEntity.UPDATE_USER}} = {{ApplicationService.CURRENT_USER}};

                    // 更新前処理。入力検証や自動補完項目の設定を行う。
                    var beforeSaveArgs = new {{SaveContext.BEFORE_SAVE}}<{{dataClass.ErrorDataCsClassName}}>(batchUpdateState, errors);
                    {{BeforeMethodName}}(dbEntity, beforeSaveArgs);

                    // 一括更新データ全件のうち1件でもエラーやコンファームがある場合は処理中断
                    if (batchUpdateState.HasError()) return;
                    if (!batchUpdateState.Options.IgnoreConfirm && batchUpdateState.HasConfirm()) return;

                    // 更新実行
                    const string SAVE_POINT = "SAVE_POINT"; // 更新後処理でエラーが発生した場合はこのデータの更新のみロールバックする
                                                            // EntityのStateはUnchangedのままなので、
                                                            // 以降のデータの更新時にこのデータの更新SQLが再度発行されるといったことはない。
                    {{appSrv.DbContext}}.Database.CurrentTransaction!.CreateSavepoint(SAVE_POINT);
                    try {
                        {{appSrv.DbContext}}.Add(dbEntity);
                        {{appSrv.DbContext}}.SaveChanges();
                    } catch (DbUpdateException ex) {
                        {{appSrv.DbContext}}.Database.CurrentTransaction!.RollbackToSavepoint(SAVE_POINT);
                        errors.Add(ex.Message);
                        return;
                    }

                    // 更新後処理
                    try {
                        var afterSaveEventArgs = new {{SaveContext.AFTER_SAVE_EVENT_ARGS}}(batchUpdateState);
                        {{AfterMethodName}}(dbEntity, afterSaveEventArgs);
                        {{appSrv.DbContext}}.Database.CurrentTransaction!.ReleaseSavepoint(SAVE_POINT);
                    } catch (Exception ex) {
                        errors.Add($"更新後処理でエラーが発生しました: {ex.Message}");
                        {{appSrv.DbContext}}.Database.CurrentTransaction!.RollbackToSavepoint(SAVE_POINT);
                    }
                }

                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}の新規登録前に実行されます。
                /// エラーチェック、ワーニング、自動算出項目の設定などを行います。
                /// </summary>
                protected virtual void {{BeforeMethodName}}({{efCoreEntity.ClassName}} dbEntity, {{SaveContext.BEFORE_SAVE}}<{{dataClass.ErrorDataCsClassName}}> e) {
                    // このメソッドをオーバーライドしてエラーチェック等を記述してください。
                }
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}の新規登録SQL発行後、コミット前に実行されます。
                /// </summary>
                protected virtual void {{AfterMethodName}}({{efCoreEntity.ClassName}} dbEntity, {{SaveContext.AFTER_SAVE_EVENT_ARGS}} e) {
                    // このメソッドをオーバーライドして必要な更新後処理を記述してください。
                }
                
                """;
        }
    }
}
