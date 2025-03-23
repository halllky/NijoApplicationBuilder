using Nijo.Ver1.CodeGenerating;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Parts.JavaScript {
    /// <summary>
    /// TypeScriptの "index.ts" ファイル
    /// </summary>
    internal class IndexTs {
        /// <summary>
        /// TypeScriptの "index.ts" ファイルをレンダリングします。
        /// 同ディレクトリ内の他のソースファイルが全てレンダリングされた後に呼ばれる必要があります。
        /// </summary>
        internal static void Render(DirectorySetupper dir, CodeRenderingContext ctx) {
            var imports = ctx
                .GetGeneratedFileNames(dir)
                .Select(path => Path.GetFileNameWithoutExtension(path));

            dir.Generate(new SourceFile {
                FileName = "index.ts",
                Contents = $$"""
                    {{imports.SelectTextTemplate(path => $$"""
                    export * from "./{{path.Replace("\"", "\\\"")}}"
                    """)}}
                    """,
            });
        }

    }
}
