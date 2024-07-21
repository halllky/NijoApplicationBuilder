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
    internal class BatchUpdateContext {
        /// <summary>
        /// 一括更新処理全体を通してのコンテキスト引数
        /// </summary>
        internal const string CLASS_NAME = "BatchUpadteContext";

        internal SourceFile Render() => new SourceFile {
            FileName = "SaveContext.cs",
            RenderContent = context => {
                return $$"""
                    namespace {{context.Config.RootNamespace}};

                    /// <summary>
                    /// 一括更新処理のコンテキスト引数。エラーメッセージや確認メッセージなどを書きやすくするためのもの。
                    /// </summary>
                    public partial class {{CLASS_NAME}} {
                        public {{CLASS_NAME}}(bool ignoreConfirm) {
                            IgnoreConfirm = ignoreConfirm;
                        }

                        private readonly List<string> _confirms = new();
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
                            _confirms.Add(message);
                        }
                        /// <summary>
                        /// 更新処理を実行してもよいかどうかをユーザーに問いかけるメッセージが1件以上あるかどうかを返します。
                        /// </summary>
                        public bool HasConfirm() {
                            return _confirms.Count > 0;
                        }
                        /// <summary>
                        /// trueの場合、 <see cref="AddConfirm" /> による警告があっても更新処理が続行されます。
                        /// 画面側で警告に対して「はい(Yes)」が選択されたあとのリクエストではこの値がtrueになります。
                        /// </summary>
                        public bool IgnoreConfirm { get; }

                        /// <summary>
                        /// ユーザーに知らせるべきエラー（入力値不正など）を追加します。
                        /// </summary>
                        /// <param name="errorItemIndex"></param>
                        /// <param name="error"></param>
                        public void AddError(int errorItemIndex, {{DataClassForSave.I_ERROR_DATA_STRUCTURE}} error) {
                            TODO #35
                        }
                        /// <summary>
                        /// 更新処理の途中で、ユーザーに伝えるべきエラー（入力値不正など）が発生したかどうか。
                        /// なおシステムエラーの場合は一般例外と同様に処理されるため、このプロパティを参照するまでもなく制御がcatch句に飛ぶ。
                        /// </summary>
                        public bool HasError() {

                        }
                    }
                    """;
            },
        };
    }
}
