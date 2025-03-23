using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models.QueryModelModules;
using Nijo.Ver1.Parts.Common;
using Nijo.Ver1.Parts.JavaScript;
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
            var searchCondition = new SearchCondition(rootAggregate);
            aggregateFile.AddCSharpClass(searchCondition.RenderCSharp(ctx));
            aggregateFile.AddTypeScriptSource(searchCondition.RenderTypeScript(ctx));

            // データ型: 検索条件メッセージ
            var searchConditionMessages = new MessageContainer(rootAggregate);
            aggregateFile.AddCSharpClass(searchConditionMessages.RenderCSharp());
            aggregateFile.AddTypeScriptSource(searchConditionMessages.RenderTypeScript());

            // 処理: 検索条件クラスのURL変換
            // - URL => TS
            // - TS => URL
            var urlConversion = new SearchConditionUrlConversion(searchCondition);
            aggregateFile.AddTypeScriptSource(urlConversion.ConvertUrlToTypeScript(ctx));
            aggregateFile.AddTypeScriptSource(urlConversion.ConvertTypeScriptToUrl(ctx));

            // データ型: 検索結果クラス
            var searchResult = new SearchResult(rootAggregate);
            aggregateFile.AddCSharpClass(searchResult.RenderCSharpDeclaring());

            // データ型: 画面表示用型 DisplayData
            // - 定義(CS, TS): 値 + 状態(existsInDB, willBeChanged, willBeDeleted) + ReadOnly(画面の自動生成の一機能と位置づけるべきかも)
            // - ディープイコール関数
            // - UIの制約定義オブジェクト（文字種、maxlength, 桁, required）
            // - TS側オブジェクト作成関数
            // - 変換処理: SearchResult => DisplayData
            var displayData = new DisplayData(rootAggregate);
            aggregateFile.AddCSharpClass(displayData.RenderCSharpDeclaring(ctx));
            aggregateFile.AddCSharpClass(displayData.ConvertSearchResultToDisplayData(ctx));
            aggregateFile.AddTypeScriptSource(displayData.RenderTypeScriptType(ctx));
            aggregateFile.AddTypeScriptSource(displayData.RenderUiConstraintType(ctx));
            aggregateFile.AddTypeScriptSource(displayData.RenderUiConstraintValue(ctx));
            aggregateFile.AddTypeScriptSource(displayData.RenderTypeScriptObjectCreationFunction(ctx));

            var deepEquals = new DeepEqual(rootAggregate);
            aggregateFile.AddCSharpClass(deepEquals.RenderTypeScript());

            // 検索処理
            // - reactは型名マッピングのみ
            // - ASP.NET Core Controller Action
            // - AppSrv
            //   - CreateQuerySource
            //   - AppendWhereClause
            //   - Sort
            //   - Paging
            // - 以上がload, count それぞれ
            var searchProcessing = new SearchProcessing(rootAggregate);
            aggregateFile.AddWebapiControllerAction(searchProcessing.RenderAspNetCoreControllerAction(ctx));
            aggregateFile.AddCSharpClass(searchProcessing.RenderAppSrvMethods(ctx));

            // RefToモジュール
            // - データ型
            //   - RefDisplayData
            // - TS側オブジェクト作成関数
            // - 検索処理
            //   - Reactは型マッピングのみ
            //   - ASP.NET Core Controller Action
            //   - ApplicationService
            var refDisplayData = new DisplayDataRefEntry(rootAggregate);
            var searchRefs = new SearchProcessingRefs(rootAggregate);
            aggregateFile.AddCSharpClass(refDisplayData.RenderCsClass(ctx));
            aggregateFile.AddTypeScriptSource(refDisplayData.RenderTsType(ctx));
            aggregateFile.AddTypeScriptSource(refDisplayData.RenderTypeScriptObjectCreationFunction(ctx));
            aggregateFile.AddCSharpClass(searchRefs.RenderAppSrvMethod(ctx));
            aggregateFile.AddCSharpClass(searchRefs.RenderAspNetCoreControllerAction(ctx));

            // UI用モジュール
            // - DisplayData等のマッピングオブジェクト
            // - React Router のURL定義
            // - ナビゲーション用関数
            // など
            ctx.Use<MappingsForCustomize>()
                .AddQueryModel(rootAggregate);

            // 権限
            // TODO ver.1

            aggregateFile.ExecuteRendering(ctx);
        }

        public void GenerateCode(CodeRenderingContext ctx) {
            // メッセージコンテナ
        }
    }
}
