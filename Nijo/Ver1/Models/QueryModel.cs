using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models.QueryModelModules;
using Nijo.Ver1.Parts.Common;
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
            GenerateCode(ctx, rootAggregate, aggregateFile);
            aggregateFile.ExecuteRendering(ctx);
        }

        internal static void GenerateCode(CodeRenderingContext ctx, RootAggregate rootAggregate, SourceFileByAggregate aggregateFile) {
            // データ型: 検索条件クラス
            // - CS
            // - TS
            //   - export type 検索条件型
            //   - export type ソート可能メンバー型
            // - TS側オブジェクト作成関数
            var searchCondition = new SearchCondition(rootAggregate);
            aggregateFile.AddCSharpClass(searchCondition.RenderCSharp(ctx));
            aggregateFile.AddTypeScriptTypeDef(searchCondition.RenderTypeScript(ctx));
            aggregateFile.AddTypeScriptTypeDef(searchCondition.RenderTypeScriptSortableMemberType());
            aggregateFile.AddTypeScriptFunction(searchCondition.RenderNewObjectFunction());

            // データ型: 検索条件メッセージ
            var searchConditionMessages = new SearchConditionMessageContainer(rootAggregate);
            aggregateFile.AddCSharpClass(SearchConditionMessageContainer.RenderCSharpRecursively(rootAggregate));
            aggregateFile.AddTypeScriptTypeDef(searchConditionMessages.RenderTypeScript()); // ちなみに子孫集約はルート集約の中にレンダリングされる

            // 処理: 検索条件クラスのURL変換
            // - URL => TS
            // - TS => URL
            var urlConversion = new SearchConditionUrlConversion(searchCondition);
            aggregateFile.AddTypeScriptFunction(urlConversion.ConvertUrlToTypeScript(ctx));
            aggregateFile.AddTypeScriptFunction(urlConversion.ConvertTypeScriptToUrl(ctx));

            // データ型: 検索結果クラス
            aggregateFile.AddCSharpClass(SearchResult.RenderTree(rootAggregate));

            // データ型: 画面表示用型 DisplayData
            // - 定義(CS, TS): 値 + 状態(existsInDB, willBeChanged, willBeDeleted) + ReadOnly(画面の自動生成の一機能と位置づけるべきかも)
            // - ディープイコール関数
            // - UIの制約定義オブジェクト（文字種、maxlength, 桁, required）
            // - TS側オブジェクト作成関数
            // - 変換処理: SearchResult => DisplayData
            var displayData = new DisplayData(rootAggregate);
            aggregateFile.AddCSharpClass(displayData.RenderCSharpDeclaring(ctx));
            aggregateFile.AddTypeScriptTypeDef(displayData.RenderTypeScriptType(ctx));
            aggregateFile.AddTypeScriptTypeDef(displayData.RenderUiConstraintType(ctx));
            aggregateFile.AddTypeScriptTypeDef(displayData.RenderUiConstraintValue(ctx));
            aggregateFile.AddTypeScriptFunction(displayData.RenderTypeScriptObjectCreationFunction(ctx));

            var deepEquals = new DeepEqual(rootAggregate);
            aggregateFile.AddTypeScriptFunction(deepEquals.RenderTypeScript());

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
            aggregateFile.AddAppSrvMethod(searchProcessing.RenderAppSrvMethods(ctx));

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
            aggregateFile.AddTypeScriptTypeDef(refDisplayData.RenderTsType(ctx));
            aggregateFile.AddTypeScriptFunction(refDisplayData.RenderTypeScriptObjectCreationFunction(ctx));
            aggregateFile.AddAppSrvMethod(searchRefs.RenderAppSrvMethod(ctx));
            aggregateFile.AddWebapiControllerAction(searchRefs.RenderAspNetCoreControllerAction(ctx));

            // UI用モジュール
            // - DisplayData等のマッピングオブジェクト
            // - React Router のURL定義
            // - ナビゲーション用関数
            // など
            ctx.Use<CommandQueryMappings>()
                .AddQueryModel(rootAggregate);

            // 権限
            // TODO ver.1
        }

        public void GenerateCode(CodeRenderingContext ctx) {
            // 一覧検索の戻り値の型
            ctx.CoreLibrary(dir => {
                dir.Directory("Util", utilDir => {
                    utilDir.Generate(SearchProcessingReturn.RenderCSharp(ctx));
                });
            });
            ctx.ReactProject(dir => {
                dir.Directory("util", utilDir => {
                    utilDir.Generate(SearchProcessingReturn.RenderTypeScript());
                });
            });
        }
    }
}
