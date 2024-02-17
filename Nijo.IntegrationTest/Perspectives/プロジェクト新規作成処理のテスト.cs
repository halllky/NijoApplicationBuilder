using Microsoft.Extensions.DependencyInjection;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.IntegrationTest.Perspectives {
    internal class プロジェクト新規作成処理のテスト {
        [Test, CancelAfter(300000)]
        public async Task 正常終了するか() {
            var logger = new TestContextLogger();

            const string PROJECT_NAME = "プロジェクト新規作成処理のテスト";
            var projectDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, PROJECT_NAME);

            // 作成先フォルダがある場合は削除する
            if (Directory.Exists(projectDir)) {
                // .gitフォルダ下のファイルは読み取り専用なのでアクセス権限を変更してから削除する
                var dirInfo = new DirectoryInfo(projectDir) { Attributes = FileAttributes.Normal };
                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories)) {
                    file.Attributes = FileAttributes.Normal;
                }
                dirInfo.Delete(true);
            }

            // 作成先フォルダがないことを確認
            if (Directory.Exists(projectDir)) Assert.Fail("新規作成しようとしている位置にディレクトリが既に存在します。");

            var services = new ServiceCollection();
            GeneratedProject.ConfigureDefaultServices(services);
            var serviceProvider = services.BuildServiceProvider();

            //// nijo create コマンドを実行
            //var terminal = new Terminal(TestContext.CurrentContext.WorkDirectory, logger);
            //await terminal.Run(new[] {
            //    ".\\nijo.exe", "create", PROJECT_NAME
            //}, TestContext.CurrentContext.CancellationToken);

            // nijo create コマンドを実行（デバッグしたい場合）
            GeneratedProject.Create(
                projectDir,
                PROJECT_NAME,
                true,
                serviceProvider,
                TestContext.CurrentContext.CancellationToken,
                logger);

            var project = GeneratedProject.Open(
                projectDir,
                serviceProvider,
                logger);

            // とりあえずもっとも単純なパターンで書き換えてみる
            // (デフォルトでもバックグランドタスク用のテーブルが生成されているはずなので
            // 敢えて集約定義を書かなくても十分判定可能かもしれない)
            var test1xml = new DataPattern(E_DataPattern._000_単純な集約).LoadXmlString();
            File.WriteAllText(project.SchemaXml.GetPath(), test1xml);

            // 何らかのDBアクセスする処理が正常終了するかを確認
            using var ts = CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.CurrentContext.CancellationToken);
            await project.Debugger.StartDebugging(ts.Token);
            using var webDriver = project.CreateWebDriver();

            // 設定画面にあるデバッグ用の「DB更新」というラベルのボタンを押してDB再作成。
            // デフォルトでダミーデータ4個が一緒に作成されるオプションのためこのタイミングでデータも一緒に作られる
            webDriver.FindElement(Util.ByInnerText("設定")).Click();
            webDriver.FindElement(Util.ByInnerText("DB更新")).Click();

            // DB作成が正常終了していればダミーデータ4個分のリンクがあるはず
            webDriver.FindElement(Util.ByInnerText("集約A")).Click();
            Assert.That(webDriver.FindElements(Util.ByInnerText("詳細")).Count, Is.EqualTo(4));
        }
    }
}
