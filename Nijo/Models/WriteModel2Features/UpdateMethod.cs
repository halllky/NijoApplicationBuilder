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
    /// 既存データ更新処理
    /// </summary>
    internal class UpdateMethod {
        internal UpdateMethod(GraphNode<Aggregate> rootAggregate) {
            _rootAggregate = rootAggregate;
        }

        private readonly GraphNode<Aggregate> _rootAggregate;

        internal object MethodName => $"Update{_rootAggregate.Item.PhysicalName}";
        internal string BeforeMethodName => $"OnBeforeUpdate{_rootAggregate.Item.PhysicalName}";
        internal string AfterMethodName => $"OnAfterUpdate{_rootAggregate.Item.PhysicalName}";

        /// <summary>
        /// データ更新処理をレンダリングします。
        /// </summary>
        internal string Render(CodeRenderingContext context) {
            var appSrv = new ApplicationService();
            var efCoreEntity = new EFCoreEntity(_rootAggregate);
            var dataClass = new DataClassForSave(_rootAggregate, DataClassForSave.E_Type.UpdateOrDelete);
            var argType = $"{DataClassForSaveBase.UPDATE_COMMAND}<{dataClass.CsClassName}>";

            var keys = _rootAggregate
                .GetKeys()
                .OfType<AggregateMember.ValueMember>();

            return $$"""
                /// <summary>
                /// 既存の{{_rootAggregate.Item.DisplayName}}を更新します。
                /// </summary>
                public virtual void {{MethodName}}({{argType}} after, {{SaveContext.BEFORE_SAVE}}<{{dataClass.ErrorDataCsClassName}}> saveContext) {
                    var afterDbEntity = after.{{DataClassForSaveBase.VALUES_CS}}.{{DataClassForSave.TO_DBENTITY}}();

                    // 更新に必要な項目が空の場合は処理中断
                {{keys.SelectTextTemplate(vm => $$"""
                    if (afterDbEntity.{{vm.Declared.GetFullPathAsDbEntity().Join("?.")}} == null) {
                        saveContext.Errors.{{vm.Declared.GetFullPathAsDbEntity().Join(".")}}.Add("{{vm.MemberName}}が空です。");
                    }
                """)}}
                    if (afterDbEntity.{{EFCoreEntity.VERSION}} == null) {
                        saveContext.Errors.Add("更新時は更新前データのバージョンを指定する必要があります。");
                    }
                    if (saveContext.Errors.HasError()) {
                        return;
                    }

                    // 更新前データ取得
                    var beforeDbEntity = {{appSrv.DbContext}}.{{efCoreEntity.DbSetName}}
                        .AsNoTracking()
                        {{WithIndent(efCoreEntity.RenderInclude(true), "        ")}}
                        .SingleOrDefault(e {{WithIndent(keys.SelectTextTemplate((vm, i) => $$"""
                                           {{(i == 0 ? "=>" : "&&")}} e.{{vm.GetFullPathAsDbEntity().Join(".")}} == afterDbEntity.{{vm.Declared.GetFullPathAsDbEntity().Join(".")}}
                                           """), "                           ")}});
                    if (beforeDbEntity == null) {
                        saveContext.Errors.Add("更新対象のデータが見つかりません。");
                        return;
                    }
                    if (beforeDbEntity.{{EFCoreEntity.VERSION}} != afterDbEntity.{{EFCoreEntity.VERSION}}) {
                        saveContext.Errors.Add("ほかのユーザーが更新しました。");
                        return;
                    }

                    // 自動的に設定される項目
                    afterDbEntity.{{EFCoreEntity.VERSION}}++;
                    afterDbEntity.{{EFCoreEntity.UPDATED_AT}} = {{ApplicationService.CURRENT_TIME}};
                    afterDbEntity.{{EFCoreEntity.UPDATE_USER}} = {{ApplicationService.CURRENT_USER}};

                    // 更新前処理。入力検証や自動補完項目の設定を行う。
                    {{BeforeMethodName}}(beforeDbEntity, afterDbEntity, saveContext);

                    // エラーやコンファームがある場合は処理中断
                    if (saveContext.Errors.HasError()) return;
                    if (!saveContext.Options.IgnoreConfirm && saveContext.HasConfirm()) return;

                    // 更新実行
                    try {
                        var entry = {{appSrv.DbContext}}.Entry(afterDbEntity);
                        entry.State = EntityState.Modified;
                        entry.Property(e => e.{{EFCoreEntity.VERSION}}).OriginalValue = beforeDbEntity.{{EFCoreEntity.VERSION}};

                        {{WithIndent(RenderDescendantAttaching(), "        ")}}
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


        /// <summary>
        /// DbContextに更新後の子孫要素のエンティティをアタッチさせる処理をレンダリングします。
        /// </summary>
        private string RenderDescendantAttaching() {
            var dbContext = new ApplicationService().DbContext;
            var builder = new StringBuilder();

            var descendantDbEntities = _rootAggregate.EnumerateDescendants().ToArray();
            for (int i = 0; i < descendantDbEntities.Length; i++) {
                var paths = descendantDbEntities[i].PathFromEntry().ToArray();
                var before = $"before{descendantDbEntities[i].Item.PhysicalName}_{i}";
                var after_ = $"after{descendantDbEntities[i].Item.PhysicalName}_{i}";

                // before, after それぞれの子孫インスタンスを一次配列に格納する
                void RenderEntityArray(bool renderBefore) {
                    var tempVar = renderBefore ? before : after_;

                    if (paths.Any(path => path.Terminal.As<Aggregate>().IsChildrenMember())) {
                        // 子集約までの経路の途中に配列が含まれる場合
                        builder.Append($"var {tempVar} {(renderBefore ? "= beforeDbEntity" : " =  afterDbEntity")}");

                        var select = false;
                        foreach (var path in paths) {
                            if (select && path.Terminal.As<Aggregate>().IsChildrenMember()) {
                                builder.Append($".SelectMany(x => x.{path.RelationName})");
                            } else if (select) {
                                builder.Append($".Select(x => x.{path.RelationName})");
                            } else {
                                builder.Append($".{path.RelationName}?");
                                if (path.Terminal.As<Aggregate>().IsChildrenMember()) select = true;
                            }
                        }
                        builder.Append($".OfType<{descendantDbEntities[i].Item.EFCoreEntityClassName}>()");
                        builder.AppendLine($" ?? Enumerable.Empty<{descendantDbEntities[i].Item.EFCoreEntityClassName}>();");

                    } else {
                        // 子集約までの経路の途中に配列が含まれない場合
                        builder.AppendLine($"var {tempVar} = new {descendantDbEntities[i].Item.EFCoreEntityClassName}?[] {{");
                        builder.AppendLine($"    {(renderBefore ? "beforeDbEntity" : "afterDbEntity")}.{paths.Select(p => p.RelationName).Join("?.")},");
                        builder.AppendLine($"}}.OfType<{descendantDbEntities[i].Item.EFCoreEntityClassName}>().ToArray();");
                    }
                }
                RenderEntityArray(true);
                RenderEntityArray(false);

                // ChangeState変更
                builder.AppendLine($"foreach (var a in {after_}) {{");
                builder.AppendLine($"    var b = {before}.SingleOrDefault(b => b.{EFCoreEntity.KEYEQUALS}(a));");
                builder.AppendLine($"    if (b == null) {{");
                builder.AppendLine($"        {dbContext}.Entry(a).State = EntityState.Added;");
                builder.AppendLine($"    }} else {{");
                builder.AppendLine($"        {dbContext}.Entry(a).State = EntityState.Modified;");
                builder.AppendLine($"    }}");
                builder.AppendLine($"}}");

                builder.AppendLine($"foreach (var b in {before}) {{");
                builder.AppendLine($"    var a = {after_}.SingleOrDefault(a => a.{EFCoreEntity.KEYEQUALS}(b));");
                builder.AppendLine($"    if (a == null) {{");
                builder.AppendLine($"        {dbContext}.Entry(b).State = EntityState.Deleted;");
                builder.AppendLine($"    }}");
                builder.AppendLine($"}}");
                builder.AppendLine();
            }

            return builder.ToString();
        }

    }
}
