using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebServer {
    /// <summary>
    /// <see cref="Models.ReadModel2"/> においてサーバーからクライアント側に返す読み取り専用情報のコンテナ。
    ///
    /// 編集できないときにその理由を明示するということを実装しやすくしている。
    /// </summary>
    internal class ReadOnlyInfo {
        internal const string CS_CLASS_NAME = "ReadOnlyInfo";
        internal const string TS_TYPE_NAME = "string"; // クライアント側には単に編集できない理由を表す文字列だけが返るため

        internal static SourceFile RenderCSharp() => new SourceFile {
            FileName = "ReadOnlyInfo.cs",
            RenderContent = context => {

                // TODO: SetReadOnly(bool, string?)
                // TODO: ReadOnlyBecause(string)

                return $$"""
                    namespace {{context.Config.RootNamespace}};

                    public sealed partial class {{CS_CLASS_NAME}} {

                    }
                    """;
            },
        };
    }
}
