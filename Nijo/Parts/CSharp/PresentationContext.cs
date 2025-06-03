using Nijo.CodeGenerating;
using Nijo.Parts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.CSharp {
    /// <summary>
    /// ユーザーとの対話が発生する何らかの処理で、
    /// 1回のリクエスト・レスポンスのサイクルの一連の情報を保持するコンテキスト情報
    /// </summary>
    internal class PresentationContext {

        internal const string INTERFACE = "IPresentationContext";
        internal const string OPTIONS = "IPresentationContextOptions";

        internal static SourceFile RenderStaticCore(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "IPresentationContext.cs",
                Contents = $$"""
                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// ユーザーとの対話が発生する何らかの処理で、
                    /// 1回のリクエスト・レスポンスのサイクルの一連の情報を保持するコンテキスト情報
                    /// </summary>
                    public interface {{INTERFACE}} {

                        /// <inheritdoc cref="{{OPTIONS}}"/>
                        {{OPTIONS}} Options { get; }

                        /// <summary>
                        /// パラメータの各値に対するメッセージ。エラーや警告など。
                        /// </summary>
                        {{MessageContainer.INTERFACE}} Messages { get; }

                        /// <summary>
                        /// 「～しますがよろしいですか？」などの確認メッセージを追加します。
                        /// </summary>
                        void AddConfirm(string text);

                        /// <summary>
                        /// 「～しますがよろしいですか？」などの確認メッセージが発生しているかどうかを返します。
                        /// </summary>
                        bool HasConfirm();

                        /// <summary>
                        /// キャストします。
                        /// メッセージコンテナの型が実際の型と異なる場合は例外を投げます。
                        /// </summary>
                        {{INTERFACE}}<TMessageRoot> Cast<TMessageRoot>() where TMessageRoot : {{MessageContainer.INTERFACE}};
                    }

                    /// <inheritdoc cref="{{INTERFACE}}"/>
                    /// <typeparam name="TMessageRoot">パラメータのメッセージ型</typeparam>
                    public interface {{INTERFACE}}<TMessageRoot> : {{INTERFACE}} where TMessageRoot : {{MessageContainer.INTERFACE}} {
                        /// <summary>
                        /// パラメータの各値に対するメッセージ。エラーや警告など。
                        /// </summary>
                        new TMessageRoot Messages { get; }
                    }

                    /// <summary>
                    /// <see cref="{{INTERFACE}}"/> のオプション
                    /// </summary>
                    public interface {{OPTIONS}} {
                        /// <summary>
                        /// Confirm（「～しますがよろしいですか？」の確認メッセージ）が発生しても無視するかどうか。
                        /// HTTPリクエストは「～しますがよろしいですか？」に対してOKが選択される前と後で計2回発生するが、
                        /// 1回目はfalse, 2回目はtrueになる。
                        /// </summary>
                        bool IgnoreConfirm { get; }
                    }
                    """,
            };
        }
    }
}
