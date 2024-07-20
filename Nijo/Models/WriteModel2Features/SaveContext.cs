using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// 保存処理関連のコンテキスト引数
    /// </summary>
    internal class SaveContext {
        /// <summary>
        /// 一括更新処理全体を通してのコンテキスト引数
        /// </summary>
        internal const string BATCH_UPDATE_CONTEXT = "BatchUpadteContext";
        /// <summary>
        /// 更新前イベントの引数
        /// </summary>
        internal const string BEFORE_SAVE_CONTEXT = "BeforeSaveContext";
        /// <summary>
        /// 更新後イベントの引数
        /// </summary>
        internal const string AFTER_SAVE_CONTEXT = "AfterSaveContext";

        internal SourceFile Render() => new SourceFile {
            FileName = "SaveContext.cs",
            RenderContent = context => {
                return $$"""
                    namespace {{context.Config.RootNamespace}};

                    /// <summary>
                    /// 一括更新処理のコンテキスト引数。エラーメッセージや確認メッセージなどを書きやすくするためのもの。
                    /// </summary>
                    public partial class {{BATCH_UPDATE_CONTEXT}} {
                        public {{BATCH_UPDATE_CONTEXT}}(bool ignoreConfirm) {
                            IgnoreConfirm = ignoreConfirm;
                        }

                        /// <summary>
                        /// <para>
                        /// 更新処理を実行してもよいかどうかをユーザーに問いかけるメッセージを追加します。
                        /// </para>
                        /// <para>
                        /// ボタンの意味を統一してユーザーが混乱しないようにするため、
                        /// 「はい(Yes)」を選択したときに処理が続行され、
                        /// 「いいえ(No)」を選択したときに処理が中断されるような文言にしてください。
                        /// </para>
                        /// <para>
                        /// <see cref="IgnoreConfirm"/> がfalseのリクエストで何らかのコンファームが発生した場合、
                        /// 更新処理は中断されます。
                        /// </para>
                        /// </summary>
                        public void AddConfirm(string message) {
                            TODO #35
                        }

                        /// <summary>
                        /// trueの場合、 <see cref="AddConfirm" /> による警告があっても更新処理が続行されます。
                        /// 画面側で警告に対して「はい(Yes)」が選択されたあとのリクエストではこの値がtrueになります。
                        /// </summary>
                        public bool IgnoreConfirm { get; }

                        /// <summary>
                        /// 更新処理の途中で、ユーザーに伝えるべきエラー（入力値不正など）が発生したかどうか。
                        /// なおシステムエラーの場合は一般例外と同様に処理されるため、このプロパティを参照するまでもなく制御がcatch句に飛ぶ。
                        /// </summary>
                        public bool HasUserError { get; }
                    }

                    /// <summary>
                    /// 更新処理実行前のコンテキスト引数。エラーメッセージや確認メッセージなどを書きやすくするためのもの。
                    /// </summary>
                    public partial class {{BEFORE_SAVE_CONTEXT}}<TErrorData> where TErrorData: new() {
                        public {{BEFORE_SAVE_CONTEXT}}({{BATCH_UPDATE_CONTEXT}} outerContext) {
                            _outerContext = outerContext;
                        }
                        private readonly {{BATCH_UPDATE_CONTEXT}} _outerContext;

                        /// <inheritdoc cref="{{BATCH_UPDATE_CONTEXT}}.AddConfirm(string)"/>
                        public void AddConfirm(string message) => _outerContext.AddConfirm(message);

                        /// <inheritdoc cref="{{BATCH_UPDATE_CONTEXT}}.IgnoreConfirm"/>
                        public bool IgnoreConfirm { get; }

                        /// <inheritdoc cref="{{BATCH_UPDATE_CONTEXT}}.HasUserError"/>
                        public bool HasUserError { get; }

                        /// <summary>入力値不正などのエラー情報。何らかのエラーがある場合は更新処理が中断されます。</summary>
                        public TErrorData Errors { get; } = new();
                    }

                    /// <summary>
                    /// 更新処理実行後のコンテキスト引数
                    /// </summary>
                    public partial class {{AFTER_SAVE_CONTEXT}} {
                        // とくになし
                    }
                    """;
            },
        };
    }
}
