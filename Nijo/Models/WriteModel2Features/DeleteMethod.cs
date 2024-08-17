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
                    vm.MemberName,
                    DbEntityFullPath = vm.Declared.GetFullPathAsDbEntity().ToArray(),
                    ErrorFullPath = vm.Declared.Owner.IsOutOfEntryTree()
                        ? vm.Declared.Owner.GetRefEntryEdge().Terminal.GetFullPathAsForSave()
                        : vm.Declared.GetFullPathAsForSave(),
                })
                .ToArray();

            return $$"""
                /// <summary>
                /// 既存の{{_rootAggregate.Item.DisplayName}}を削除します。
                /// </summary>
                public virtual void {{MethodName}}({{argType}} after, {{SaveContext.BEFORE_SAVE}}<{{dataClass.ErrorDataCsClassName}}> saveContext) {
                    var afterDbEntity = after.{{DataClassForSaveBase.VALUES_CS}}.{{DataClassForSave.TO_DBENTITY}}();

                    // 更新に必要な項目が空の場合は処理中断
                {{keys.SelectTextTemplate(k => $$"""
                    if (afterDbEntity.{{k.DbEntityFullPath.Join("?.")}} == null) {
                        saveContext.Errors.{{k.ErrorFullPath.Join(".")}}.Add("{{k.MemberName}}が空です。");
                    }
                """)}}
                    if (afterDbEntity.{{EFCoreEntity.VERSION}} == null) {
                        saveContext.Errors.Add("更新時は更新前データのバージョンを指定する必要があります。");
                    }
                    if (saveContext.Errors.HasError()) {
                        return;
                    }

                    #pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
                    // 更新前データ取得
                {{keys.SelectTextTemplate(k => $$"""
                    var {{k.TempVarName}} = afterDbEntity.{{k.DbEntityFullPath.Join(".")}};
                """)}}

                    var beforeDbEntity = {{appSrv.DbContext}}.{{efCoreEntity.DbSetName}}
                        {{WithIndent(efCoreEntity.RenderInclude(true), "        ")}}
                        .SingleOrDefault(e {{WithIndent(keys.SelectTextTemplate((k, i) => $$"""
                                           {{(i == 0 ? "=>" : "&&")}} e.{{k.DbEntityFullPath.Join(".")}} == {{k.TempVarName}}
                                           """), "                           ")}});
                    #pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。

                    if (beforeDbEntity == null) {
                        saveContext.Errors.Add("削除対象のデータが見つかりません。");
                        return;
                    }
                    if (beforeDbEntity.{{EFCoreEntity.VERSION}} != afterDbEntity.{{EFCoreEntity.VERSION}}) {
                        saveContext.Errors.Add("ほかのユーザーが更新しました。");
                        return;
                    }

                    // 更新前処理。入力検証や自動補完項目の設定を行う。
                    {{BeforeMethodName}}(beforeDbEntity, afterDbEntity, saveContext);

                    // エラーやコンファームがある場合は処理中断
                    if (saveContext.Errors.HasError()) return;
                    if (!saveContext.Options.IgnoreConfirm && saveContext.HasConfirm()) return;

                    // 更新実行
                    try {
                        {{appSrv.DbContext}}.Remove(beforeDbEntity);
                        {{appSrv.DbContext}}.SaveChanges();
                    } catch (DbUpdateException ex) {
                        saveContext.Errors.Add(ex.Message);
                        return;
                    }

                    // 更新後処理
                    var afterSaveContext = new {{dataClass.AfterSaveContextCsClassName}}();
                    {{AfterMethodName}}(beforeDbEntity, afterDbEntity, afterSaveContext);
                }

                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}の更新前に実行されます。
                /// エラーチェック、ワーニング、自動算出項目の設定などを行います。
                /// </summary>
                protected virtual void {{BeforeMethodName}}({{efCoreEntity.ClassName}} beforeDbEntity, {{efCoreEntity.ClassName}} afterDbEntity, {{SaveContext.BEFORE_SAVE}}<{{dataClass.ErrorDataCsClassName}}> context) {
                    // このメソッドをオーバーライドしてエラーチェック等を記述してください。
                }
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}の更新SQL発行後、コミット前に実行されます。
                /// </summary>
                protected virtual void {{AfterMethodName}}({{efCoreEntity.ClassName}} beforeDbEntity, {{efCoreEntity.ClassName}} afterDbEntity, {{dataClass.AfterSaveContextCsClassName}} context) {
                    // このメソッドをオーバーライドして必要な更新後処理を記述してください。
                }
                """;
        }
    }
}
