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
