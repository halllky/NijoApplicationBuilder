using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.Parts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Parts.CSharp {
    /// <summary>
    /// ユーザーとの対話が発生する何らかの処理で、
    /// 1回のリクエスト・レスポンスのサイクルの一連の情報を保持するコンテキスト情報
    /// </summary>
    internal class PresentationContext {

        internal const string CLASS_NAME = "PresentationContext";
        internal const string OPTIONS = "PresentationContextOptions";
        internal const string RESULT = "PresentationContextResult";

        internal static SourceFile RenderStaticCsharp(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "PresentationContext.cs",
                Contents = $$"""
                    using System.Text.Json.Nodes;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// ユーザーとの対話が発生する何らかの処理で、
                    /// 1回のリクエスト・レスポンスのサイクルの一連の情報を保持するコンテキスト情報
                    /// </summary>
                    public sealed class {{CLASS_NAME}} {
                        /// <inheritdoc cref="{{CLASS_NAME}}"/>
                        public {{CLASS_NAME}}({{OPTIONS}} options, {{MessageContainer.ABSTRACT_CLASS}} messageContainerRoot) {
                            Options = options;
                            Messages = messageContainerRoot;
                        }

                        /// <inheritdoc cref="{{OPTIONS}}"/>
                        public {{OPTIONS}} Options { get; }


                        #region トーストメッセージ
                        /// <summary>トーストメッセージ</summary>
                        public string? ToastMessage { get; set; }
                        #endregion トーストメッセージ


                        #region 詳細メッセージ（エラー、警告、インフォメーション）
                        /// <summary>
                        /// パラメータの各値に対するメッセージ。エラーや警告など。
                        /// </summary>
                        public {{MessageContainer.ABSTRACT_CLASS}} Messages { get; }

                        public void AddError(string message) => Messages.AddError(message);
                        public void AddWarn(string message) => Messages.AddWarn(message);
                        public void AddInfo(string message) => Messages.AddInfo(message);
                        public bool HasError() => Messages.HasError();
                        #endregion 詳細メッセージ（エラー、警告、インフォメーション）


                        #region 確認メッセージ
                        private readonly List<string> _confirms = [];
                        /// <summary>
                        /// 「～しますがよろしいですか？」などの確認メッセージを追加します。
                        /// </summary>
                        public void AddConfirm(string text) {
                            _confirms.Add(text);
                        }
                        public bool HasConfirm() {
                            return _confirms.Count > 0;
                        }
                        #endregion 確認メッセージ

                        /// <summary>
                        /// このインスタンスの現在の状態に基づき、処理結果を表すプレーンなオブジェクトルートを返します。
                        /// </summary>
                        public {{RESULT}} ToResult() {
                            return new {{RESULT}} {
                                Ok = !HasError() && (Options.IgnoreConfirm || _confirms.Count > 0),
                                ToastMessage = ToastMessage,
                                ParamMessage = Messages,
                                Confirms = _confirms,
                            };
                        }
                    }

                    /// <summary>
                    /// <see cref="{{CLASS_NAME}}"/> のオプション
                    /// </summary>
                    public partial class {{OPTIONS}} {
                        /// <summary>
                        /// Confirm（「～しますがよろしいですか？」の確認メッセージ）が発生しても無視するかどうか。
                        /// HTTPリクエストは「～しますがよろしいですか？」に対してOKが選択される前と後で計2回発生するが、
                        /// 1回目はfalse, 2回目はtrueになる。
                        /// </summary>
                        public required bool IgnoreConfirm { get; init; }
                    }

                    /// <summary>
                    /// <see cref="{{CLASS_NAME}}"/> 処理結果の状態。
                    /// Webサーバー側からクライアント側に返されるプレーンなオブジェクト。
                    /// </summary>
                    public sealed class {{RESULT}} {
                        /// <summary>
                        /// 処理の目的を完遂できたかどうか。
                        /// <see cref="{{OPTIONS}}.IgnoreConfirm"/> がfalseである処理1巡目の場合、エラーがなければtrue
                        /// </summary>
                        public required bool Ok { get; init; }
                        /// <summary>
                        /// トーストメッセージ
                        /// </summary>
                        publid required string? ToastMessage { get; init; }
                        /// <summary>
                        /// パラメータの各値に対するメッセージ。エラーや警告など。
                        /// </summary>
                        publid required {{MessageContainer.ABSTRACT_CLASS}}? ParamMessage { get; init; }
                        /// <summary>
                        /// 確認メッセージ
                        /// </summary>
                        public List<string> Confirms { get; init; }

                        [Obsolete("ToastMessageに変更されました。")]
                        public string? Summary => ToastMessage;
                        [Obsolete("ParamMessageに変更されました。")]
                        public string? Details => ParamMessage;
                    }
                    """,
            };
        }
    }
}
