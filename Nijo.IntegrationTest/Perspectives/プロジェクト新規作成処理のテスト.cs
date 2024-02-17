using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            services.AddTransient<IPackageInstaller>(_ => {
                return new PackageInstallerForCreateTest(logger);
            });

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


        /// <summary>
        /// 新規作成処理のたびにnpn ci による大量のパッケージのインストールが発生して
        /// 通信量を圧迫するのを防ぐため、ローカルにインストール済みのnode_modulesを利用するための仕組み
        /// </summary>
        private class PackageInstallerForCreateTest : IPackageInstaller {
            public PackageInstallerForCreateTest(ILogger logger) {
                _logger = logger;
            }
            private readonly ILogger _logger;

            public async Task InstallDependencies(GeneratedProject project, CancellationToken cancellationToken) {

                var reactTemplateDir = Path.GetFullPath(Path.Combine(
                    TestContext.CurrentContext.WorkDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "Nijo.ApplicationTemplates",
                    "REACT_AND_WEBAPI",
                    "react"));

                var nodeModules = Path.GetFullPath(Path.Combine(
                    project.WebClientProjectRoot,
                    "node_modules"));

                if (Directory.Exists(nodeModules)) {
                    _logger.LogInformation("node_modulesフォルダが既に存在するためインストールをスキップします。");

                } else if (NodeModulesIsInstalled(reactTemplateDir)) {

                    // reactテンプレートのnode_modulesをコピーする
                    var reactTemplateDirNodeModules = Path.GetFullPath(Path.Combine(
                        reactTemplateDir,
                        "node_modules"));
                    _logger.LogInformation("npm ci のかわりに右記ディレクトリからのコピーを行います: {0}", reactTemplateDirNodeModules);

                    var process = new Process();
                    process.StartInfo.FileName = "robocopy";
                    process.StartInfo.ArgumentList.Add("/S");
                    process.StartInfo.ArgumentList.Add("/NFL"); // No File List
                    process.StartInfo.ArgumentList.Add("/NDL"); // No Directory List
                    process.StartInfo.ArgumentList.Add("/NJH"); // No Job Header
                    process.StartInfo.ArgumentList.Add("/NJS"); // No Job Summary
                    process.StartInfo.ArgumentList.Add(reactTemplateDirNodeModules);
                    process.StartInfo.ArgumentList.Add(nodeModules);

                    process.Start();

                    await process.WaitForExitAsync(TestContext.CurrentContext.CancellationToken);

                    // 戻り値1が正常終了
                    if (process.ExitCode != 1) {
                        throw new InvalidOperationException("robocopyが終了コード1以外で終了しました。");
                    }

                    _logger.LogInformation("コピーを完了しました。");

                } else {
                    // インストール済みでない場合は通常通り npm ci する
                    var defaultInstaller = new PackageInstaller();
                    await defaultInstaller.InstallDependencies(project, cancellationToken);
                }
            }

            private static bool NodeModulesIsInstalled(string dir) {

                // 念のため全く違うディレクトリのnode_modulesの有無を確認しようとしていないかを確認
                var packageJson = Path.Combine(dir, "package.json");
                if (!File.Exists(packageJson))
                    throw new InvalidOperationException($"Reactテンプレートプロジェクトではない場所のnode_moduleの有無を確認しようとしています: {dir}");

                var nodeModules = new DirectoryInfo(Path.Combine(dir, "node_modules"));
                if (!nodeModules.Exists) return false;

                var anyPackage = nodeModules.GetDirectories("*", SearchOption.TopDirectoryOnly).Any();
                if (!anyPackage) return false;

                return true;
            }
        }
    }
}
