using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.DataModelModules;
using Nijo.Parts.Common;
using Nijo.Parts.CSharp;
using Nijo.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Models {

    /// <summary>
    /// データモデル。
    /// アプリケーションに永続化されるデータの形を表す。
    /// トランザクションの境界の単位（より強い整合性の範囲）で区切られる。
    /// データモデルの境界を跨ぐエラーチェックは、一時的に整合性が崩れる可能性がある。
    /// DDD（ドメイン駆動設計）における集約ルートの概念とほぼ同じ。
    /// </summary>
    internal class DataModel : IModel {
        public string SchemaName => "data-model";

        public void Validate(XElement rootAggregateElement, SchemaParseContext context, Action<XElement, string> addError) {
            // ルートとChildrenはキー必須
            var rootAndChildren = rootAggregateElement
                .DescendantsAndSelf()
                .Where(el => el.Parent == el.Document?.Root
                          || el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value == SchemaParseContext.NODE_TYPE_CHILDREN);
            foreach (var el in rootAndChildren) {
                if (el.Elements().All(member => member.Attribute(BasicNodeOptions.IsKey.AttributeName) == null)) {
                    addError(el, "キーが指定されていません。");
                }
            }

            // 子集約に主キー属性がないことを確認
            var childAggregates = rootAggregateElement
                .Descendants()
                .Where(el => el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value == SchemaParseContext.NODE_TYPE_CHILD);
            foreach (var child in childAggregates) {
                var membersWithKey = child.Elements().Where(member => member.Attribute(BasicNodeOptions.IsKey.AttributeName) != null).ToList();
                if (membersWithKey.Count != 0) {
                    addError(child, "データモデルの子集約には主キー属性を付与することができません。");
                    foreach (var member in membersWithKey) {
                        addError(member, "この子集約のメンバーに主キー属性を付与することはできません。");
                    }
                }
            }

            // 循環参照のチェック（主キーや必須制約による閉路が生じないか）
            ValidateCircularReferences(rootAggregateElement, context, addError);
        }

        /// <summary>
        /// データモデルの循環参照チェック
        /// 主キーや必須制約による閉路が生じないか確認します
        /// </summary>
        private static void ValidateCircularReferences(XElement rootAggregateElement, SchemaParseContext context, Action<XElement, string> addError) {
            // 自身を起点とするすべての外部参照を取得
            var refElements = rootAggregateElement
                .Descendants()
                .Where(el => el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value?.StartsWith(SchemaParseContext.NODE_TYPE_REFTO + ":") == true)
                .ToList();

            if (refElements.Count == 0) {
                return; // 外部参照がなければ終了
            }

            // 循環参照チェック
            var graph = new Dictionary<XElement, List<(XElement RefTo, bool IsRequired)>>();
            var allVertexes = new HashSet<XElement>();

            // グラフ構築
            foreach (var refElement in refElements) {
                var targetElement = context.FindRefTo(refElement);
                if (targetElement == null) continue;

                // 参照元の最上位ルート集約
                var sourceRoot = refElement.AncestorsAndSelf().Last(e => e.Parent == e.Document?.Root);

                // 参照先の最上位ルート集約
                var targetRoot = targetElement.AncestorsAndSelf().Last(e => e.Parent == e.Document?.Root);

                // 自身のツリー内なら無視
                if (sourceRoot == targetRoot) continue;

                // 必須属性かどうか
                bool isRequired = refElement.Attribute(BasicNodeOptions.IsRequired.AttributeName)?.Value?.ToLower() == "true";

                // 主キーかどうか
                bool isKey = refElement.Attribute(BasicNodeOptions.IsKey.AttributeName)?.Value?.ToLower() == "true";

                // キーまたは必須の場合のみグラフに追加
                if (isKey || isRequired) {
                    if (!graph.TryGetValue(sourceRoot, out var value)) {
                        value = [];
                        graph[sourceRoot] = value;
                    }

                    value.Add((targetRoot, isRequired));

                    allVertexes.Add(sourceRoot);
                    allVertexes.Add(targetRoot);
                }
            }

            // 循環検出
            if (HasCycle(graph, allVertexes, out var cycle)) {
                // 必須キーの循環依存がある場合はエラー
                var hasMandatoryCycle = cycle.All(node => {
                    var sourceIdx = cycle.IndexOf(node);
                    var targetIdx = (sourceIdx + 1) % cycle.Count;

                    if (sourceIdx == -1 || targetIdx == -1) return false;

                    var source = cycle[sourceIdx];
                    var target = cycle[targetIdx];

                    return graph.ContainsKey(source) &&
                           graph[source].Any(edge => edge.RefTo == target && edge.IsRequired);
                });

                if (hasMandatoryCycle) {
                    string cycleStr = string.Join(" -> ", cycle.Select(context.GetPhysicalName));
                    addError(rootAggregateElement, $"循環参照の中で全てのリンクが必須またはキーとなっており、データを登録できません。循環: {cycleStr}");
                }
            }
        }

        /// <summary>
        /// グラフ内での循環を検出します
        /// </summary>
        private static bool HasCycle(Dictionary<XElement, List<(XElement RefTo, bool IsRequired)>> graph, HashSet<XElement> allVertexes, out List<XElement> cycle) {
            cycle = [];
            var visited = new Dictionary<XElement, bool>();
            var recStack = new HashSet<XElement>();

            foreach (var vertex in allVertexes) {
                if (!visited.ContainsKey(vertex)) {
                    if (IsCyclicUtil(vertex, visited, recStack, graph, cycle)) {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsCyclicUtil(XElement vertex, Dictionary<XElement, bool> visited, HashSet<XElement> recStack,
                                  Dictionary<XElement, List<(XElement RefTo, bool IsRequired)>> graph, List<XElement> cycle) {
            // 現在の頂点を訪問済みとマーク
            visited[vertex] = true;

            // 再帰スタックに追加
            recStack.Add(vertex);

            // この頂点から出ている辺を調べる
            if (graph.TryGetValue(vertex, out var value)) {
                foreach (var (RefTo, _) in value) {
                    var neighbor = RefTo;

                    // 未訪問の隣接頂点を再帰的に探索
                    if (!visited.ContainsKey(neighbor)) {
                        if (IsCyclicUtil(neighbor, visited, recStack, graph, cycle)) {
                            // 循環が見つかった場合、循環リストに追加
                            if (cycle.Count == 0 || cycle[0] != neighbor) {
                                cycle.Add(vertex);
                            }
                            return true;
                        }
                    }
                    // 再帰スタック内に既に存在する場合は循環がある
                    else if (recStack.Contains(neighbor)) {
                        // 循環の開始点として記録
                        cycle.Clear();
                        cycle.Add(neighbor);
                        cycle.Add(vertex);
                        return true;
                    }
                }
            }

            // この頂点からの探索が終了したので再帰スタックから削除
            recStack.Remove(vertex);
            return false;
        }

        public void GenerateCode(CodeRenderingContext ctx, RootAggregate rootAggregate) {
            var aggregateFile = new SourceFileByAggregate(rootAggregate);

            // データ型: EFCore Entity
            var efCoreEntity = new EFCoreEntity(rootAggregate);
            aggregateFile.AddCSharpClass(EFCoreEntity.RenderClassDeclaring(efCoreEntity, ctx), "Class_EFCoreEntity");
            ctx.Use<DbContextClass>().AddEntities(efCoreEntity.EnumerateThisAndDescendants());

            // データ型: SaveCommand
            aggregateFile.AddCSharpClass(SaveCommand.RenderAll(rootAggregate, ctx), "Class_SaveCommand");

            // データ型: ほかの集約から参照されるときのキー
            aggregateFile.AddCSharpClass(KeyClass.KeyClassEntry.RenderClassDeclaringRecursively(rootAggregate, ctx), "Class_KeyClass");

            // データ型: SaveCommandメッセージ
            var saveCommandMessage = new SaveCommandMessageContainer(rootAggregate);
            aggregateFile.AddCSharpClass(SaveCommandMessageContainer.RenderTree(rootAggregate), "Class_SaveCommandMessage");
            ctx.Use<MessageContainer.BaseClass>()
                .Register(saveCommandMessage.InterfaceName, saveCommandMessage.CsClassName)
                .Register(saveCommandMessage.CsClassName, saveCommandMessage.CsClassName);

            // 処理: 新規登録、更新、削除
            var create = new CreateMethod(rootAggregate);
            var update = new UpdateMethod(rootAggregate);
            var delete = new DeleteMethod(rootAggregate);
            aggregateFile.AddAppSrvMethod(create.Render(ctx), "新規登録処理");
            aggregateFile.AddAppSrvMethod(update.Render(ctx), "更新処理");
            aggregateFile.AddAppSrvMethod(delete.Render(ctx), "物理削除処理");

            // 処理: 自動生成されるバリデーションエラーチェック
            aggregateFile.AddAppSrvMethod($$"""
                #region 自動生成されるバリデーション処理
                {{CheckRequired.Render(rootAggregate, ctx)}}
                {{CheckMaxLength.Render(rootAggregate, ctx)}}
                {{CheckCharacterType.Render(rootAggregate, ctx)}}
                {{CheckDigitsAndScales.Render(rootAggregate, ctx)}}
                {{DynamicEnum.RenderAppSrvCheckMethod(rootAggregate, ctx)}}
                #endregion 自動生成されるバリデーション処理
                """, "バリデーション処理");

            // 処理: ダミーデータ作成関数
            ctx.Use<DummyDataGenerator>()
                .Add(rootAggregate);

            // 定数: メタデータ
            ctx.Use<Metadata>().Add(rootAggregate);

            // カスタムロジック用モジュール
            ctx.Use<CommandQueryMappings>().AddDataModel(rootAggregate);

            // QueryModelと全く同じ型の場合はそれぞれのモデルのソースも生成
            if (rootAggregate.GenerateDefaultQueryModel) {
                QueryModel.GenerateCode(ctx, rootAggregate, aggregateFile);
            }

            // 標準の一括作成コマンド
            if (rootAggregate.GenerateBatchUpdateCommand) {
                var batchUpdate = new BatchUpdate(rootAggregate);
                aggregateFile.AddWebapiControllerAction(batchUpdate.RenderControllerAction(ctx));
                aggregateFile.AddAppSrvMethod(batchUpdate.RenderAppSrvMethod(ctx), "一括更新処理");
            }

            aggregateFile.ExecuteRendering(ctx);
        }

        public void GenerateCode(CodeRenderingContext ctx) {
            // メッセージ
            UpdateMethod.RegisterCommonParts(ctx);

            // TODO ver.1: 追加更新削除区分のenum(C#)
        }
    }
}
