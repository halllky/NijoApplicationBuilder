using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebServer {
    internal class RegexCache {

        internal const string CLASS_NAME = "RegexCache";
        internal const string ONLY_ALPHA_NUMERIC = "OnlyAlphaNumeric";

        internal static SourceFile Render() {
            return new SourceFile {
                FileName = "RegexCache.cs",
                RenderContent = ctx => {
                    return $$"""
                        using System;
                        using System.Collections.Generic;
                        using System.Linq;
                        using System.Text;
                        using System.Threading.Tasks;

                        namespace {{ctx.Config.RootNamespace}};

                        /// <summary>
                        /// 正規表現のキャッシュのようなもの。
                        /// ソースコード中で直接Regexのインスタンスを生成する場合と比較して実行時のパフォーマンスに優れる。
                        /// </summary>
                        public static partial class {{CLASS_NAME}} {

                            /// <summary>
                            /// 半角英数字のみ、スペースも記号も含まない文字列か否かを判定します。
                            /// </summary>
                            [System.Text.RegularExpressions.GeneratedRegex("^[a-zA-Z0-9]+$")]
                            public static partial System.Text.RegularExpressions.Regex {{ONLY_ALPHA_NUMERIC}}();
                        }
                        """;
                },
            };
        }

    }
}
