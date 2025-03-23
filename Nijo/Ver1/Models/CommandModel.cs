using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models.CommandModelModules;
using Nijo.Ver1.Parts.Common;
using Nijo.Ver1.Parts.JavaScript;
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
            aggregateFile.AddTypeScriptTypeDef(parameterType.RenderTypeScript(ctx));
            aggregateFile.AddTypeScriptFunction(parameterType.RenderNewObjectFn());

            // データ型: パラメータ型メッセージ
            var parameterMessages = new ParameterTypeMessageContainer(rootAggregate);
            aggregateFile.AddCSharpClass(parameterMessages.RenderCSharp());
            aggregateFile.AddTypeScriptTypeDef(parameterMessages.RenderTypeScript());

            // データ型: 戻り値型定義
            var returnType = new ReturnType(rootAggregate);
            aggregateFile.AddCSharpClass(returnType.RenderCSharp(ctx));
            aggregateFile.AddTypeScriptTypeDef(returnType.RenderTypeScript(ctx));

            // 処理: TypeScript用マッピング、Webエンドポイント、本処理抽象メソッド
            var commandProcessing = new CommandProcessing(rootAggregate);
            aggregateFile.AddCSharpClass(commandProcessing.RenderAspNetCoreControllerAction(ctx));
            aggregateFile.AddCSharpClass(commandProcessing.RenderAppSrvMethods(ctx));

            // カスタムロジック用モジュール
            ctx.Use<MappingsForCustomize>().AddCommandModel(rootAggregate);

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
        internal static ChildAggreagte GetCommandModelParameterChild(this RootAggregate rootAggregate) {
            var param = rootAggregate
                .GetMembers()
                .Single(m => m is ChildAggreagte && m.PhysicalName == PARAMETER_PHYSICAL_NAME);
            return (ChildAggreagte)param;
        }

        /// <summary>
        /// CommandModelの戻り値の型が定義された集約を返します。
        /// 定義されていない場合は例外になります。
        /// </summary>
        internal static ChildAggreagte GetCommandModelReturnValueChild(this RootAggregate rootAggregate) {
            var param = rootAggregate
                .GetMembers()
                .Single(m => m is ChildAggreagte && m.PhysicalName == RETURN_VALUE_PHYSICAL_NAME);
            return (ChildAggreagte)param;
        }
    }
}
