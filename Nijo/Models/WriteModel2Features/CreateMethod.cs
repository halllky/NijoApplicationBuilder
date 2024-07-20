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

        internal string BeforeMethodName => $"Creating{_rootAggregate.Item.PhysicalName}";
        internal string MethodName => $"Create{_rootAggregate.Item.PhysicalName}";
        internal string AfterMethodName => $"Created{_rootAggregate.Item.PhysicalName}";

        /// <summary>
        /// データ新規登録処理をレンダリングします。
        /// </summary>
        internal string Render(CodeRenderingContext context) {
            var appSrv = new ApplicationService();
            var efCoreEntity = new EFCoreEntity(_rootAggregate);
            var dataClass = new DataClassForSave(_rootAggregate, DataClassForSave.E_Type.Create);
            var argType = $"{DataClassForSaveBase.CREATE_COMMAND}<{dataClass.CsClassName}>";
            var beforeSaveContext = $"{SaveContext.BEFORE_SAVE_CONTEXT}<{dataClass.ErrorDataCsClassName}>";
            var afterSaveContext = $"{SaveContext.AFTER_SAVE_CONTEXT}";

            return $$"""
                /// <summary>
                /// 新しい{{_rootAggregate.Item.DisplayName}}を作成する情報を受け取って登録します。
                /// </summary>
                public virtual void {{MethodName}}({{argType}} command, {{SaveContext.BATCH_UPDATE_CONTEXT}} saveContext) {

                    var dbEntity = command.{{DataClassForSaveBase.VALUES_CS}}.{{DataClassForSave.TO_DBENTITY}}();

                    // 自動的に設定される項目
                    dbEntity.{{EFCoreEntity.VERSION}} = 0;
                    dbEntity.{{EFCoreEntity.CREATED_AT}} = {{ApplicationService.CURRENT_TIME}};
                    dbEntity.{{EFCoreEntity.UPDATED_AT}} = {{ApplicationService.CURRENT_TIME}};
                    dbEntity.{{EFCoreEntity.CREATE_USER}} = {{ApplicationService.CURRENT_USER}};
                    dbEntity.{{EFCoreEntity.UPDATE_USER}} = {{ApplicationService.CURRENT_USER}};

                    // 更新前処理。入力検証や自動補完項目の設定を行う。
                    var beforeSaveContext = new {{beforeSaveContext}}(saveContext);
                    {{BeforeMethodName}}(dbEntity, beforeSaveContext);

                    // エラーやコンファームがある場合は処理中断
                    if (beforeSaveContext.Errors.HasError()) return;
                    if (!beforeSaveContext.IgnoreConfirm && beforeSaveContext.HasConfirm()) return;

                    // 更新実行
                    try {
                        {{appSrv.DbContext}}.Add(dbEntity);
                        {{appSrv.DbContext}}.SaveChanges();
                    } catch (DbUpdateException ex) {
                        beforeSaveContext.Errors.Add(ex);
                        return;
                    }

                    // 更新後処理
                    var afterSaveContext = new {{afterSaveContext}}();
                    {{AfterMethodName}}(dbEntity, afterSaveContext);
                }

                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}の新規登録前に実行されます。
                /// エラーチェック、ワーニング、自動算出項目の設定などを行います。
                /// </summary>
                protected virtual void {{BeforeMethodName}}({{efCoreEntity.ClassName}} dbEntity, {{beforeSaveContext}} context) {
                    // このメソッドをオーバーライドしてエラーチェック等を記述してください。
                }
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}の新規登録SQL発行後、コミット前に実行されます。
                /// </summary>
                protected virtual void {{AfterMethodName}}({{efCoreEntity.ClassName}} dbEntity, {{afterSaveContext}} context) {
                    // このメソッドをオーバーライドして必要な更新後処理を記述してください。
                }
                
                """;
        }
    }
}
