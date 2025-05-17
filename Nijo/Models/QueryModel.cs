using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.QueryModelModules;
using Nijo.Parts.Common;
using Nijo.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Models {
    /// <summary>
    /// クエリモデル。
    /// <see cref="DataModel"/> を変換して人間や外部システムが閲覧するための形に直した情報の形。
    /// </summary>
    internal class QueryModel : IModel {
        internal const string NODE_TYPE = "query-model";
        public string SchemaName => NODE_TYPE;

        public void Validate(XElement rootAggregateElement, SchemaParseContext context, Action<XElement, string> addError) {
            // ルート集約はURLにかかわるのでキー必須
            if (rootAggregateElement.Elements().All(member => member.Attribute(BasicNodeOptions.IsKey.AttributeName) == null)) {
                addError(rootAggregateElement, "キーが指定されていません。");
            }

            // 子集約には主キー属性を付与できない
            var childAggregates = rootAggregateElement.Descendants()
                .Where(el => el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value == SchemaParseContext.NODE_TYPE_CHILD);
            foreach (var childAggregate in childAggregates) {
                var membersWithKey = childAggregate.Elements()
                    .Where(member => member.Attribute(BasicNodeOptions.IsKey.AttributeName) != null).ToList();
                if (membersWithKey.Any()) {
                    addError(childAggregate, "クエリモデルの子集約には主キー属性を付与することができません。");
                    foreach (var member in membersWithKey) {
                        addError(member, "この子集約のメンバーに主キー属性を付与することはできません。");
                    }
                }
            }

            // 子配列には主キー属性を付与できない
            var childrenAggregates = rootAggregateElement.Descendants()
                .Where(el => el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value == SchemaParseContext.NODE_TYPE_CHILDREN);
            foreach (var childrenAggregate in childrenAggregates) {
                var membersWithKey = childrenAggregate.Elements()
                    .Where(member => member.Attribute(BasicNodeOptions.IsKey.AttributeName) != null).ToList();
                if (membersWithKey.Any()) {
                    addError(childrenAggregate, "クエリモデルの子配列には主キー属性を付与することができません。");
                    foreach (var member in membersWithKey) {
                        addError(member, "この子配列のメンバーに主キー属性を付与することはできません。");
                    }
                }
            }

            // 外部参照のチェック
            ValidateRefTo(rootAggregateElement, context, addError);
        }

        /// <summary>
        /// クエリモデルの外部参照をチェックします
        /// </summary>
        private void ValidateRefTo(XElement rootAggregateElement, SchemaParseContext context, Action<XElement, string> addError) {
            // 自身を起点とするすべての外部参照を取得
            var refElements = rootAggregateElement
                .Descendants()
                .Where(el => el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value?.StartsWith("ref-to:") == true)
                .ToList();

            if (!refElements.Any()) return;

            foreach (var refElement in refElements) {
                var refTo = context.FindRefTo(refElement);
                if (refTo == null) {
                    addError(refElement, $"参照先の要素が見つかりません: {refElement.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value}");
                    continue;
                }

                // 自身のツリーの集約を参照していないかチェック
                var rootElement = refElement.AncestorsAndSelf().Last(e => e.Parent == e.Document?.Root);
                var refToRoot = refTo.AncestorsAndSelf().Last(e => e.Parent == e.Document?.Root);

                if (rootElement == refToRoot) {
                    addError(refElement, "自身のツリーの集約を参照することはできません。");
                    continue;
                }

                // 参照先がクエリモデルまたはGDQMデータモデルか確認
                var refToType = refToRoot.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value;
                var isQueryModel = refToType == "query-model";
                var isGDQM = context.HasGenerateDefaultQueryModelAttribute(refToRoot);

                if (!isQueryModel && !isGDQM) {
                    addError(refElement, "クエリモデルの集約からは、クエリモデルまたはGenerateDefaultQueryModel属性が付与されたデータモデルの集約しか参照できません。");
                }
            }

            // 循環参照チェック
            if (HasCircularReferences(rootAggregateElement, context)) {
                addError(rootAggregateElement, "クエリモデルで循環参照を定義することはできません。");
            }
        }

        /// <summary>
        /// クエリモデルの循環参照をチェックします
        /// </summary>
        private bool HasCircularReferences(XElement rootAggregateElement, SchemaParseContext context) {
            var visited = new HashSet<XElement>();
            var recStack = new HashSet<XElement>();

            // DFSで循環参照を検出
            bool HasCircular(XElement element) {
                if (recStack.Contains(element)) {
                    return true; // 循環参照発見
                }

                if (visited.Contains(element)) {
                    return false; // 既に訪問済みで循環なし
                }

                visited.Add(element);
                recStack.Add(element);

                // この要素からの参照をチェック
                var refElements = element.Descendants()
                    .Where(el => el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value?.StartsWith(SchemaParseContext.NODE_TYPE_REFTO + ":") == true);

                foreach (var refElement in refElements) {
                    var refTo = context.FindRefTo(refElement);
                    if (refTo == null) continue;

                    // 参照先のルート要素
                    var refToRoot = refTo.AncestorsAndSelf().Last(e => e.Parent == e.Document?.Root);

                    // 自身のツリー内の参照はスキップ
                    var currentRoot = element.AncestorsAndSelf().Last(e => e.Parent == e.Document?.Root);
                    if (refToRoot == currentRoot) continue;

                    if (HasCircular(refToRoot)) {
                        return true;
                    }
                }

                recStack.Remove(element);
                return false;
            }

            return HasCircular(rootAggregateElement);
        }

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
            var searchCondition = new SearchCondition.Entry(rootAggregate);
            aggregateFile.AddCSharpClass(SearchCondition.Entry.RenderCSharpRecursively(rootAggregate, ctx), "Class_SearchCondition");
            aggregateFile.AddTypeScriptTypeDef(SearchCondition.Entry.RenderTypeScriptRecursively(rootAggregate, ctx));
            aggregateFile.AddTypeScriptTypeDef(searchCondition.RenderTypeScriptSortableMemberType());
            aggregateFile.AddTypeScriptFunction(searchCondition.RenderNewObjectFunction());

            // データ型: 検索条件メッセージ
            var searchConditionMessages = new SearchConditionMessageContainer(rootAggregate);
            aggregateFile.AddCSharpClass(SearchConditionMessageContainer.RenderCSharpRecursively(rootAggregate), "Class_SearchConditionMessage");
            aggregateFile.AddTypeScriptTypeDef(searchConditionMessages.RenderTypeScript()); // ちなみに子孫集約はルート集約の中にレンダリングされる
            ctx.Use<MessageContainer.BaseClass>().Register(searchConditionMessages.CsClassName, searchConditionMessages.CsClassName);

            // 処理: 検索条件クラスのURL変換
            // - URL => TS
            // - TS => URL
            var urlConversion = new SearchConditionUrlConversion(searchCondition);
            aggregateFile.AddTypeScriptFunction(urlConversion.ConvertUrlToTypeScript(ctx));
            aggregateFile.AddTypeScriptFunction(urlConversion.ConvertTypeScriptToUrl(ctx));

            // データ型: 検索結果クラス
            aggregateFile.AddCSharpClass(SearchResult.RenderTree(rootAggregate), "Class_SearchResult");

            // データ型: 画面表示用型 DisplayData
            // - 定義(CS, TS): 値 + 状態(existsInDB, willBeChanged, willBeDeleted) + ReadOnly(画面の自動生成の一機能と位置づけるべきかも)
            // - ディープイコール関数
            // - UIの制約定義オブジェクト（文字種、maxlength, 桁, required）
            // - TS側オブジェクト作成関数
            // - 変換処理: SearchResult => DisplayData
            // - 主キーの抽出・設定関数（URLなどのために使用）
            var displayData = new DisplayData(rootAggregate);
            aggregateFile.AddCSharpClass(DisplayData.RenderCSharpRecursively(rootAggregate, ctx), "Class_DisplayData");
            aggregateFile.AddTypeScriptTypeDef(DisplayData.RenderTypeScriptRecursively(rootAggregate, ctx));
            aggregateFile.AddTypeScriptTypeDef(displayData.RenderUiConstraintType(ctx));
            aggregateFile.AddTypeScriptTypeDef(displayData.RenderUiConstraintValue(ctx));
            aggregateFile.AddTypeScriptFunction(DisplayData.RenderTsNewObjectFunctionRecursively(rootAggregate, ctx));
            aggregateFile.AddTypeScriptFunction(displayData.RenderExtractPrimaryKey());
            aggregateFile.AddTypeScriptFunction(displayData.RenderAssignPrimaryKey());

            var deepEquals = new DeepEqual(rootAggregate);
            aggregateFile.AddTypeScriptFunction(deepEquals.RenderTypeScript());

            // データ型: 画面表示用型メッセージ
            var displayDataMessages = new DisplayDataMessageContainer(rootAggregate);
            aggregateFile.AddCSharpClass(DisplayDataMessageContainer.RenderCSharpRecursively(rootAggregate), "Class_DisplayDataMessage");
            aggregateFile.AddTypeScriptTypeDef(displayDataMessages.RenderTypeScript()); // ちなみに子孫集約はルート集約の中にレンダリングされる
            ctx.Use<MessageContainer.BaseClass>().Register(displayDataMessages.CsClassName, displayDataMessages.CsClassName);

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
            aggregateFile.AddAppSrvMethod(searchProcessing.RenderAppSrvMethods(ctx), "検索処理");

            // RefToモジュール
            // - データ型
            //   - RefDisplayData
            // - TS側オブジェクト作成関数
            // - 検索処理
            //   - Reactは型マッピングのみ
            //   - ASP.NET Core Controller Action
            //   - ApplicationService
            aggregateFile.AddCSharpClass(DisplayDataRef.RenderCSharpRecursively(rootAggregate, ctx), "Class_DisplayDataRef");
            aggregateFile.AddTypeScriptTypeDef(DisplayDataRef.RenderTypeScriptRecursively(rootAggregate, ctx));
            aggregateFile.AddTypeScriptFunction(DisplayDataRef.RenderTypeScriptFunctionsRecursively(rootAggregate, ctx));
            aggregateFile.AddAppSrvMethod(SearchProcessingRefs.RenderAppSrvMethodRecursively(rootAggregate, ctx), "参照検索処理");
            aggregateFile.AddWebapiControllerAction(SearchProcessingRefs.RenderAspNetCoreControllerActionRecursively(rootAggregate, ctx));

            // UI用モジュール
            // - DisplayData等のマッピングオブジェクト
            // - React Router のURL定義
            // - ナビゲーション用関数
            // など
            ctx.Use<CommandQueryMappings>()
                .AddQueryModel(rootAggregate);

            // 定数: メタデータ ※DataModelの場合は全く同じ値になるので割愛
            if (!rootAggregate.GenerateDefaultQueryModel) {
                ctx.Use<Metadata>().Add(rootAggregate);
            }

            // 定数: メタデータ（画面用リフレクション）
            ctx.Use<MetadataForPage>().Add(rootAggregate);

            // ユニットテスト
            ctx.Use<QueryModelUnitTest>().Add(rootAggregate);
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
