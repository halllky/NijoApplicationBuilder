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

            // テストプロジェクトが初期化されているかどうかの判定
            var initialized = File.Exists(Path.Combine(dir, "nijo.xml"));

            if (initialized) {
                Current = GeneratedProject.Open(dir, serviceProvider, logger);

            } else {
                // git clone した直後は「自動テストで作成されたプロジェクト」フォルダが空の状態で存在しているので
                if (Directory.Exists(dir)) Directory.Delete(dir, true);

                Current = GeneratedProject.Create(
                    dir,
                    DIR_NAME,
                    keepTempIferror: true,
                    serviceProvider: serviceProvider,
                    log: logger,
                    initGitRepository: false);

                // デバッグ用スクリプトの生成（ダブルクリックで自動テストプロジェクト起動できるようにするもの）
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    File.WriteAllText(Path.Combine(dir, "BUILD.cmd"), $$"""
                        @echo off
                        chcp 65001
                        cd %~dp0
                        dotnet build ..\Nijo
                        ..\Nijo\bin\Debug\net8.0\nijo.exe update . 
                        pause
                        """, new UTF8Encoding(false, false));

                    File.WriteAllText(Path.Combine(dir, "DEBUG.cmd"), $$"""
                        @echo off
                        chcp 65001
                        cd %~dp0
                        dotnet build ..\Nijo
                        ..\Nijo\bin\Debug\net8.0\nijo.exe debug . 
                        pause
                        """, new UTF8Encoding(false, false));

                    File.WriteAllText(Path.Combine(dir, "TSC.cmd"), $$"""
                        @echo off
                        cd %~dp0react
                        call npm run tsc
                        pause
                        """, new UTF8Encoding(false, false));
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    File.WriteAllText(Path.Combine(dir, "BUILD.command"), $$"""
                        cd `dirname $0`
                        dotnet build ../Nijo
                        ../Nijo/bin/Debug/net8.0/nijo update . 
                        """.Replace("\r\n", "\n"), new UTF8Encoding(false, false));

                    File.WriteAllText(Path.Combine(dir, "DEBUG.command"), $$"""
                        cd `dirname $0`
                        dotnet build ../Nijo
                        ../Nijo/bin/Debug/net8.0/nijo debug . 
                        """.Replace("\r\n", "\n"), new UTF8Encoding(false, false));

                    File.WriteAllText(Path.Combine(dir, "TSC.command"), $$"""
                        cd `dirname $0`
                        cd react
                        npm run tsc
                        """.Replace("\r\n", "\n"), new UTF8Encoding(false, false));
                }

                // Visual Studio Code 用設定
                var vscode = Path.Combine(dir, ".vscode");
                var settingsJson = Path.Combine(vscode, "settings.json");
                if (!Directory.Exists(vscode)) {
                    Directory.CreateDirectory(vscode);
                }
                if (!File.Exists(settingsJson)) {
                    File.WriteAllText(settingsJson, $$"""
                        {
                          "workbench.colorCustomizations": {
                            "titleBar.activeBackground": "#c17838",
                            "titleBar.activeForeground": "#000000"
                          }
                        }
                        """.Replace("\r\n", "\n"), new UTF8Encoding(false, false));
                }
            }
        }

        [OneTimeTearDown]
        public void TearDown() {

        }

        /// <summary>
        /// 自動テストで作成されるプロジェクトのソースコードを所定のデータパターンのもので置き換えます。
        /// </summary>
        internal static void UpdateAutoGeneratedCode(DataPattern dataPattern) {
            File.WriteAllText(Current.SchemaXml.GetPath(), dataPattern.LoadXmlString());
            Current.CodeGenerator.GenerateCode(new NijoCodeGenerator.CodeGenerateOptions {
                OverwriteConcreteAppSrvFile = true,
            });
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
                var root = Current.ReactProject.GetDebuggingClientUrl();
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
