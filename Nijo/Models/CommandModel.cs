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

        public string HelpTextMarkdown => $$"""
            コマンド。
            アクター（このアプリケーションのユーザまたは外部システム）がこのアプリケーションの状態やこのアプリケーションの{{nameof(DataModel)}}に何らかの変更を加えるときの操作のデータの形。
            コマンドが実行されると{{nameof(DataModel)}}に何らかの変更がかかる。
            CQS, CQRS における Command とほぼ同じ。
            {{nameof(QueryModel)}} とは対の関係にある。
            （{{nameof(CommandModel)}}はアクターから{{nameof(DataModel)}}へのデータの流れ、{{nameof(QueryModel)}}は{{nameof(DataModel)}}からアクターへのデータの流れ）
            """;

        public void Validate(XElement rootAggregateElement, SchemaParseContext context, Action<XElement, string> addError) {
        }

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
            ctx.Use<MessageContainer.BaseClass>().Register(parameterMessages.CsClassName, parameterMessages.CsClassName);

            // データ型: 戻り値型定義
            var returnType = new ReturnType(rootAggregate);
            aggregateFile.AddCSharpClass(returnType.RenderCSharp(ctx));
            aggregateFile.AddTypeScriptTypeDef(returnType.RenderTypeScript(ctx));

            // 処理: TypeScript用マッピング、Webエンドポイント、本処理抽象メソッド
            var commandProcessing = new CommandProcessing(rootAggregate);
            aggregateFile.AddWebapiControllerAction(commandProcessing.RenderAspNetCoreControllerAction(ctx));
            aggregateFile.AddAppSrvMethod(commandProcessing.RenderAppSrvMethods(ctx));

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
