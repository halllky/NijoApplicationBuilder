using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models.DataModelModules;
using Nijo.Ver1.Parts.Common;
using Nijo.Ver1.Parts.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models {

    /// <summary>
    /// データモデル。
    /// 永続化され、アプリケーションのデータの整合性を保つ境界。
    /// </summary>
    internal class DataModel : IModel {
        public void GenerateCode(CodeRenderingContext ctx, RootAggregate rootAggregate) {
            var aggregateFile = new SourceFileByAggregate(rootAggregate);

            // データ型: EFCore Entity
            var efCoreEntity = new EFCoreEntity(rootAggregate);
            aggregateFile.AddCSharpClass(efCoreEntity.RenderClassDeclaring(ctx));

            ctx.Use<DbContextClass>()
                .AddDbSet(efCoreEntity.RenderDbSetProperty(ctx))
                .AddOnModelCreating(efCoreEntity.RenderOnModelCreatingCalling(ctx));

            // データ型: SaveCommand
            var saveCommand = new SaveCommand(rootAggregate);
            aggregateFile.AddCSharpClass(saveCommand.RenderCreateCommandDeclaring(ctx));
            aggregateFile.AddCSharpClass(saveCommand.RenderUpdateCommandDeclaring(ctx));
            aggregateFile.AddCSharpClass(saveCommand.RenderDeleteCommandDeclaring(ctx));

            // データ型: SaveCommandメッセージ
            var saveCommandMessage = new MessageContainer(rootAggregate);
            aggregateFile.AddCSharpClass(saveCommandMessage.RenderCSharp());
            aggregateFile.AddTypeScriptSource(saveCommandMessage.RenderTypeScript());

            // データ型: ほかの集約から参照されるときのキー
            var refTargetKey = new KeyClass(rootAggregate);
            aggregateFile.AddCSharpClass(refTargetKey.RenderClassDeclaring(ctx));

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
                {{DynamicEnum.RenderAppSrvCheckMethod(rootAggregate, ctx)}}
                #endregion 自動生成されるバリデーション処理
                """);

            // 処理: ダミーデータ作成関数
            ctx.Use<DummyDataGenerator>()
                .Add(rootAggregate);

            // 他モデルと全く同じ型の場合はそれぞれのモデルのソースも生成
            if (rootAggregate.GenerateDefaultQueryModel) {
                new QueryModel().GenerateCode(ctx, rootAggregate);
            }
            if (rootAggregate.GenerateBatchUpdateCommand) {
                var batchUpdate = new BatchUpdate(rootAggregate);
                aggregateFile.AddAppSrvMethod(batchUpdate.RenderAppSrvMethod(ctx));
                aggregateFile.AddWebapiControllerAction(batchUpdate.RenderControllerAction(ctx));
            }

            aggregateFile.ExecuteRendering(ctx);
        }

        public void GenerateCode(CodeRenderingContext ctx) {
            // TODO ver.1: 追加更新削除区分のenum(C#, TypeScript)
        }
    }
}
