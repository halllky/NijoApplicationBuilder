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
            # CommandModel コマンドモデル
            アクター（このアプリケーションのユーザまたは外部システム）がこのアプリケーションの状態やこのアプリケーションのDataModelに何らかの変更を加えるときの操作のデータの形。
            コマンドが実行されるとDataModelに何らかの変更がかかる。
            CQS, CQRS における Command とほぼ同じ。
            QueryModel とは対の関係にある。（CommandModelはアクターからDataModelへのデータの流れ、QueryModelはDataModelからアクターへのデータの流れ）

            CommandModelの集約1件からは以下のモジュールが生成される。

            ## コマンド処理
            このコマンドを実行するためのAPIが生成される。
            コマンド処理のデータフローは以下。

            ```mermaid
            sequenceDiagram
                participant ts_custom as TypeScript側<br/>自動生成されない部分
                participant ts_autogen as TypeScript側<br/>自動生成される部分
                participant aspcore as ASP.NET Core
                participant cs_autogen as C#35;側Execute<br/>メソッド(骨組みのみ自動生成)

                ts_custom->>ts_autogen: パラメータオブジェクトの<br/>新規作成関数(自動生成)を呼ぶ
                ts_autogen-->>ts_custom:　
                ts_custom->>ts_custom:UI操作等で<br/>パラメータオブジェクトの<br/>内容を編集
                ts_custom->>ts_autogen: パラメータオブジェクトを渡して<br/>コマンド実行関数(自動生成)<br/>を呼ぶ
                ts_autogen->>aspcore: コマンド実行エンドポイント<br/>呼び出し(HTTP POST)
                aspcore->>cs_autogen: アプリケーションサービスの<br/>Executeメソッド呼び出し<br/>(開発者が実装する必要がある)

                cs_autogen-->>aspcore: 処理結果返却
                aspcore-->>ts_autogen:　
                ts_autogen-->>ts_custom:　
            ```

            ## 重要な特性
            * CommandModelではC#側の処理の主要な内容は一切自動生成されず、すべて開発者が実装する必要がある
                * Executeメソッド内の実装はすべて開発者の責任となる
                * DataModelの更新・参照や、他システムとの連携処理などはすべて開発者が実装する
            * CommandModelではパラメータの形も戻り値の形も両方ともスキーマ定義で指定する
                * Parameter子集約がコマンドへの入力パラメータとなる
                * ReturnValue子集約がコマンドからの戻り値となる

            ## パラメータクラス（Parameter）
            コマンドのパラメータオブジェクトは、スキーマ定義で「Parameter」という物理名を持つ子集約として定義する。
            このパラメータオブジェクトの構造がそのままC#とTypeScriptの型として生成される。

            ```ts
            type とあるCommandのParameter型 = {
                /** スキーマ定義で指定された項目がパラメータの項目として自動的に生成される */
                入力項目その1?: string
                入力項目その2?: number

                /** 子集約がある場合はネストした形になる */
                子パラメータ: {
                    子の入力項目1?: string
                    子の入力項目2?: number
                    // 以下略...
                }

                /** 配列の子集約も定義可能 */
                明細パラメータ: {
                    明細の入力項目1?: string
                    明細の入力項目2?: number
                    // 以下略...
                }[]

                // 以下略...
            }
            ```

            ## 戻り値クラス（ReturnValue）
            コマンドの戻り値オブジェクトは、スキーマ定義で「ReturnValue」という物理名を持つ子集約として定義する。
            この戻り値オブジェクトの構造がそのままC#とTypeScriptの型として生成される。

            ```ts
            type とあるCommandのReturnValue型 = {
                /** スキーマ定義で指定された項目が戻り値の項目として自動的に生成される */
                結果項目その1?: string
                結果項目その2?: number

                /** 子集約がある場合はネストした形になる */
                子結果: {
                    子の結果項目1?: string
                    子の結果項目2?: number
                    // 以下略...
                }

                /** 配列の子集約も定義可能 */
                明細結果: {
                    明細の結果項目1?: string
                    明細の結果項目2?: number
                    // 以下略...
                }[]

                // 以下略...
            }
            ```

            ## TypeScriptによる開発補助のための関数等
            * コマンド実行をJavaScriptから呼び出すための関数。およびそのリクエストを受け付けるためのASP.NET Core Controller Action。
            * パラメータオブジェクトを新規作成する関数

            ## メタデータ
            スキーマ定義で指定された、項目ごとの文字列長や桁数や必須か否かなどの情報自体をプロジェクトで使用したい場合に使う。
            主にReact Hook Form や zod のようなバリデーション機能をもったライブラリで使用されるような使い方を想定。
            C#, TypeScript それぞれで生成される。
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
