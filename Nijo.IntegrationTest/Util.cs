using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Nijo.Util.DotnetEx;
using System.Diagnostics.CodeAnalysis;

namespace Nijo.IntegrationTest {
    internal static class Util {
        /// <summary>
        /// デバッグモードで作成される集約1個あたりのダミーデータの数
        /// </summary>
        internal static int DUMMY_DATA_COUNT = 4;

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
                TestContext.Out.WriteLine($"{DateTime.Now:g}\t[{logLevel}]\t{scope}{formatter(state, exception)}");
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
                    "Nijo.ApplicationTemplate",
                    "react"));
                var reactTemplateDirNodeModules = Path.Combine(
                    reactTemplateDir,
                    "node_modules");
                var testProjectNodeModules = Path.GetFullPath(Path.Combine(
                    project.ReactProject.ProjectRoot,
                    "node_modules"));

                // 自動テストプロジェクトのnode_modulesがインストール済みの場合
                if (Directory.Exists(testProjectNodeModules)) {
                    _logger.LogInformation("node_modulesフォルダが既に存在するためインストールをスキップします。");
                    return;
                }

                // raectテンプレートにnode_modulesがなければ npm ci
                // （git clone 直後はこの状態がありうる）
                if (!Directory.Exists(reactTemplateDirNodeModules)) {

                    // 念のため全く違うディレクトリのnode_modulesの有無を確認しようとしていないかを確認
                    var packageJson = Path.Combine(reactTemplateDir, "package.json");
                    if (!File.Exists(packageJson))
                        throw new InvalidOperationException($"Reactテンプレートプロジェクトではない場所のnode_moduleの有無を確認しようとしています: {reactTemplateDir}");

                    var npmCi = new Process();
                    try {
                        npmCi.StartInfo.WorkingDirectory = reactTemplateDir;
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                            npmCi.StartInfo.FileName = "powershell";
                            npmCi.StartInfo.Arguments = "/c \"npm ci\"";
                        } else {
                            npmCi.StartInfo.FileName = "npm";
                            npmCi.StartInfo.Arguments = "ci";
                        }

                        _logger.LogInformation("reactテンプレートプロジェクトの依存パッケージをインストールします。");
                        npmCi.Start();
                        await npmCi.WaitForExitAsync(cancellationToken);

                        if (npmCi.ExitCode != 0) {
                            throw new InvalidOperationException("npm ci が終了コード0以外で終了しました。");
                        }

                    } finally {
                        npmCi.EnsureKill();
                    }
                }

                // reactテンプレートのnode_modulesをコピー
                var process = new Process();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    process.StartInfo.FileName = "robocopy";
                    process.StartInfo.ArgumentList.Add("/S");
                    process.StartInfo.ArgumentList.Add("/NFL"); // No File List
                    process.StartInfo.ArgumentList.Add("/NDL"); // No Directory List
                    process.StartInfo.ArgumentList.Add("/NJH"); // No Job Header
                    process.StartInfo.ArgumentList.Add("/NJS"); // No Job Summary
                    process.StartInfo.ArgumentList.Add(reactTemplateDirNodeModules);
                    process.StartInfo.ArgumentList.Add(testProjectNodeModules);
                } else {
                    process.StartInfo.FileName = "rsync";
                    process.StartInfo.ArgumentList.Add("-atu");
                    process.StartInfo.ArgumentList.Add("--delete");
                    process.StartInfo.ArgumentList.Add(reactTemplateDirNodeModules);
                    process.StartInfo.ArgumentList.Add(Path.GetDirectoryName(testProjectNodeModules)!);
                }

                process.StartInfo.RedirectStandardError = true;
                process.ErrorDataReceived += (s, e) => {
                    TestContext.Out.WriteLine($"ERROR!!: {e.Data}");
                };

                _logger.LogInformation("npm ci のかわりに右記ディレクトリからのコピーを行います: {dirName}", reactTemplateDirNodeModules);
                process.Start();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(TestContext.CurrentContext.CancellationToken);

                // robocopyは戻り値1が正常終了
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && process.ExitCode != 1) {
                    throw new InvalidOperationException($"robocopyが終了コード1以外で終了しました: {process.ExitCode} ({reactTemplateDirNodeModules} => {testProjectNodeModules})");
                }

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && process.ExitCode != 0) {
                    throw new InvalidOperationException($"rsyncが終了コード0以外で終了しました: {process.ExitCode} ({reactTemplateDirNodeModules} => {Path.GetDirectoryName(testProjectNodeModules)})");
                }

                _logger.LogInformation("コピーを完了しました。");
            }
        }
        #endregion DI


        #region NUnit
        internal static string ToJson(this object obj) {
            var options = _cachedOptions ??= new JsonSerializerOptions {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                WriteIndented = true,
            };
            return JsonSerializer.Serialize(obj, options);
        }
        private static JsonSerializerOptions? _cachedOptions;

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

        internal static async Task WaitUntil(Func<bool> checker, TimeSpan? timeout = null) {
            var to = timeout ?? TimeSpan.FromSeconds(30);
            var current = TimeSpan.Zero;
            var interval = TimeSpan.FromSeconds(1);
            while (current <= to) {
                var ok = checker();
                if (ok) return;

                await Task.Delay(interval, TestContext.CurrentContext.CancellationToken);
                current += interval;
            }
            throw new TimeoutException();
        }
        #endregion NUnit


        #region Selenium, Web
        /// <summary>
        /// テスト用プロジェクトにHTTPリクエストを送信し、結果を受け取ります。
        /// </summary>
        /// <param name="path">URLのうちドメインより後ろの部分</param>
        /// <returns>HTTPレスポンス</returns>
        public static async Task<HttpResponseMessage> Get(this GeneratedProject project, string path, Dictionary<string, string>? parameters = null) {
            var query = parameters == null
                ? string.Empty
                : $"?{await new FormUrlEncodedContent(parameters).ReadAsStringAsync()}";
            var uri = new Uri(project.WebApiProject.GetDebugUrl(), path + query);

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
            var uri = new Uri(project.WebApiProject.GetDebugUrl(), path);
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
            var uri = new Uri(project.WebApiProject.GetDebugUrl(), path);
            var message = new HttpRequestMessage(HttpMethod.Delete, uri);

            using var client = new HttpClient();
            return await client.SendAsync(message);
        }

        /// <summary>
        /// テスト用データベースにSELECT文を発行します。
        /// </summary>
        public static IEnumerable<SqliteDataReader> ExecSql(this GeneratedProject project, string sql) {
            var dataSource = Path.GetFullPath(Path.Combine(project.SolutionRoot, $"DEBUG.sqlite3")).Replace("\\", "/");
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
            return OpenQA.Selenium.By.XPath($"//*[text()='{escaped}']");
        }
        internal static OpenQA.Selenium.By ByValue(string value) {
            var escaped = value.Replace("'", "\\'");
            return OpenQA.Selenium.By.XPath($"//input[@value='{escaped}']");
        }

        /// <summary>
        /// DOM構造が特殊なDescriptionのtextareaを選択する
        /// </summary>
        internal static OpenQA.Selenium.IWebElement ActivateTextarea(this OpenQA.Selenium.IWebDriver driver, string name) {
            driver.FindElement(OpenQA.Selenium.By.Name(name)).Click();
            return driver.SwitchTo().ActiveElement();
        }
        /// <summary>
        /// 画面のスクリーンショットをとる
        /// </summary>
        internal static OpenQA.Selenium.Screenshot CaptureScreenShot(this OpenQA.Selenium.IWebDriver driver) {
            driver.Manage().Window.FullScreen(); // 画面サイズによる表示差異が出ないようにする
            return (driver as OpenQA.Selenium.ITakesScreenshot)!.GetScreenshot();
        }
        /// <summary>
        /// 画面から操作してデータを初期化
        /// </summary>
        internal static async Task InitializeData(this OpenQA.Selenium.IWebDriver driver) {
            driver.FindElement(ByInnerText("DBを再作成する")).Click();
            driver.SwitchTo().Alert().Accept();
            await WaitUntil(() => driver.FindElements(ByInnerText("DBを再作成しました。")).Count > 0);
            driver.CancelLocalRepositoryChanges();
        }
        /// <summary>
        /// MultiViewからSingleView(新規作成)に遷移する
        /// </summary>
        internal static async Task NavigateToCreateView(this OpenQA.Selenium.IWebDriver driver) {
            // 初期データ読み込みのため一瞬待つ
            await Task.Delay(TimeSpan.FromSeconds(1));

            // 「追加」前のhrefを記憶しておく
            var hrefs = driver
                .FindElements(ByInnerText("詳細"))
                .Select(a => a.GetAttribute("href"))
                .ToArray();

            driver.FindElement(ByInnerText("新規作成")).Click();
        }
        /// <summary>
        /// MultiViewで一時保存する
        /// </summary>
        internal static async Task SaveInMultiView(this OpenQA.Selenium.IWebDriver driver) {
            // c0f3693a1f13a16762f2e9509ea8ccf3bf279890 時点では自動保存されない仕様なので明示的にクリック
            driver
                .FindElements(ByInnerText("一時保存"))
                .Skip(1)
                .Single()
                .Click();
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        /// <summary>
        /// SingleViewで保存する
        /// </summary>
        internal static async Task SaveInSingleView(this OpenQA.Selenium.IWebDriver driver) {
            driver
                .FindElements(ByInnerText("保存"))
                .Single()
                .Click();
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        /// <summary>
        /// MultiViewからEditViewに画面遷移させる。
        /// 検索結果が1件だけになるような検索条件が入力済みの前提
        /// </summary>
        internal static async Task SearchSingleAndNavigateToEditView(this OpenQA.Selenium.IWebDriver driver) {
            // 再検索
            driver.FindElement(ByInnerText("再読み込み")).Click();
            await Task.Delay(TimeSpan.FromSeconds(1)); // 再建策前の「詳細」がヒットしてしまうのを防ぐ
            await WaitUntil(() => driver.FindElements(ByInnerText("詳細")).Count > 0);

            // 画面遷移
            var elements = driver.FindElements(ByInnerText("詳細"));
            if (elements.Count >= 2) throw new InvalidOperationException("検索結果は1件のみになる想定のところ、2件以上あります。");
            elements.Single().Click();
        }
        /// <summary>
        /// ローカルリポジトリの変更をコミットする
        /// </summary>
        internal static void CommitLocalRepositoryChanges(this OpenQA.Selenium.IWebDriver driver) {
            driver.FindElement(ByInnerText("一時保存")).Click();
            driver.FindElement(ByInnerText("確定")).Click();
            driver.SwitchTo().Alert().Accept();
        }
        /// <summary>
        /// ローカルリポジトリの変更をキャンセルする
        /// </summary>
        internal static void CancelLocalRepositoryChanges(this OpenQA.Selenium.IWebDriver driver) {
            driver.FindElement(ByInnerText("一時保存")).Click();
            driver.FindElement(ByInnerText("取り消し")).Click();
            driver.SwitchTo().Alert().Accept();
        }
        #endregion Selenium, Web
    }
}

namespace Nijo.IntegrationTest.Tests {
    [NonParallelizable]
    public partial class 観点 {
    }
}
