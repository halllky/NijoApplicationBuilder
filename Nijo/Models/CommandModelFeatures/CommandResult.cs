using Nijo.Core;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.CommandModelFeatures {
    /// <summary>
    /// <see cref="CommandModel"/> における本処理実行時引数。
    /// 主な役割は処理結果のハンドリングに関する処理。
    /// </summary>
    internal class CommandResult : ISummarizedFile {

        internal const string TS_TYPE_NAME = "CommandResult";

        // C#側ではWebAPIから実行された場合とコマンドラインで実行された場合で処理結果のハンドリング方法が異なる。
        // 例えばファイル生成処理の場合、Webならブラウザにバイナリを返してダウンロードさせるが、
        // コマンドラインの場合は実行環境のどこかのフォルダにファイルを出力する。
        internal const string GENERATOR_INTERFACE_NAME = "ICommandResultGenerator";

        internal const string GENERATOR_WEB_CLASS_NAME = "CommandResultGeneratorInWeb";
        internal const string GENERATOR_CLI_CLASS_NAME = "CommandResultGeneratorInCli";

        internal const string RESULT_INTERFACE_NAME = "ICommandResult";
        internal const string RESULT_WEB_CLASS_NAME = "CommandResultInWeb";
        internal const string RESULT_CLI_CLASS_NAME = "CommandResultInCli";

        // HTTPレスポンス
        internal const string TYPE_MESSAGE = "message";
        internal const string TYPE_REDIRECT = "redirect";
        internal const string HTTP_CONFIRM_DETAIL = "detail";
        internal const string HTTP_ERROR_DETAIL = "detail";

        /// <summary>
        /// コマンド処理でこの詳細画面へ遷移する処理を書けるように登録する
        /// </summary>
        internal void Register(GraphNode<Aggregate> aggregate) {
            _redirectableList.Add(new() {
                Aggregate = aggregate,
                DisplayData = new ReadModel2Features.DataClassForDisplay(aggregate),
            });
        }
        private readonly List<Redirectable> _redirectableList = new();
        private class Redirectable {
            internal required GraphNode<Aggregate> Aggregate { get; init; }
            internal required ReadModel2Features.DataClassForDisplay DisplayData { get; init; }
        }


        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {
            context.ReactProject.Types.Add(RenderTsDeclaring(context));

            // サーバー側処理結果ハンドラ
            context.CoreLibrary.UtilDir(dir => {
                dir.Generate(RenderInterface(context));
            });
            context.WebApiProject.UtilDir(dir => {
                dir.Generate(RenderResultHandlerInWeb(context));
            });
            context.CliProject.AutoGeneratedDir(dir => {
                dir.Generate(RenderResultHandlerInCommandLine(context));
            });
        }

        private SourceFile RenderInterface(CodeRenderingContext context) => new SourceFile {
            FileName = "ICommandResultGenerator.cs",
            RenderContent = ctx => {
                return $$"""
                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// コマンド本処理実行時引数。
                    /// 主な役割は処理結果のハンドリングに関する処理。
                    /// </summary>
                    public interface {{GENERATOR_INTERFACE_NAME}}<TErrors> where TErrors : {{MessageReceiver.RECEIVER}} {
                        /// <summary>
                        /// 処理が成功した旨のみをユーザーに伝えます。
                        /// </summary>
                        {{RESULT_INTERFACE_NAME}} Ok() {
                            return this.Ok<object?>(null, null);
                        }
                        /// <summary>
                        /// 処理が成功した旨のみをユーザーに伝えます。
                        /// </summary>
                        /// <param name="detail">詳細情報</param>
                        {{RESULT_INTERFACE_NAME}} Ok<T>(T detail) {
                            return this.Ok(null, detail);
                        }
                        /// <summary>
                        /// 処理が成功した旨のみをユーザーに伝えます。
                        /// </summary>
                        /// <param name="text">メッセージ</param>
                        /// <param name="detail">詳細情報</param>
                        {{RESULT_INTERFACE_NAME}} Ok<T>(string? text, T detail);

                    {{_redirectableList.SelectTextTemplate(x => $$"""
                        /// <summary>
                        /// {{x.Aggregate.Item.DisplayName}}の詳細画面へ遷移します。
                        /// </summary>
                        {{RESULT_INTERFACE_NAME}} Redirect({{x.DisplayData.CsClassName}} displayData, {{ReadModel2Features.SingleView.E_SINGLE_VIEW_TYPE}} mode, {{ReadModel2Features.SingleView.E_REFETCH_TYPE}} refetchType);
                    """)}}

                        /// <summary>
                        /// ユーザーにファイルを返します。
                        /// </summary>
                        /// <param name="file">ファイルコンテンツのバイナリ</param>
                        /// <param name="contentType">HTTPレスポンスヘッダにつけるContent-Type</param>
                        {{RESULT_INTERFACE_NAME}} File(byte[] bytes, string contentType);

                        /// <summary>
                        /// 処理の途中でエラーが発生した旨をユーザーに伝えます。
                        /// </summary>
                        /// <param name="error">エラー内容</param>
                        {{RESULT_INTERFACE_NAME}} Error(string error);
                        /// <summary>
                        /// 処理の途中でエラーが発生した旨をユーザーに伝えます。
                        /// </summary>
                        /// <param name="errors">エラー内容</param>
                        {{RESULT_INTERFACE_NAME}} Error(TErrors errors);
                    
                        /// <summary>
                        /// 処理を続行してもよいかどうかユーザー側が確認し了承する必要がある旨を返します。
                        /// </summary>
                        /// <param name="confirm">確認メッセージの</param>
                        {{RESULT_INTERFACE_NAME}} Confirm(string confirm);
                        /// <summary>
                        /// 処理を続行してもよいかどうかユーザー側が確認し了承する必要がある旨を返します。
                        /// </summary>
                        /// <param name="confirms">確認メッセージの一覧</param>
                        {{RESULT_INTERFACE_NAME}} Confirm(IEnumerable<string> confirms);
                    }

                    /// <summary>
                    /// コマンド実行結果。
                    /// コマンド処理のメソッド内の記述中に想定外のオブジェクトがreturnされるコードが書かれたとき
                    /// コンパイルエラーにするためだけにインターフェースとしている。
                    /// </summary>
                    public interface {{RESULT_INTERFACE_NAME}} {
                        // 特になし
                    }
                    """;
            },
        };

        internal const string ACTION_RESULT_CONTAINER = "ActionResultContainer";
        private SourceFile RenderResultHandlerInWeb(CodeRenderingContext context) => new SourceFile {
            FileName = "CommandResultInWeb.cs",
            RenderContent = ctx => {
                var appSrv = new ApplicationService();

                return $$"""
                    using Microsoft.AspNetCore.Mvc;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// コマンド本処理実行時引数。
                    /// 主な役割は処理結果のハンドリングに関する処理。
                    /// </summary>
                    public sealed partial class {{GENERATOR_WEB_CLASS_NAME}}<TErrors> : {{GENERATOR_INTERFACE_NAME}}<TErrors>
                        where TErrors : {{MessageReceiver.RECEIVER}}, new() {
                        public {{GENERATOR_WEB_CLASS_NAME}}(ControllerBase controller, {{appSrv.ConcreteClassName}} applicationService) {
                            _controller = controller;
                            _applicationService = applicationService;
                        }
                        private readonly ControllerBase _controller;
                        private readonly {{appSrv.ConcreteClassName}} _applicationService;

                        public {{RESULT_INTERFACE_NAME}} Ok<T>(string? text, T detail) {
                            return new {{ACTION_RESULT_CONTAINER}} {
                                ActionResult = _controller.Ok(new { type = "{{TYPE_MESSAGE}}", text, detail }),
                            };
                        }

                    {{_redirectableList.SelectTextTemplate(x => $$"""
                        public {{RESULT_INTERFACE_NAME}} Redirect({{x.DisplayData.CsClassName}} displayData, {{ReadModel2Features.SingleView.E_SINGLE_VIEW_TYPE}} mode, {{ReadModel2Features.SingleView.E_REFETCH_TYPE}} refetchType) {
                            return new {{ACTION_RESULT_CONTAINER}} {
                                ActionResult = _controller.Ok(new { type = "{{TYPE_REDIRECT}}", url = _applicationService.{{ReadModel2Features.SingleView.GET_URL_FROM_DISPLAY_DATA}}(displayData, mode, refetchType) }),
                            };
                        }
                    """)}}

                        public {{RESULT_INTERFACE_NAME}} File(byte[] bytes, string contentType) {
                            return new {{ACTION_RESULT_CONTAINER}} {
                                ActionResult = _controller.File(bytes, contentType),
                            };
                        }

                        public {{RESULT_INTERFACE_NAME}} Error(string error) {
                            var errorObject = new TErrors();
                            errorObject.AddError(error);
                            return Error(errorObject);
                        }
                        public {{RESULT_INTERFACE_NAME}} Error(TErrors errors) {
                            return new {{ACTION_RESULT_CONTAINER}} {
                                ActionResult = _controller.UnprocessableEntity(new { {{HTTP_ERROR_DETAIL}} = errors.ToJsonNodes(null) }),
                            };
                        }
                        public {{RESULT_INTERFACE_NAME}} Confirm(string confirm) {
                            return Confirm([confirm]);
                        }
                        public {{RESULT_INTERFACE_NAME}} Confirm(IEnumerable<string> confirms) {
                            return new {{ACTION_RESULT_CONTAINER}} {
                                ActionResult = _controller.Accepted(new { {{HTTP_CONFIRM_DETAIL}} = confirms.ToArray() }),
                            };
                        }

                        public class {{ACTION_RESULT_CONTAINER}} : {{RESULT_INTERFACE_NAME}} {
                            public required IActionResult ActionResult { get; init; }
                        }
                    }
                    """;
            },
        };
        private SourceFile RenderResultHandlerInCommandLine(CodeRenderingContext context) => new SourceFile {
            FileName = "CommandResultInCommandLine.cs",
            RenderContent = ctx => {
                return $$"""
                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// コマンド本処理実行時引数。
                    /// 主な役割は処理結果のハンドリングに関する処理。
                    /// </summary>
                    public sealed partial class {{GENERATOR_CLI_CLASS_NAME}}<TErrors> : {{GENERATOR_INTERFACE_NAME}}<TErrors>
                        where TErrors : {{MessageReceiver.RECEIVER}} {
                        public {{RESULT_INTERFACE_NAME}} Ok<T>(string? text, T detail) {
                            throw new NotImplementedException("TODO #3 未実装");
                        }
                    {{_redirectableList.SelectTextTemplate(x => $$"""
                        public {{RESULT_INTERFACE_NAME}} Redirect({{x.DisplayData.CsClassName}} displayData, {{ReadModel2Features.SingleView.E_SINGLE_VIEW_TYPE}} mode, {{ReadModel2Features.SingleView.E_REFETCH_TYPE}} refetchType) {
                            throw new NotImplementedException("TODO #3 未実装");
                        }
                    """)}}
                        public {{RESULT_INTERFACE_NAME}} File(byte[] bytes, string contentType) {
                            throw new NotImplementedException("TODO #3 未実装");
                        }
                        public {{RESULT_INTERFACE_NAME}} Error(string error) {
                            throw new NotImplementedException("TODO #3 未実装");
                        }
                        public {{RESULT_INTERFACE_NAME}} Error(TErrors errors) {
                            throw new NotImplementedException("TODO #3 未実装");
                        }
                        public {{RESULT_INTERFACE_NAME}} Confirm(string confirm) {
                            throw new NotImplementedException("TODO #3 未実装");
                        }
                        public {{RESULT_INTERFACE_NAME}} Confirm(IEnumerable<string> confirms) {
                            throw new NotImplementedException("TODO #3 未実装");
                        }
                    }
                    """;
            },
        };

        private static string RenderTsDeclaring(CodeRenderingContext context) {
            return $$"""
                /** コマンド正常終了時の処理結果 */
                export type {{TS_TYPE_NAME}}
                  = { type: '{{TYPE_MESSAGE}}', text?: string, detail?: object } // 処理結果のメッセージや詳細情報が画面上に表示される。
                  | { type: '{{TYPE_REDIRECT}}', url: string } // 特定の画面へ遷移する。画面初期値はクエリパラメータに付される。
                  | { type: 'file', blob: string } // ファイルダウンロード
                """;
        }
    }
}
