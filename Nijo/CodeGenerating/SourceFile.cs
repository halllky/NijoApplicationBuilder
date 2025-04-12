using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.CodeGenerating {
    /// <summary>
    /// 自動生成されるソースコードのファイル1個分
    /// </summary>
    public class SourceFile {
        /// <summary>
        /// 自動生成されるソースコードのファイル1個分を表すクラスを作成します。
        /// </summary>
        /// <param name="callerFilePath">このソースファイルがどのファイルから生成されたか（コンパイル時に自動的に設定される）</param>
        /// <param name="callerMemberName">このソースファイルがどのメソッドから生成されたか（コンパイル時に自動的に設定される）</param>
        public SourceFile([CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null) {
            CallerFilePath = callerFilePath;
            CallerMemberName = callerMemberName;
        }

        /// <summary>このソースファイルがどのファイルから生成されたか</summary>
        public string? CallerFilePath { get; }
        /// <summary>このソースファイルがどのメソッドから生成されたか</summary>
        public string? CallerMemberName { get; }

        /// <summary>生成されるファイルの名前</summary>
        public required string FileName { get; init; }
        /// <summary>生成されるファイルの内容</summary>
        public required string Contents { get; init; }

        internal void Render(string filepath) {
            var ext = Path.GetExtension(filepath).ToLower();
            var encoding = ext == ".cs" || ext == ".sql"
                ? Encoding.UTF8 // With BOM
                : new UTF8Encoding(false);
            var newLine = ext == ".cs" || ext == ".sql"
                ? "\r\n"
                : "\n";
            var comment = ext switch {
                ".sql" => "--",
                _ => "//",
            };
            using var sw = new StreamWriter(filepath, append: false, encoding) {
                NewLine = newLine,
            };

            sw.WriteLine($$"""
                {{comment}} このファイルは自動生成されました。このファイルの内容を直接書き換えても、次回の自動生成処理で上書きされるのでご注意ください。
                {{(CallerFilePath != null ? $"{comment} ※ このファイルを生成したクラス  : {Path.GetFileName(CallerFilePath)}" : "")}}
                {{(CallerMemberName != null ? $"{comment} ※ このファイルを生成したメソッド: {CallerMemberName}" : "")}}

                """);

            foreach (var line in Contents.Split(Environment.NewLine)) {
                if (line.Contains(SKIP_MARKER)) continue;
                sw.WriteLine(line);
            }
        }
    }
}
