using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models.QueryModelModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models {
    /// <summary>
    /// クエリモデル。
    /// <see cref="DataModel"/> を変換して人間や外部システムが閲覧するための形に直した情報の形。
    /// </summary>
    internal class QueryModel : IModel {
        public void GenerateCode(CodeRenderingContext ctx, RootAggregate rootAggregate) {
            var aggregateFile = new SourceFileByAggregate(rootAggregate);

            // データ型: 検索条件クラス
            // - CS
            // - TS
            //   - export type 検索条件型
            //   - export type ソート可能メンバー型
            // - TS側オブジェクト作成関数
            var searchCondition = new SearchCondition();
            aggregateFile.AddCSharpClass(searchCondition.RenderCSharp(ctx));
            aggregateFile.AddTypeScriptSource(searchCondition.RenderTypeScript(ctx));

            // 処理: URL変換
            // - URL => TS
            // - TS => URL
            var urlConversion = new UrlConversion();
            aggregateFile.AddTypeScriptSource(urlConversion.ConvertUrlToTypeScript(ctx));
            aggregateFile.AddTypeScriptSource(urlConversion.ConvertTypeScriptToUrl(ctx));

            // データ型: 画面表示用型 DisplayData
            // - 定義(CS, TS): 値 + 状態(existsInDB, willBeChanged, willBeDeleted) + ReadOnly(画面の自動生成の一機能と位置づけるべきかも)
            // - ディープイコール関数
            // - UIの制約定義オブジェクト（文字種、maxlength, 桁, required）
            // - TS側オブジェクト作成関数
            // - 変換処理: SearchResult => DisplayData
            var displayData = new DisplayData(rootAggregate);
            aggregateFile.AddCSharpClass(displayData.RenderCSharpDeclaring(ctx));
            aggregateFile.AddTypeScriptSource(displayData.RenderTypeScriptType(ctx));
            aggregateFile.AddCSharpClass(displayData.RenderDeepEqualsFunction(ctx));
            aggregateFile.AddCSharpClass(displayData.RenderUiConstraints(ctx));
            aggregateFile.AddTypeScriptSource(displayData.RenderTypeScriptObjectCreationFunction(ctx));
            aggregateFile.AddCSharpClass(displayData.ConvertSearchResultToDisplayData(ctx));

            // 検索処理
            // - reactフック
            //   - load
            //   - loadSingle
            // - ASP.NET Core Controller Action
            // - AppSrv
            //   - CreateQuerySource
            //   - AppendWhereClause
            //   - Sort
            //   - Paging
            // - 以上がload, count それぞれ
            var searchProcessing = new SearchProcessing();
            aggregateFile.AddTypeScriptSource(searchProcessing.RenderReactHookLoad(ctx));
            aggregateFile.AddTypeScriptSource(searchProcessing.RenderReactHookLoadSingle(ctx));
            aggregateFile.AddCSharpClass(searchProcessing.RenderAspNetCoreControllerAction(ctx));
            aggregateFile.AddCSharpClass(searchProcessing.RenderAppSrvCreateQuerySource(ctx));
            aggregateFile.AddCSharpClass(searchProcessing.RenderAppSrvAppendWhereClause(ctx));
            aggregateFile.AddCSharpClass(searchProcessing.RenderAppSrvSort(ctx));
            aggregateFile.AddCSharpClass(searchProcessing.RenderAppSrvPaging(ctx));

            // 一括更新コマンド（画面の自動生成の一機能と位置づけるべきかも）
            // - Reactフック, ASP.NET Core Action
            // - abstractメソッド
            var batchUpdateCommand = new BatchUpdateCommand();
            aggregateFile.AddTypeScriptSource(batchUpdateCommand.RenderReactHook(ctx));
            aggregateFile.AddCSharpClass(batchUpdateCommand.RenderAspNetCoreAction(ctx));
            aggregateFile.AddCSharpClass(batchUpdateCommand.RenderAbstractMethod(ctx));

            // UI用モジュール
            // - DisplayData等のマッピングオブジェクト
            // - React Router のURL定義
            // - ナビゲーション用関数
            var uiModule = new UiModule();
            aggregateFile.AddCSharpClass(uiModule.RenderMappingObject(ctx));
            aggregateFile.AddTypeScriptSource(uiModule.RenderReactRouterUrlDefinition(ctx));
            aggregateFile.AddCSharpClass(uiModule.RenderNavigationFunction(ctx));

            // RefToモジュール
            // - データ型
            //   - RefDisplayData
            // - TS側オブジェクト作成関数
            // - 検索処理
            //   - Reactフック
            //   - ASP.NET Core Controller Action
            var refDisplayData = new DisplayDataRefEntry(rootAggregate);
            var searchRefs = new SearchProcessingRefs(rootAggregate);
            aggregateFile.AddCSharpClass(refDisplayData.RenderCsClass(ctx));
            aggregateFile.AddTypeScriptSource(refDisplayData.RenderTsType(ctx));
            aggregateFile.AddTypeScriptSource(refDisplayData.RenderTypeScriptObjectCreationFunction(ctx));
            aggregateFile.AddCSharpClass(searchRefs.RenderAppSrvMethod(ctx));
            aggregateFile.AddTypeScriptSource(searchRefs.RenderReactHook(ctx));
            aggregateFile.AddCSharpClass(searchRefs.RenderAspNetCoreControllerAction(ctx));

            // 権限
            // TODO ver.1

            aggregateFile.ExecuteRendering(ctx);
        }

        public void GenerateCode(CodeRenderingContext ctx) {
            // メッセージコンテナ
        }
    }
}
