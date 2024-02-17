using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) {
                return true;
            }

            public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
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

        #region Selenium, Web
        /// <summary>
        /// 実行中のテスト用プロジェクトをWebから操作する機構を作成します。
        /// </summary>
        public static IWebDriver CreateWebDriver(this GeneratedProject _) {
            var exeDir = Assembly.GetExecutingAssembly().Location;
            var driver = new ChromeDriver(exeDir);
            try {

                // トップページに移動する
                var root = TestProject.Current.Debugger.GetDebuggingClientUrl();
                driver.Navigate().GoToUrl(root);

                return driver;

            } catch {
                driver.Dispose();
                throw;
            }
        }

        /// <summary>
        /// テスト用プロジェクトにHTTPリクエストを送信し、結果を受け取ります。
        /// </summary>
        /// <param name="path">URLのうちドメインより後ろの部分</param>
        /// <returns>HTTPレスポンス</returns>
        public static async Task<HttpResponseMessage> Get(this GeneratedProject project, string path, Dictionary<string, string>? parameters = null) {
            var query = parameters == null
                ? string.Empty
                : $"?{await new FormUrlEncodedContent(parameters).ReadAsStringAsync()}";
            var uri = new Uri(project.Debugger.GetDebugUrl(), path + query);

            var message = new HttpRequestMessage(HttpMethod.Get, uri);

            using var client = new HttpClient();
            return await client.SendAsync(message);
        }
        /// <summary>
        /// テスト用プロジェクトにHTTPリクエストを送信し、結果を受け取ります。
        /// </summary>
        /// <param name="path">URLのうちドメインより後ろの部分</param>
        /// <param name="body">リクエストボディ</param>
        /// <returns>HTTPレスポンス</returns>
        public static async Task<HttpResponseMessage> Post(this GeneratedProject project, string path, object body) {
            var uri = new Uri(project.Debugger.GetDebugUrl(), path);
            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            message.Content = new StringContent(body.ToJson(), Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            return await client.SendAsync(message);
        }
        /// <summary>
        /// テスト用プロジェクトにHTTPリクエストを送信し、結果を受け取ります。
        /// </summary>
        /// <param name="path">URLのうちドメインより後ろの部分</param>
        /// <returns>HTTPレスポンス</returns>
        public static async Task<HttpResponseMessage> Delete(this GeneratedProject project, string path) {
            var uri = new Uri(project.Debugger.GetDebugUrl(), path);
            var message = new HttpRequestMessage(HttpMethod.Delete, uri);

            using var client = new HttpClient();
            return await client.SendAsync(message);
        }

        /// <summary>
        /// テスト用データベースにSELECT文を発行します。
        /// </summary>
        public static IEnumerable<SqliteDataReader> ExecSql(this GeneratedProject project, string sql) {
            var dataSource = Path.GetFullPath(Path.Combine(project.ProjectRoot, $"DEBUG.sqlite3")).Replace("\\", "/");
            var connStr = new SqliteConnectionStringBuilder();
            connStr.DataSource = dataSource;
            connStr.Pooling = false;
            using var conn = new SqliteConnection(connStr.ToString());
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                yield return reader;
            }
        }

        internal static OpenQA.Selenium.By ByInnerText(string innerText) {
            var escaped = innerText.Replace("'", "\\'");
            return OpenQA.Selenium.By.XPath($"//*[contains(text(), '{escaped}')]");
        }
        #endregion Selenium, Web
    }
}
