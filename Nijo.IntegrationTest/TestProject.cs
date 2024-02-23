using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Nijo.IntegrationTest {
    /// <summary>
    /// 自動テストで作成されたプロジェクト。テスト全体で共有する
    /// </summary>
    [SetUpFixture]
    public class TestProject {

#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
        public static GeneratedProject Current { get; private set; }
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。

        #region SETUP
        [OneTimeSetUp]
        public void SetUp() {
            var serviceProvider = Util.ConfigureServices();
            var logger = serviceProvider.GetRequiredService<ILogger>();

            // 出力が文字化けするので
            Console.OutputEncoding = Encoding.UTF8;

            // 依存先パッケージのインストールにかかる時間とデータ量を削減するために全テストで1つのディレクトリを共有する
            const string DIR_NAME = "自動テストで作成されたプロジェクト";
            var dir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", DIR_NAME));

            var initialized = Directory.Exists(dir)
                && (Directory.GetFiles(dir).Length >= 1
                || Directory.GetDirectories(dir).Length >= 1);
            logger.LogInformation("getfiles: {0}", Directory.GetFiles(dir).Length);
            logger.LogInformation("getdirs : {0}", Directory.GetDirectories(dir).Length);

            if (initialized) {
                Current = GeneratedProject.Open(dir, serviceProvider, logger);

            } else {
                Current = GeneratedProject.Create(
                    dir,
                    DIR_NAME,
                    keepTempIferror: true,
                    serviceProvider: serviceProvider,
                    log: logger,
                    initGitRepository: false);

                // デバッグ用スクリプトの生成（ダブルクリックで自動テストプロジェクト起動できるようにするもの）
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    var debugCommand = Path.Combine(dir, "DEBUG.command");
                    File.WriteAllText(debugCommand, $$"""
                        cd `dirname $0`
                        dotnet build ../Nijo
                        ../Nijo/bin/Debug/net8.0/nijo debug .
                        """, Encoding.UTF8);
                }
            }
        }

        [OneTimeTearDown]
        public void TearDown() {

        }
        #endregion SETUP


        #region LAUNCH
        /// <summary>
        /// 実行中のテスト用プロジェクトをWebから操作する機構を作成します。
        /// </summary>
        public static IWebDriver CreateWebDriver() {
            var exeDir = Assembly.GetExecutingAssembly().Location;
            var driver = new ChromeDriver(exeDir);
            try {

                // トップページに移動する
                var root = Current.GetDebuggingClientUrl();
                driver.Navigate().GoToUrl(root);

                return driver;

            } catch {
                driver.Dispose();
                throw;
            }
        }
        #endregion LAUNCH
    }
}
