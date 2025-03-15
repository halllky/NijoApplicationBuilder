using Nijo.Ver1.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Parts.Core {
    /// <summary>
    /// ユーザーとの対話が発生する何らかの処理で、
    /// 1回のリクエスト・レスポンスのサイクルの一連の情報を保持するコンテキスト情報
    /// </summary>
    internal class PresentationContext {

        internal const string INTERFACE_NAME = "IPresentationContext";

        internal static SourceFile RenderInterface() {
            return new SourceFile {
                FileName = "IPresentationContext.cs",
                Contents = $$"""
                    /// <summary>
                    /// ユーザーとの対話が発生する何らかの処理で、
                    /// 1回のリクエスト・レスポンスのサイクルの一連の情報を保持するコンテキスト情報
                    /// </summary>
                    public interface {{INTERFACE_NAME}} {
                        // TODO ver.1
                    }
                    """,
            };
        }
    }
}
