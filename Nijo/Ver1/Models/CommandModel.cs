using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models.CommandModelModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models {
    internal class CommandModel : IModel {
        public void GenerateCode(CodeRenderingContext ctx, RootAggregate rootAggregate) {
            var aggregateFile = new SourceFileByAggregate(rootAggregate);

            // データ型: パラメータ型定義
            var parameterType = new ParameterType(rootAggregate);
            aggregateFile.AddCSharpClass(parameterType.RenderCSharp(ctx));
            aggregateFile.AddTypeScriptSource(parameterType.RenderTypeScript(ctx));

            // データ型: 戻り値型定義
            var returnType = new ReturnType(rootAggregate);
            aggregateFile.AddCSharpClass(returnType.RenderCSharp(ctx));
            aggregateFile.AddTypeScriptSource(returnType.RenderTypeScript(ctx));

            // データ型: メッセージ型定義
            var messageType = new MessageType(rootAggregate);
            aggregateFile.AddCSharpClass(messageType.RenderCSharp(ctx));
            aggregateFile.AddTypeScriptSource(messageType.RenderTypeScript(ctx));

            // 処理: クライアント側新規オブジェクト作成関数
            var objectCreation = new ObjectCreation(rootAggregate);
            aggregateFile.AddTypeScriptSource(objectCreation.RenderTypeScript(ctx));

            // 処理: Reactフック、Webエンドポイント、本処理抽象メソッド
            var commandProcessing = new CommandProcessing(rootAggregate);
            aggregateFile.AddTypeScriptSource(commandProcessing.RenderReactHook(ctx));
            aggregateFile.AddCSharpClass(commandProcessing.RenderWebEndpoint(ctx));
            aggregateFile.AddCSharpClass(commandProcessing.RenderAbstractMethod(ctx));
        }

        public void GenerateCode(CodeRenderingContext ctx) {
            // 現時点では実装なし
        }
    }
}
