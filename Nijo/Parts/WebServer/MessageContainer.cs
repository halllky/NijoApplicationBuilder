using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebServer {
    /// <summary>
    /// <see cref="Models.ReadModel2"/> においてサーバーからクライアント側に返すエラーメッセージ等のコンテナ。
    /// 
    /// <code>インスタンス.プロパティ名.AddError(メッセージ)</code> のように直感的に書ける、
    /// 無駄なペイロードを避けるためにメッセージが無いときはJSON化されない、といった性質を持つ。
    /// </summary>
    internal class MessageContainer {
        internal const string CS_CLASS_NAME = "MessageContainer";
        internal const string TS_TYPE_NAME = "{ type: 'error' | 'info', text: string }[]";

        internal static SourceFile RenderCSharp() => new SourceFile {
            FileName = "MessageContainer.cs",
            RenderContent = context => {

                // TODO: AddError, AddInfo
                // TODO: メッセージが空ならJSON化しないようにする

                return $$"""
                    namespace {{context.Config.RootNamespace}};

                    public sealed partial class {{CS_CLASS_NAME}} {

                    }
                    """;
            },
        };
    }
}
