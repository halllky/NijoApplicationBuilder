using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Parts.Common;
using Nijo.SchemaParsing;
using Nijo.Models.CommandModelModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Models {
    internal class CommandModel : IModel {
        public string SchemaName => "command-model";

        public void Validate(XElement rootAggregateElement, SchemaParseContext context, Action<XElement, string> addError) {
            // コマンドモデルの物理名を取得
            var rootAggregateName = context.GetPhysicalName(rootAggregateElement);
            if (string.IsNullOrEmpty(rootAggregateName)) {
                addError(rootAggregateElement, "コマンドモデルの物理名が指定されていません。");
                return;
            }

            // 引数の物理名チェック
            var expectedParameterName = $"{rootAggregateName}{CommandModelExtensions.PARAMETER_PHYSICAL_NAME}";
            var correctParameterElement = rootAggregateElement
                .Elements()
                .FirstOrDefault(e => context.GetPhysicalName(e) == expectedParameterName);
            if (correctParameterElement == null) {
                addError(rootAggregateElement, $"引数の物理名「{expectedParameterName}」を持つ子集約が見つかりません。");
            } else if (context.GetNodeType(correctParameterElement) != E_NodeType.ChildAggregate) {
                addError(correctParameterElement, $"{SchemaParseContext.NODE_TYPE_CHILD} 型である必要があります。");
            }

            // 戻り値の物理名チェック
            var expectedReturnValueName = $"{rootAggregateName}{CommandModelExtensions.RETURN_VALUE_PHYSICAL_NAME}";
            var correctReturnValueElement = rootAggregateElement
                .Elements()
                .FirstOrDefault(e => context.GetPhysicalName(e) == expectedReturnValueName);
            if (correctReturnValueElement == null) {
                addError(rootAggregateElement, $"戻り値の物理名「{expectedReturnValueName}」を持つ子集約が見つかりません。");
            } else if (context.GetNodeType(correctReturnValueElement) != E_NodeType.ChildAggregate) {
                addError(correctReturnValueElement, $"{SchemaParseContext.NODE_TYPE_CHILD} 型である必要があります。");
            }

            // コマンドモデルの集約には主キー属性を定義できない
            ValidateNoKeyAttributes(rootAggregateElement, context, addError);

            // 外部参照のチェック
            ValidateRefTo(rootAggregateElement, context, addError);
        }

        /// <summary>
        /// コマンドモデルでは主キー属性を定義できないことをチェックします
        /// </summary>
        private void ValidateNoKeyAttributes(XElement rootAggregateElement, SchemaParseContext context, Action<XElement, string> addError) {
            // すべての集約（ルート集約、子集約、子配列）をチェック
            var allAggregates = rootAggregateElement.DescendantsAndSelf()
                .Where(el => el == rootAggregateElement ||
                            el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value == SchemaParseContext.NODE_TYPE_CHILD ||
                            el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value == SchemaParseContext.NODE_TYPE_CHILDREN);

            foreach (var aggregate in allAggregates) {
                var membersWithKey = aggregate.Elements()
                    .Where(member => member.Attribute(BasicNodeOptions.IsKey.AttributeName) != null).ToList();

                if (membersWithKey.Any()) {
                    addError(aggregate, "コマンドモデルの集約には主キー属性を定義できません。");
                    foreach (var member in membersWithKey) {
                        addError(member, "コマンドモデルのメンバーに主キー属性を定義することはできません。");
                    }
                }
            }
        }

        /// <summary>
        /// コマンドモデルの外部参照をチェックします
        /// </summary>
        private void ValidateRefTo(XElement rootAggregateElement, SchemaParseContext context, Action<XElement, string> addError) {
            // 自身を起点とするすべての外部参照を取得
            var refElements = rootAggregateElement
                .Descendants()
                .Where(el => el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value?.StartsWith(SchemaParseContext.NODE_TYPE_REFTO + ":") == true)
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
                var isQueryModel = refToType == QueryModel.NODE_TYPE;
                var isGDQM = context.HasGenerateDefaultQueryModelAttribute(refToRoot);

                if (!isQueryModel && !isGDQM) {
                    addError(refElement, $"コマンドモデルの集約からは、クエリモデルまたは{BasicNodeOptions.GenerateDefaultQueryModel.AttributeName}属性が付与されたデータモデルの集約しか参照できません。");
                }

                // RefToObjectの指定があるかどうか
                if (refElement.Attribute(BasicNodeOptions.RefToObject.AttributeName) == null) {
                    addError(refElement, $"コマンドモデルからクエリモデルを外部参照する場合、{BasicNodeOptions.RefToObject.AttributeName}属性を指定する必要があります。");
                } else {
                    var refToObject = refElement.Attribute(BasicNodeOptions.RefToObject.AttributeName)?.Value;
                    if (refToObject != BasicNodeOptions.REF_TO_OBJECT_DISPLAY_DATA && refToObject != BasicNodeOptions.REF_TO_OBJECT_SEARCH_CONDITION) {
                        addError(refElement, $"{BasicNodeOptions.RefToObject.AttributeName}属性の値は「{BasicNodeOptions.REF_TO_OBJECT_DISPLAY_DATA}」または「{BasicNodeOptions.REF_TO_OBJECT_SEARCH_CONDITION}」である必要があります。");
                    }
                }
            }
        }

        public void GenerateCode(CodeRenderingContext ctx, RootAggregate rootAggregate) {
            var aggregateFile = new SourceFileByAggregate(rootAggregate);

            // データ型: パラメータ型定義
            var parameterType = new ParameterType(rootAggregate);
            aggregateFile.AddCSharpClass(parameterType.RenderCSharp(ctx), "Class_Parameter");
            aggregateFile.AddTypeScriptTypeDef(parameterType.RenderTypeScript(ctx));
            aggregateFile.AddTypeScriptFunction(parameterType.RenderNewObjectFn());

            // データ型: パラメータ型メッセージ
            var parameterMessages = new ParameterTypeMessageContainer(rootAggregate);
            aggregateFile.AddCSharpClass(parameterMessages.RenderCSharp(), "Class_ParameterMessage");
            aggregateFile.AddTypeScriptTypeDef(parameterMessages.RenderTypeScript());
            ctx.Use<MessageContainer.BaseClass>().Register(parameterMessages.CsClassName, parameterMessages.CsClassName);

            // データ型: 戻り値型定義
            var returnType = new ReturnValue(rootAggregate);
            aggregateFile.AddCSharpClass(returnType.RenderCSharp(ctx), "Class_ReturnValue");
            aggregateFile.AddTypeScriptTypeDef(returnType.RenderTypeScript(ctx));

            // 処理: TypeScript用マッピング、Webエンドポイント、本処理抽象メソッド
            var commandProcessing = new CommandProcessing(rootAggregate);
            aggregateFile.AddWebapiControllerAction(commandProcessing.RenderAspNetCoreControllerAction(ctx));
            aggregateFile.AddAppSrvMethod(commandProcessing.RenderAppSrvMethods(ctx), "コマンド処理");

            // カスタムロジック用モジュール
            ctx.Use<CommandQueryMappings>().AddCommandModel(rootAggregate);

            // 定数: メタデータ
            ctx.Use<Metadata>()
                .Add(rootAggregate.GetCommandModelParameterChild())
                .Add(rootAggregate.GetCommandModelReturnValueChild());

            aggregateFile.ExecuteRendering(ctx);
        }

        public void GenerateCode(CodeRenderingContext ctx) {
            // 特になし
        }
    }


    internal static class CommandModelExtensions {
        // ルート集約の直下にあり、物理名がこれらである要素は特別な意味を持つ
        internal const string PARAMETER_PHYSICAL_NAME = "Parameter";
        internal const string RETURN_VALUE_PHYSICAL_NAME = "ReturnValue";

        /// <summary>
        /// CommandModelの引数の型が定義された集約を返します。
        /// 定義されていない場合は例外になります。
        /// </summary>
        internal static ChildAggregate GetCommandModelParameterChild(this RootAggregate rootAggregate) {
            var rootAggregateName = rootAggregate.PhysicalName;
            var expectedParameterName = $"{rootAggregateName}{PARAMETER_PHYSICAL_NAME}";
            var param = rootAggregate
                .GetMembers()
                .Single(m => m is ChildAggregate && m.PhysicalName == expectedParameterName);
            return (ChildAggregate)param;
        }

        /// <summary>
        /// CommandModelの戻り値の型が定義された集約を返します。
        /// 定義されていない場合は例外になります。
        /// </summary>
        internal static ChildAggregate GetCommandModelReturnValueChild(this RootAggregate rootAggregate) {
            var rootAggregateName = rootAggregate.PhysicalName;
            var expectedReturnValueName = $"{rootAggregateName}{RETURN_VALUE_PHYSICAL_NAME}";
            var param = rootAggregate
                .GetMembers()
                .Single(m => m is ChildAggregate && m.PhysicalName == expectedReturnValueName);
            return (ChildAggregate)param;
        }
    }
}
