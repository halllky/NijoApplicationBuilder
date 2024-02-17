using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nijo.IntegrationTest {
    internal static class Util {

        #region DI
        internal static IServiceProvider ConfigureServices() {
            var services = new ServiceCollection();

            GeneratedProject.ConfigureDefaultServices(services);

            services.AddSingleton<ILogger>(_ => {
                return new TestContextLogger();
            });
            services.AddTransient<IPackageInstaller>(provider => {
                var logger = provider.GetRequiredService<ILogger>();
                return new PackageInstallerForCreateTest(logger);
            });

            return services.BuildServiceProvider();
        }


        /// <summary>
        /// NUnitのテスト出力コンソールへのログ出力
        /// </summary>
        private class TestContextLogger : ILogger {

            public bool IsEnabled(LogLevel logLevel) {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
                var scope = string.Concat(_scope.Reverse().Select(x => $"{x} => "));
                TestContext.WriteLine($"{DateTime.Now:g}\t[{logLevel}]\t{scope}{formatter(state, exception)}");
            }

            #region スコープ
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
                _scope.Push(state.ToString() ?? string.Empty);
                return new Scope(this);
            }
            private class Scope(TestContextLogger owner) : IDisposable {
                private readonly TestContextLogger _owner = owner;
                private bool _disposed;

                public void Dispose() {
                    if (_disposed) return;
                    _owner._scope.Pop();
                    _disposed = true;
                }
            }
            private readonly Stack<string> _scope = new();
            #endregion スコープ
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
        #endregion DI

        internal static string ToJson(this object obj) {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                WriteIndented = true,
            });
        }

        internal static async Task<string> ReadAsJsonAsync(this HttpContent httpContent) {
            var str = await httpContent.ReadAsStringAsync();

            // テスト結果の比較に使うので、改行などを"ToJson"の結果と合わせる
            var obj = JsonSerializer.Deserialize<object>(str);
            return obj?.ToJson() ?? string.Empty;
        }

        internal static void AssertHttpResponseIsOK(HttpResponseMessage httpResponseMessage) {
            try {
                Assert.That(httpResponseMessage.IsSuccessStatusCode, Is.True);
            } catch {
                var task = httpResponseMessage.Content.ReadAsStringAsync();
                task.Wait();
                var text = task.Result;

                try {
                    // jsonなら整形してコンソール表示する
                    var jsonOption = new JsonSerializerOptions {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                        WriteIndented = true,
                    };
                    var obj = JsonSerializer.Deserialize<object>(text, jsonOption);
                    var json = JsonSerializer.Serialize(obj, jsonOption);
                    TestContext.Error.WriteLine(json);

                } catch (JsonException) {
                    TestContext.Error.WriteLine(text);
                }
                throw;
            }
        }

        #region Selenium
        internal static OpenQA.Selenium.By ByInnerText(string innerText) {
            var escaped = innerText.Replace("'", "\\'");
            return OpenQA.Selenium.By.XPath($"//*[contains(text(), '{escaped}')]");
        }
        #endregion Selenium
    }
}
