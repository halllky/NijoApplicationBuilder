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

            // データ型: 登録更新コマンドベース
            context.UseSummarizedFile<DataClassForSaveBase>().Register(rootAggregate);

            foreach (var agg in allAggregates) {
                // データ型: EFCore Entity
                var efCoreEntity = new EFCoreEntity(agg);
                aggregateFile.DataClassDeclaring.Add(efCoreEntity.Render(context));
                context.CoreLibrary.DbSetPropNameAndClassName.Add(efCoreEntity.DbSetName, efCoreEntity.ClassName);
                context.CoreLibrary.DbContextOnModelCreating.Add(efCoreEntity.RenderCallingOnModelCreating(context));

                // データ型: DataClassForSave
                var dataClassForSave = new DataClassForSave(agg, DataClassForSave.E_Type.UpdateOrDelete);
                aggregateFile.DataClassDeclaring.Add(dataClassForSave.RenderCSharp(context));
                aggregateFile.DataClassDeclaring.Add(dataClassForSave.RenderCSharpBeforeSaveEventArgs(context));
                aggregateFile.DataClassDeclaring.Add(dataClassForSave.RenderCSharpAfterSaveEventArgs(context));
                aggregateFile.DataClassDeclaring.Add(dataClassForSave.RenderCSharpReadOnlyStructure(context));
                context.ReactProject.Types.Add(rootAggregate, dataClassForSave.RenderTypeScript(context));
                context.ReactProject.Types.Add(rootAggregate, dataClassForSave.RenderTypeScriptErrorStructure(context));
                context.ReactProject.Types.Add(rootAggregate, dataClassForSave.RenderTypeScriptReadOnlyStructure(context));

                // データ型: DataClassForNewItem
                var dataClassForNewItem = new DataClassForSave(agg, DataClassForSave.E_Type.Create);
                aggregateFile.DataClassDeclaring.Add(dataClassForNewItem.RenderCSharp(context));
                aggregateFile.DataClassDeclaring.Add(dataClassForNewItem.RenderCSharpReadOnlyStructure(context));
                context.ReactProject.Types.Add(rootAggregate, dataClassForNewItem.RenderTypeScript(context));
                context.ReactProject.Types.Add(rootAggregate, dataClassForNewItem.RenderTypeScriptErrorStructure(context));
                context.ReactProject.Types.Add(rootAggregate, dataClassForNewItem.RenderTypeScriptReadOnlyStructure(context));

                // 処理: DataClassForSave, DataClassForNewItem 新規作成関数
                if (agg.IsRoot() || agg.IsChildrenMember()) {
                    context.ReactProject.Types.Add(dataClassForNewItem.RenderTsNewObjectFunction(context));
                    context.ReactProject.Types.Add(dataClassForSave.RenderTsNewObjectFunction(context));
                }
            }

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

            // 処理: SetReadOnly AppSrv
            var setReadOnly = new SetReadOnly(rootAggregate);
            aggregateFile.AppServiceMethods.Add(setReadOnly.Render(context));


            if (rootAggregate.Item.Options.GenerateDefaultReadModel) {
                // 既定のReadModel（WriteModelと同じ型のReadModel）を生成する
               context.GetModel<ReadModel2>().GenerateCode(context, rootAggregate);

            } else {
                // 既定のReadModelが無い場合でも他の集約から参照されるときのための部品は必要になるので生成する
                foreach (var agg in allAggregates) {
                    var asEntry = agg.AsEntry();

                    // データ型
                    var refTargetKeys = new DataClassForRefTargetKeys(asEntry, asEntry);
                    var refSearchCondition = new RefSearchCondition(asEntry, asEntry);
                    var refSearchResult = new RefDisplayData(asEntry, asEntry);
                    aggregateFile.DataClassDeclaring.Add(refTargetKeys.RenderCSharpDeclaringRecursively(context));
                    aggregateFile.DataClassDeclaring.Add(refSearchCondition.RenderCSharpDeclaringRecursively(context));
                    aggregateFile.DataClassDeclaring.Add(refSearchResult.RenderCSharp(context));
                    context.ReactProject.Types.Add(rootAggregate, refSearchCondition.RenderTypeScriptDeclaringRecursively(context));
                    context.ReactProject.Types.Add(rootAggregate, refSearchCondition.RenderCreateNewObjectFn(context));
                    context.ReactProject.Types.Add(rootAggregate, refTargetKeys.RenderTypeScriptDeclaringRecursively(context));
                    context.ReactProject.Types.Add(rootAggregate, refSearchResult.RenderTypeScript(context));

                    // UI: コンボボックス
                    // UI: 検索ダイアログ
                    // UI: インライン検索ビュー
                    var comboBox = new SearchComboBox(asEntry);
                    var searchDialog = new SearchDialog(asEntry);
                    var inlineRef = new SearchInline(asEntry);
                    context.ReactProject.AutoGeneratedInput.Add(comboBox.Render(context));
                    context.ReactProject.AutoGeneratedInput.Add(searchDialog.Render(context));
                    context.ReactProject.AutoGeneratedInput.Add(inlineRef.Render(context));

                    // 処理: 参照先検索
                    var searchRef = new RefSearchMethod(asEntry, asEntry);
                    context.ReactProject.AutoGeneratedHook.Add(searchRef.RenderHook(context));
                    aggregateFile.ControllerActions.Add(searchRef.RenderController(context));
                    aggregateFile.AppServiceMethods.Add(searchRef.RenderAppSrvMethodOfWriteModel(context));
                }
            }

            // ---------------------------------------------
            // 処理: デバッグ用ダミーデータ作成関数
            context.UseSummarizedFile<DummyDataGenerator>().Add(rootAggregate);
        }

        void IModel.GenerateCode(CodeRenderingContext context) {

            context.CoreLibrary.UtilDir(utilDir => {
                // データ型: 一括コミット コンテキスト引数
                var batchUpdateContext = new BatchUpdateContext();
                utilDir.Generate(batchUpdateContext.Render());

                // エラーデータ用インターフェース
                utilDir.Generate(DataClassForSave.RenderIErrorData());
            });
        }
    }
}
