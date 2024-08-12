using Nijo.Core;
using Nijo.Models.ReadModel2Features;
using Nijo.Models.RefTo;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts.Utility;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models {
    /// <summary>
    /// 画面表示されるデータ型
    /// </summary>
    internal class ReadModel2 : IModel {
        void IModel.GenerateCode(CodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {
            var allAggregates = rootAggregate.EnumerateThisAndDescendants();
            var aggregateFile = context.CoreLibrary.UseAggregateFile(rootAggregate);

            // データ型: 検索条件クラス
            var condition = new SearchCondition(rootAggregate);
            aggregateFile.DataClassDeclaring.Add(condition.RenderCSharpDeclaringRecursively(context));
            context.ReactProject.Types.Add(rootAggregate, condition.RenderTypeScriptDeclaringRecursively(context));
            context.ReactProject.Types.Add(rootAggregate, condition.RenderCreateNewObjectFn(context));

            foreach (var agg in allAggregates) {
                // データ型: 検索結果クラス
                var searchResult = new SearchResult(agg);
                aggregateFile.DataClassDeclaring.Add(searchResult.RenderCSharpDeclaring(context));

                // データ型: ビュークラス
                var displayData = new DataClassForDisplay(agg);
                aggregateFile.DataClassDeclaring.Add(displayData.RenderCSharpDeclaring(context));
                context.ReactProject.Types.Add(agg, displayData.RenderTypeScriptDeclaring(context));
                context.ReactProject.Types.Add(agg, displayData.RenderTsNewObjectFunction(context));
            }

            // 処理: 検索処理
            var load = new LoadMethod(rootAggregate);
            context.ReactProject.AutoGeneratedHook.Add(load.RenderReactHook(context));
            aggregateFile.ControllerActions.Add(load.RenderControllerAction(context));
            aggregateFile.AppServiceMethods.Add(load.RenderAppSrvBaseMethod(context));
            aggregateFile.AppServiceMethods.Add(load.RenderAppSrvAbstractMethod(context));

            // 処理: 一括更新処理
            context.UseSummarizedFile<BatchUpdateReadModel>().Register(rootAggregate);

            // UI: MultiView
            var multiView = new MultiView(rootAggregate);
            context.ReactProject.Pages.Add(multiView);
            context.ReactProject.AutoGeneratedHook.Add(multiView.RenderNavigationHook(context));

            // UI: SingleView
            var createView = new SingleView(rootAggregate, SingleView.E_Type.New);
            var readOnlyView = new SingleView(rootAggregate, SingleView.E_Type.ReadOnly);
            var editView = new SingleView(rootAggregate, SingleView.E_Type.Edit);
            context.ReactProject.Pages.Add(createView);
            context.ReactProject.Pages.Add(readOnlyView);
            context.ReactProject.Pages.Add(editView);

            // UI: ナビゲーション用関数
            context.ReactProject.UrlUtil.Add(createView.RenderNavigateFn(context));
            context.ReactProject.UrlUtil.Add(readOnlyView.RenderNavigateFn(context)); // readonly, edit は関数共用

            // ---------------------------------------------
            // 他の集約から参照されるときのための部品

            foreach (var agg in allAggregates) {
                var asEntry = agg.AsEntry();

                // データ型
                var refTargetKeys = new DataClassForRefTargetKeys(asEntry, asEntry);
                var refSearchCondition = new RefSearchCondition(asEntry, asEntry);
                var refSearchResult = new RefSearchResult(asEntry, asEntry);
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
                aggregateFile.AppServiceMethods.Add(searchRef.RenderAppSrvMethodOfReadModel(context));
            }
        }

        void IModel.GenerateCode(CodeRenderingContext context) {

            // ユーティリティクラス等
            context.CoreLibrary.UtilDir(dir => {
                dir.Generate(DataClassForDisplay.RenderBaseClass());
                dir.Generate(MessageContainer.RenderCSharp());
                dir.Generate(ReadOnlyInfo.RenderCSharp());
                dir.Generate(InstanceKey.RenderCSharp());
                dir.Generate(ISaveCommandConvertible.Render());
            });
        }
    }
}
