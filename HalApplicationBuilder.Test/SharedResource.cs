using System;
namespace HalApplicationBuilder.Test {
    /// <summary>
    /// テスト全体で共有するリソース
    /// </summary>
    public class SharedResource : IDisposable {
        public SharedResource() {
            // 依存先パッケージのインストールにかかる時間とデータ量を削減するために全テストで1つのディレクトリを共有する
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "DIST_PROJECT");
            Project = Directory.Exists(dir)
                ? HalappProject.Open(dir, log: Console.Out)
                : HalappProject.Create(dir, "TestApplication", false, log: Console.Out);
        }

        public HalappProject Project { get; }

        public void Dispose() {

        }
    }
}
