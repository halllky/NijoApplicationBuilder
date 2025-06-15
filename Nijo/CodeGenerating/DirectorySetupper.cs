using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.CodeGenerating {
    /// <summary>
    /// ソースコード自動生成処理を直感的に書けるようにするためのクラス
    /// </summary>
    public class DirectorySetupper {
        internal static void StartSetup(CodeRenderingContext ctx, string absolutePath, Action<DirectorySetupper> fn) {
            var setupper = new DirectorySetupper(ctx, absolutePath);
            setupper.Directory("", fn);
        }
        private DirectorySetupper(CodeRenderingContext ctx, string path) {
            Path = path;
            _ctx = ctx;
        }

        internal string Path { get; }

        private readonly CodeRenderingContext _ctx;

        public void Directory(string relativePath, Action<DirectorySetupper> fn) {
            var fullpath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Path, relativePath));
            if (!System.IO.Directory.Exists(fullpath))
                System.IO.Directory.CreateDirectory(fullpath);
            _ctx.Handle(fullpath);

            fn(new DirectorySetupper(_ctx, System.IO.Path.Combine(Path, relativePath)));
        }

        public void Generate(SourceFile sourceFile) {
            var fullpath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Path, sourceFile.FileName));

            if (_ctx.IsHandled(fullpath)) {
                throw new InvalidOperationException($"同じファイルを2回以上レンダリングしようとしました: {fullpath}");
            }

            _ctx.Handle(fullpath);

            var dir = System.IO.Path.GetDirectoryName(fullpath);
            if (dir != null && !System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

            sourceFile.Render(fullpath);
        }
    }
}
