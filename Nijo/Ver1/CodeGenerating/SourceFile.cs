using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.CodeGenerating {
    public class SourceFile {
        public required string FileName { get; init; }
        public required Func<CodeRenderingContext, string> RenderContent { get; init; }

        public static StreamWriter GetStreamWriter(string filepath) {
            var ext = Path.GetExtension(filepath).ToLower();
            var encoding = ext == ".cs"
                ? Encoding.UTF8 // With BOM
                : new UTF8Encoding(false);
            var newLine = ext == ".cs"
                ? "\r\n"
                : "\n";
            return new StreamWriter(filepath, append: false, encoding) {
                NewLine = newLine,
            };
        }
    }
}
