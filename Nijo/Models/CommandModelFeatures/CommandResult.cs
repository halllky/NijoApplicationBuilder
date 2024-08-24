using Nijo.Util.CodeGenerating;
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
    internal class CommandResult {

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

        internal static SourceFile RenderInterface(CodeRenderingContext context) => new SourceFile {
            FileName = "ICommandResultGenerator.cs",
            RenderContent = ctx => {
                return $$"""
                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// コマンド本処理実行時引数。
                    /// 主な役割は処理結果のハンドリングに関する処理。
                    /// </summary>
                    public interface {{GENERATOR_INTERFACE_NAME}} {
                        /// <summary>
                        /// 処理が成功した旨のみをユーザーに伝えます。
                        /// </summary>
                        {{RESULT_INTERFACE_NAME}} Success<T>() {
                            return this.Success<object?>(null, null);
                        }
                        /// <summary>
                        /// 処理が成功した旨のみをユーザーに伝えます。
                        /// </summary>
                        /// <param name="detail">詳細情報</param>
                        {{RESULT_INTERFACE_NAME}} Success<T>(T detail) {
                            return this.Success(null, detail);
                        }
                        /// <summary>
                        /// 処理が成功した旨のみをユーザーに伝えます。
                        /// </summary>
                        /// <param name="text">メッセージ</param>
                        /// <param name="detail">詳細情報</param>
                        {{RESULT_INTERFACE_NAME}} Success<T>(string? text, T detail);

                        /// <summary>
                        /// 特定画面への遷移を行います。
                        /// </summary>
                        {{RESULT_INTERFACE_NAME}} Redirect(/* TODO #3 ReadModelのオブジェクトを渡せるようにする */);

                        /// <summary>
                        /// ユーザーにファイルを返します。
                        /// </summary>
                        /// <param name="file">ファイルコンテンツのバイナリ</param>
                        /// <param name="contentType">HTTPの Content-Type</param>
                        {{RESULT_INTERFACE_NAME}} File(byte[] bytes, string contentType);
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
        internal static SourceFile RenderResultHandlerInWeb(CodeRenderingContext context) => new SourceFile {
            FileName = "CommandResultInWeb.cs",
            RenderContent = ctx => {
                return $$"""
                    using Microsoft.AspNetCore.Mvc;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// コマンド本処理実行時引数。
                    /// 主な役割は処理結果のハンドリングに関する処理。
                    /// </summary>
                    public sealed partial class {{GENERATOR_WEB_CLASS_NAME}} : {{GENERATOR_INTERFACE_NAME}} {
                        public {{GENERATOR_WEB_CLASS_NAME}}(ControllerBase controller) {
                            _controller = controller;
                        }
                        private readonly ControllerBase _controller;

                        {{RESULT_INTERFACE_NAME}} {{GENERATOR_INTERFACE_NAME}}.Success<T>(string? text, T detail) {
                            return new {{ACTION_RESULT_CONTAINER}} {
                                ActionResult = _controller.Ok(new { text, detail }),
                            };
                        }
                        {{RESULT_INTERFACE_NAME}} {{GENERATOR_INTERFACE_NAME}}.Redirect() {
                            throw new NotImplementedException("TODO #3 未実装");
                        }
                        {{RESULT_INTERFACE_NAME}} {{GENERATOR_INTERFACE_NAME}}.File(byte[] bytes, string contentType) {
                            return new {{ACTION_RESULT_CONTAINER}} {
                                ActionResult = _controller.File(bytes, contentType),
                            };
                        }

                        public class {{ACTION_RESULT_CONTAINER}} : {{RESULT_INTERFACE_NAME}} {
                            public required IActionResult ActionResult { get; init; }
                        }
                    }
                    """;
            },
        };
        internal static SourceFile RenderResultHandlerInCommandLine(CodeRenderingContext context) => new SourceFile {
            FileName = "CommandResultInCommandLine.cs",
            RenderContent = ctx => {
                return $$"""
                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// コマンド本処理実行時引数。
                    /// 主な役割は処理結果のハンドリングに関する処理。
                    /// </summary>
                    public sealed partial class {{GENERATOR_CLI_CLASS_NAME}} : {{GENERATOR_INTERFACE_NAME}} {
                        {{RESULT_INTERFACE_NAME}} {{GENERATOR_INTERFACE_NAME}}.Success<T>(string? text, T detail) {
                            throw new NotImplementedException("TODO #3 未実装");
                        }
                        {{RESULT_INTERFACE_NAME}} {{GENERATOR_INTERFACE_NAME}}.Redirect() {
                            throw new NotImplementedException("TODO #3 未実装");
                        }
                        {{RESULT_INTERFACE_NAME}} {{GENERATOR_INTERFACE_NAME}}.File(byte[] bytes, string contentType) {
                            throw new NotImplementedException("TODO #3 未実装");
                        }
                    }
                    """;
            },
        };

        internal static string RenderTsDeclaring(CodeRenderingContext context) {
            return $$"""
                /** コマンド正常終了時の処理結果 */
                export type {{TS_TYPE_NAME}}
                  = { type: 'message', text?: string, detail?: object } // 処理結果のメッセージや詳細情報が画面上に表示される。
                  | { type: 'redirect', url: string } // 特定の画面へ遷移する。画面初期値はクエリパラメータに付される。
                  | { type: 'file', blob: string } // ファイルダウンロード
                """;
        }
    }
}
