using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HalApplicationBuilder.DotnetEx {
    internal static class IO {
        internal static void CopyDirectory(string source, string dist) {

            if (!Directory.Exists(dist)) {
                Directory.CreateDirectory(dist);
                File.SetAttributes(dist, File.GetAttributes(source));
            }

            var sourceFiles = Directory.GetFiles(source);
            foreach (var sourceFile in sourceFiles) {
                var distFile = Path.Combine(dist, Path.GetFileName(sourceFile));
                File.Copy(sourceFile, distFile, true);
            }

            var childDirctories = Directory.GetDirectories(source);
            foreach (string sourceChildDir in childDirctories) {
                var distChildDir = Path.Combine(dist, Path.GetFileName(sourceChildDir));
                CopyDirectory(sourceChildDir, distChildDir);
            }
        }

        internal static FileStream OpenFileWithRetry(string path, TimeSpan? timeout = null) {
            var limit = DateTime.Now + (timeout ?? TimeSpan.FromSeconds(5));
            while (true) {
                try {
                    return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                } catch (IOException ex) when (ex.InnerException is null && ex.HResult == -2147024864) {
                    if (DateTime.Now > limit) throw;

                    // ファイルがロックされている場合、一定時間待機する
                    Thread.Sleep(100);
                }
            }
        }
    }
}
