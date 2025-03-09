using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.CodeGenerating {
    /// <summary>
    /// ソースコード自動生成処理を直感的に書けるようにするためのクラス
    /// </summary>
    public class DirectorySetupper {
        internal static DirectorySetupper StartSetup(CodeRenderingContext ctx, string absolutePath) {
            return new DirectorySetupper(ctx, absolutePath);
        }
        internal static void StartSetup(CodeRenderingContext ctx, string absolutePath, Action<DirectorySetupper> fn) {
            var setupper = StartSetup(ctx, absolutePath);
            setupper.Directory("", fn);
        }
        private DirectorySetupper(CodeRenderingContext ctx, string path) {
            Path = path;
            _ctx = ctx;
        }

        internal string Path { get; }

        private readonly CodeRenderingContext _ctx;

        public void Directory(string relativePath, Action<DirectorySetupper> fn) {
            var fullpath = System.IO.Path.Combine(Path, relativePath);
            if (!System.IO.Directory.Exists(fullpath))
                System.IO.Directory.CreateDirectory(fullpath);
            _ctx.Handle(fullpath);

            fn(new DirectorySetupper(_ctx, System.IO.Path.Combine(Path, relativePath)));
        }

        public void Generate(SourceFile sourceFile) {
            var file = System.IO.Path.Combine(Path, sourceFile.FileName);
            _ctx.Handle(file);

            var dir = System.IO.Path.GetDirectoryName(file);
            if (dir != null && !System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

            using var sw = SourceFile.GetStreamWriter(file);

            foreach (var line in sourceFile.Contents.Split(Environment.NewLine)) {
                if (line.Contains(SKIP_MARKER)) continue;
                sw.WriteLine(line);
            }
        }

        public void CopyFrom(string copySourceFile) {
            var copyTargetFile = System.IO.Path.Combine(Path, System.IO.Path.GetFileName(copySourceFile));
            _ctx.Handle(copyTargetFile);

            var encoding = GetEncoding(copySourceFile);
            using var reader = new StreamReader(copySourceFile, encoding);
            using var writer = SourceFile.GetStreamWriter(copyTargetFile);
            while (!reader.EndOfStream) {
                writer.WriteLine(reader.ReadLine());
            }
        }

        private static Encoding GetEncoding(string filepath) {
            return System.IO.Path.GetExtension(filepath).ToLower() == ".cs"
                ? Encoding.UTF8 // With BOM
                : new UTF8Encoding(false);
        }
    }
}
