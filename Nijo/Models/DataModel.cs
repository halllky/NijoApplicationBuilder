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
    /// 永続化され、アプリケーションのデータの整合性を保つ境界。
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
            aggregateFile.AddCSharpClass(EFCoreEntity.RenderClassDeclaring(efCoreEntity, ctx));
            ctx.Use<DbContextClass>().AddEntities(efCoreEntity.EnumerateThisAndDescendants());

            // データ型: SaveCommand
            var saveCommand = new SaveCommand(rootAggregate);
            aggregateFile.AddCSharpClass(saveCommand.RenderAll(ctx));

            // データ型: ほかの集約から参照されるときのキー
            aggregateFile.AddCSharpClass(KeyClass.KeyClassEntry.RenderClassDeclaringRecursively(rootAggregate, ctx));

            // データ型: SaveCommandメッセージ
            var saveCommandMessage = new SaveCommandMessageContainer(rootAggregate);
            aggregateFile.AddCSharpClass(SaveCommandMessageContainer.RenderTree(rootAggregate));
            ctx.Use<MessageContainer.BaseClass>()
                .Register(saveCommandMessage.InterfaceName, saveCommandMessage.CsClassName)
                .Register(saveCommandMessage.CsClassName, saveCommandMessage.CsClassName);

            // 処理: 新規登録、更新、削除
            var create = new CreateMethod(rootAggregate);
            var update = new UpdateMethod(rootAggregate);
            var delete = new DeleteMethod(rootAggregate);
            aggregateFile.AddAppSrvMethod(create.Render(ctx));
            aggregateFile.AddAppSrvMethod(update.Render(ctx));
            aggregateFile.AddAppSrvMethod(delete.Render(ctx));

            // 処理: 自動生成されるバリデーションエラーチェック
            aggregateFile.AddAppSrvMethod($$"""
                #region 自動生成されるバリデーション処理
                {{CheckRequired.Render(rootAggregate, ctx)}}
                {{CheckMaxLength.Render(rootAggregate, ctx)}}
                {{CheckCharacterType.Render(rootAggregate, ctx)}}
                {{CheckDigitsAndScales.Render(rootAggregate, ctx)}}
                {{DynamicEnum.RenderAppSrvCheckMethod(rootAggregate, ctx)}}
                #endregion 自動生成されるバリデーション処理
                """);

            // 処理: ダミーデータ作成関数
            ctx.Use<DummyDataGenerator>()
                .Add(rootAggregate);

            // 定数: メタデータ
            ctx.Use<Metadata>().Add(rootAggregate);

            // QueryModelと全く同じ型の場合はそれぞれのモデルのソースも生成
            if (rootAggregate.GenerateDefaultQueryModel) {
                QueryModel.GenerateCode(ctx, rootAggregate, aggregateFile);
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
