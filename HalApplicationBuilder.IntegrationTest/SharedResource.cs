using System;
using System.Text;

namespace HalApplicationBuilder.IntegrationTest {

    /// <summary>
    /// テスト全体で共有するリソース
    /// </summary>
    [SetUpFixture]
    public class SharedResource {

#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
        public static HalappProject Project { get; private set; }
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。

        [OneTimeSetUp]
        public void SetUp() {
            // 出力が文字化けするので
            Console.OutputEncoding = Encoding.UTF8;

            // 依存先パッケージのインストールにかかる時間とデータ量を削減するために全テストで1つのディレクトリを共有する
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "自動テストで作成されたプロジェクト");
            Project = Directory.Exists(dir)
                ? HalappProject.Open(dir, log: Console.Out, verbose: true)
                : HalappProject.Create(dir, "自動テストで作成されたプロジェクト", false, log: Console.Out, verbose: true);
        }

        [OneTimeTearDown]
        public void TearDown() {

        }
    }
}
