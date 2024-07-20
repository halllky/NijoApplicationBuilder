using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// 一括更新処理のコンテキスト引数。
    /// エラーメッセージや確認メッセージなどを書きやすくするためのもの。
    /// </summary>
    internal class BatchUpdateContext {

        internal const string CLASS_NAME = "BatchUpadteContext";

        internal SourceFile Render() => new SourceFile {
            FileName = "BatchUpdateContext.cs",
            RenderContent = context => {
                return $$"""
                    namespace {{context.Config.RootNamespace}};

                    /// <summary>
                    /// 一括更新処理のコンテキスト引数。
                    /// エラーメッセージや確認メッセージなどを書きやすくするためのもの。
                    /// </summary>
                    public partial class {{CLASS_NAME}} {
                        public {{CLASS_NAME}}(bool ignoreConfirm) {
                            IgnoreConfirm = ignoreConfirm;
                        }

                        /// <summary>
                        /// 更新処理を実行してもよいかどうかをユーザーに問いかけるメッセージを追加します。
                        /// ボタンの意味を統一してユーザーが混乱しないようにするため、
                        /// 「はい(Yes)」を選択したときに処理が続行され、
                        /// 「いいえ(No)」を選択したときに処理が中断されるような文言にしてください。
                        /// </summary>
                        void AddConfirm(string message);

                        /// <summary>
                        /// trueの場合、 <see cref="AddConfirm" /> による警告があっても更新処理が続行されます。
                        /// 画面側で警告に対して「はい(Yes)」が選択されたあとのリクエストではこの値がtrueになります。
                        /// </summary>
                        bool IgnoreConfirm { get; }
                    }
                    """;
            },
        };
    }
}
