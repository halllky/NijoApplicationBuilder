using Nijo.Core;
using Nijo.Models.RefTo;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts;
using Nijo.Parts.WebClient;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models {
    /// <summary>
    /// 登録・更新・削除される単位のデータ
    /// </summary>
    internal class WriteModel2 : IModel {
        void IModel.GenerateCode(CodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {
            var allAggregates = rootAggregate.EnumerateThisAndDescendants();
            var aggregateFile = context.CoreLibrary.UseAggregateFile(rootAggregate);
            var uiContext = context.UseSummarizedFile<UiContext>();

            // データ型: 登録更新コマンドベース
            context.UseSummarizedFile<DataClassForSaveBase>().Register(rootAggregate);

            foreach (var agg in allAggregates) {
                // データ型: EFCore Entity
                var efCoreEntity = new EFCoreEntity(agg);
                aggregateFile.DataClassDeclaring.Add(efCoreEntity.Render(context));

                var dbContext = context.UseSummarizedFile<Parts.WebServer.DbContextClass>();
                dbContext.AddDbSet(efCoreEntity.ClassName, efCoreEntity.DbSetName);
                dbContext.AddOnModelCreating(efCoreEntity.RenderCallingOnModelCreating(context));

                // データ型: DataClassForNewItem
                var dataClassForNewItem = new DataClassForSave(agg, DataClassForSave.E_Type.Create);
                aggregateFile.DataClassDeclaring.Add(dataClassForNewItem.RenderCSharp(context));
                aggregateFile.DataClassDeclaring.Add(dataClassForNewItem.RenderCSharpReadOnlyStructure(context));
                context.ReactProject.Types.Add(rootAggregate, dataClassForNewItem.RenderTypeScript(context));
                context.ReactProject.Types.Add(rootAggregate, dataClassForNewItem.RenderTypeScriptReadOnlyStructure(context));

                // データ型: DataClassForSave
                var dataClassForSave = new DataClassForSave(agg, DataClassForSave.E_Type.UpdateOrDelete);
                aggregateFile.DataClassDeclaring.Add(dataClassForSave.RenderCSharp(context));
                aggregateFile.DataClassDeclaring.Add(dataClassForSave.RenderCSharpMessageStructure(context)); // メッセージクラスはCreate/Saveで共用
                aggregateFile.DataClassDeclaring.Add(dataClassForSave.RenderCSharpReadOnlyStructure(context));
                context.ReactProject.Types.Add(rootAggregate, dataClassForSave.RenderTypeScript(context));
                context.ReactProject.Types.Add(rootAggregate, dataClassForSave.RenderTypeScriptReadOnlyStructure(context));

                // 処理: DataClassForSave, DataClassForNewItem 新規作成関数
                if (agg.IsRoot() || agg.IsChildrenMember()) {
                    context.ReactProject.Types.Add(dataClassForNewItem.RenderTsNewObjectFunction(context));
                    context.ReactProject.Types.Add(dataClassForSave.RenderTsNewObjectFunction(context));
                }

                // データ型: ほかの集約から参照されるときのキー
                var asEntry = agg.AsEntry();
                var refTargetKeys = new DataClassForRefTargetKeys(asEntry, asEntry);
                aggregateFile.DataClassDeclaring.Add(refTargetKeys.RenderCSharpDeclaringRecursively(context));
                context.ReactProject.Types.Add(rootAggregate, refTargetKeys.RenderTypeScriptDeclaringRecursively(context));
            }

            // データ型: 一括更新処理 エラーメッセージの入れ物
            context.UseSummarizedFile<SaveContext>().AddWriteModel(rootAggregate);

            // 処理: 一括更新処理
            context.UseSummarizedFile<BatchUpdateWriteModel>().Register(rootAggregate);

            // 処理: 新規作成処理 AppSrv
            // 処理: 更新処理 AppSrv
            // 処理: 削除処理 AppSrv
            var create = new CreateMethod(rootAggregate);
            var update = new UpdateMethod(rootAggregate);
            var delete = new DeleteMethod(rootAggregate);
            aggregateFile.AppServiceMethods.Add(create.Render(context));
            aggregateFile.AppServiceMethods.Add(update.Render(context));
            aggregateFile.AppServiceMethods.Add(delete.Render(context));

            // 処理: 自動生成されるバリデーションエラーチェック
            aggregateFile.AppServiceMethods.Add(RequiredCheck.Render(rootAggregate, context));
            aggregateFile.AppServiceMethods.Add(MaxLengthCheck.Render(rootAggregate, context));

            // 処理: SetReadOnly AppSrv
            var setReadOnly = new SetReadOnly(rootAggregate);
            aggregateFile.AppServiceMethods.Add(setReadOnly.Render(context));

            // ---------------------------------------------
            // WriteModelと同じ型のReadModelを生成する
            if (rootAggregate.Item.Options.GenerateDefaultReadModel) {
               context.GetModel<ReadModel2>().GenerateCode(context, rootAggregate);
            }

            // ---------------------------------------------
            // 処理: デバッグ用ダミーデータ作成関数
            context.UseSummarizedFile<DummyDataGenerator>().Add(rootAggregate);
        }

        void IModel.GenerateCode(CodeRenderingContext context) {
        }

        IEnumerable<string> IModel.ValidateAggregate(GraphNode<Aggregate> rootAggregate) {
            foreach (var agg in rootAggregate.EnumerateThisAndDescendants()) {

                // ルート集約またはChildrenはキー必須
                if (agg.IsRoot() || agg.IsChildrenMember()) {
                    var ownKeys = agg
                        .GetKeys()
                        .Where(m => m is AggregateMember.ValueMember vm && vm.DeclaringAggregate == vm.Owner
                                 || m is AggregateMember.Ref);
                    if (!ownKeys.Any()) {
                        yield return $"{agg.Item.DisplayName}にキーが1つもありません。";
                    }
                }

                // HasLifecycleはReadModel専用の属性
                if (agg.Item.Options.HasLifeCycle) {
                    yield return
                        $"{agg.Item.DisplayName}: {nameof(AggregateBuildOption.HasLifeCycle)}は{nameof(ReadModel2)}専用の属性です。" +
                        $"{nameof(WriteModel2)}でライフサイクルが分かれる部分は別のルート集約として定義し、元集約への参照をキーにすることで表現してください。";
                }

                foreach (var member in agg.GetMembers()) {

                    // WriteModelからReadModelへの参照は不可
                    if (member is AggregateMember.Ref @ref
                        && @ref.RefTo.GetRoot().Item.Options.Handler != NijoCodeGenerator.Models.WriteModel2.Key) {

                        yield return $"{agg.Item.DisplayName}.{member.MemberName}: {nameof(WriteModel2)}の参照先は{nameof(WriteModel2)}である必要があります。";
                    }
                }
            }
        }
    }
}
