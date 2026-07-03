using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.QueryModelModules;
using Nijo.Parts.Common;
using Nijo.Parts.CSharp;
using Nijo.Parts.JavaScript;
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
            // キーと ref-to のチェック
            var isView = rootAggregateElement.Attribute(BasicNodeOptions.MapToView.AttributeName) != null;

            if (isView) {
                // ビューの場合でもChildrenがある場合はキーが必要
                var hasChildOrChildren = rootAggregateElement
                    .Descendants()
                    .Any(el => el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value == SchemaParseContext.NODE_TYPE_CHILD
                            || el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value == SchemaParseContext.NODE_TYPE_CHILDREN);

                if (hasChildOrChildren) {
                    // ルートとChildrenはキー必須
                    var rootAndChildren = rootAggregateElement
                        .DescendantsAndSelf()
                        .Where(el => el.Parent?.Parent == el.Document?.Root
                                  || el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value == SchemaParseContext.NODE_TYPE_CHILDREN);
                    foreach (var el in rootAndChildren) {
                        var hasKey = el.Elements().Any(member => member.Attribute(BasicNodeOptions.IsKey.AttributeName) != null);

                        if (!hasKey) {
                            addError(el, "Child/Childrenがあるビューにマッピングされるクエリモデルにはキーが必要です。");
                        }
                    }
                }

                // キーなしのビューからのref-toを禁止
                var hasAnyKey = rootAggregateElement
                    .DescendantsAndSelf()
                    .Any(el => el.Elements().Any(member => member.Attribute(BasicNodeOptions.IsKey.AttributeName) != null));

                if (!hasAnyKey) {
                    // 自身と子孫のすべてにキーがない場合、ref-toをチェック
                    var refToMembers = rootAggregateElement
                        .Descendants()
                        .Where(member => member.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value?.StartsWith(SchemaParseContext.NODE_TYPE_REFTO + ":") == true)
                        .ToList();

                    foreach (var refToMember in refToMembers) {
                        addError(refToMember, "キーが定義されていないビューからは他の集約を参照(ref-to)できません。EF Coreの制約により、キーレスエンティティからのナビゲーションプロパティはサポートされていません。");
                    }
                }
            } else {
                // MapToViewが指定されていない通常のQueryModelの場合は子集約・子配列への主キー属性を禁止
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
            }

            // MapToViewの場合は外部参照のチェックとキーレスエンティティからのナビゲーションプロパティチェック
            if (isView) {
                // キーなしのビューからのref-toを禁止
                var hasAnyKey = rootAggregateElement
                    .DescendantsAndSelf()
                    .Any(el => el.Elements().Any(member => member.Attribute(BasicNodeOptions.IsKey.AttributeName) != null));

                if (!hasAnyKey) {
                    // 自身と子孫のすべてにキーがない場合、ref-toをチェック
                    var refToMembers = rootAggregateElement
                        .Descendants()
                        .Where(member => member.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value?.StartsWith(SchemaParseContext.NODE_TYPE_REFTO + ":") == true)
                        .ToList();

                    foreach (var refToMember in refToMembers) {
                        addError(refToMember, "キーが定義されていないビューからは他の集約を参照(ref-to)できません。EF Coreの制約により、キーレスエンティティからのナビゲーションプロパティはサポートされていません。");
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
                var rootElement = refElement.GetRootAggregateElement();
                var refToRoot = refTo.GetRootAggregateElement();

                if (rootElement == refToRoot) {
                    addError(refElement, "自身のツリーの集約を参照することはできません。");
                    continue;
                }

                // 参照先がクエリモデルまたはGDQMデータモデルか確認
                var refToType = refToRoot.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value;
                var isQueryModel = refToType == "query-model";
                var isGDQM = refToRoot.HasGenerateDefaultQueryModelAttribute();

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
                    var refToRoot = refTo.AncestorsAndSelf().Last(e => e.Parent?.Parent == e.Document?.Root);

                    // 自身のツリー内の参照はスキップ
                    var currentRoot = element.AncestorsAndSelf().Last(e => e.Parent?.Parent == e.Document?.Root);
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
            aggregateFile.AddTypeScriptFunction(searchCondition.RenderPkAssignFunction());

            // データ型: 検索条件メッセージ
            var searchConditionMessages = new SearchConditionMessageContainer(rootAggregate);
            aggregateFile.AddCSharpClass(SearchConditionMessageContainer.RenderCSharpRecursively(rootAggregate), "Class_SearchConditionMessage");
            ctx.Use<MessageContainer.BaseClass>().Register(searchConditionMessages.CsClassName, searchConditionMessages.CsClassName);

            // 処理: 検索条件クラスのURL変換
            // - URL => TS
            // - TS => URL
            var urlConversion = new SearchConditionUrlConversion(searchCondition);
            aggregateFile.AddTypeScriptFunction(urlConversion.ConvertUrlToTypeScript(ctx));
            aggregateFile.AddTypeScriptFunction(urlConversion.ConvertTypeScriptToUrl(ctx));

            // データ型: 検索結果クラス
            // ※ ビューにマッピングされる場合はSearchResultのEFCoreエンティティも生成する
            var searchResult = new SearchResult(rootAggregate);
            aggregateFile.AddCSharpClass(searchResult.RenderTree(ctx), "Class_SearchResult");
            if (rootAggregate.IsView) {
                ctx.Use<DbContextClass>().AddEntities(searchResult.EnumerateThisAndChildren());
            }

            // データ型: 画面表示用型 DisplayData
            // - 定義(CS, TS): 値 + 状態(existsInDB, willBeChanged, willBeDeleted)
            // - ディープイコール関数
            // - UIの制約定義オブジェクト（文字種、maxlength, 桁, required）
            // - TS側オブジェクト作成関数
            // - 変換処理: SearchResult => DisplayData
            // - 主キーの抽出・設定関数（URLなどのために使用）
            var displayData = new DisplayData(rootAggregate);
            aggregateFile.AddCSharpClass(DisplayData.RenderCSharpRecursively(rootAggregate, ctx), "Class_DisplayData");
            aggregateFile.AddTypeScriptTypeDef(DisplayData.RenderTypeScriptRecursively(rootAggregate, ctx));
            aggregateFile.AddTypeScriptFunction(EditablePresentationObject.RenderTsNewObjectFunctionRecursively(displayData, ctx));
            aggregateFile.AddTypeScriptFunction(new DeepEqualFunction(displayData).Render(ctx));

            // データ型: 画面表示用型メッセージ
            var displayDataMessages = new DisplayDataMessageContainer(rootAggregate);
            aggregateFile.AddCSharpClass(DisplayDataMessageContainer.RenderCSharpRecursively(rootAggregate), "Class_DisplayDataMessage");
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

            // データ型: ほかの集約から参照されるときのキー
            if (rootAggregate.IsView) {
                aggregateFile.AddCSharpClass(DataModelModules.KeyClass.KeyClassEntry.RenderClassDeclaringRecursively(rootAggregate, ctx), "Class_KeyClass");
            }

            // UI用モジュール
            // - DisplayData等のマッピングオブジェクト
            // - React Router のURL定義
            // - ナビゲーション用関数
            // など
            ctx.Use<CommandQueryMappings>()
                .AddQueryModel(rootAggregate);

            // 定数: メタデータ ※DataModelの場合は全く同じ値になるので割愛
            if (!rootAggregate.GenerateDefaultQueryModel) {
                ctx.Use<MetadataForPage>().Add(rootAggregate);
            }
        }

        public void GenerateCode(CodeRenderingContext ctx) {
            GenerateQueryModelCommonModules(ctx);
        }

        internal static void GenerateQueryModelCommonModules(CodeRenderingContext ctx) {
            ctx.Use<DeepEqualFunction.OptionType>();

            ctx.CoreLibrary(dir => {
                dir.Directory("Util", utilDir => {
                    utilDir.Generate(SearchProcessingReturn.RenderCSharp(ctx)); // 一覧検索の戻り値の型
                });
            });
            ctx.ReactProject(dir => {
                dir.Directory("util", utilDir => {
                    utilDir.Generate(SearchCondition.Entry.RenderTsBaseType()); // 検索条件の基底型
                    utilDir.Generate(SearchProcessingReturn.RenderTypeScript()); // 一覧検索の戻り値の型
                });
            });
        }
    }
}
