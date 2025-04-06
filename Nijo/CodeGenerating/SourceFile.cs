using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.CodeGenerating {
    public class SourceFile {
        public required string FileName { get; init; }
        public required string Contents { get; init; }

        internal void Render(string filepath) {
            var ext = Path.GetExtension(filepath).ToLower();
            var encoding = ext == ".cs" || ext == ".sql"
                ? Encoding.UTF8 // With BOM
                : new UTF8Encoding(false);
            var newLine = ext == ".cs" || ext == ".sql"
                ? "\r\n"
                : "\n";
            using var sw = new StreamWriter(filepath, append: false, encoding) {
                NewLine = newLine,
            };

            foreach (var line in Contents.Split(Environment.NewLine)) {
                if (line.Contains(SKIP_MARKER)) continue;
                sw.WriteLine(line);
            }
        }
    }
}
