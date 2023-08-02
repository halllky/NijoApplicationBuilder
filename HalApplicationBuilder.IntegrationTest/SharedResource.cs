using Microsoft.AspNetCore.Http;
using Microsoft.Build.Evaluation;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace HalApplicationBuilder.IntegrationTest {

    /// <summary>
    /// テスト全体で共有するリソース
    /// </summary>
    [SetUpFixture]
    public class SharedResource {

#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
        public static HalappProject Project { get; private set; }
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。

        #region SETUP
        [OneTimeSetUp]
        public void SetUp() {
            // 出力が文字化けするので
            Console.OutputEncoding = Encoding.UTF8;

            // 依存先パッケージのインストールにかかる時間とデータ量を削減するために全テストで1つのディレクトリを共有する
            const string DIR_NAME = "自動テストで作成されたプロジェクト";
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", DIR_NAME);
            Project = Directory.Exists(dir)
                ? HalappProject.Open(dir, log: Console.Out, verbose: true)
                : HalappProject.Create(dir, DIR_NAME, true, log: Console.Out, verbose: true);
        }

        [OneTimeTearDown]
        public void TearDown() {

        }
        #endregion SETUP
    }

    /// <summary>
    /// テスト用の糖衣構文
    /// </summary>
    public static class HalappProjectExtension {
        private const string CONN_STR = @"Data Source=""bin/Debug/debug.sqlite3""";

        /// <summary>
        /// DataPatternでソースコードやDB定義を更新してビルドをかけます。
        /// </summary>
        public static void Build(this HalappProject project, DataPattern pattern) {
            // halapp.xmlの更新
            File.WriteAllText(project.GetAggregateSchemaPath(), pattern.LoadXmlString());

            project.Build();

            // 実行時設定ファイルの作成
            var runtimeConfig = Path.Combine(project.ProjectRoot, "halapp-runtime-config.json");
            var json = new {
                currentDb = "SQLITE3",
                db = new[] {
                    new { name = "SQLITE3", connStr = CONN_STR },
                },
            }.ToJson();
            File.WriteAllText(runtimeConfig, json);

            // DB（前のテストで作成されたDBを削除）
            var migrationDir = Path.Combine(project.ProjectRoot, "Migrations");
            if (Directory.Exists(migrationDir)) {
                foreach (var file in Directory.GetFiles(migrationDir)) {
                    File.Delete(file);
                }
            }

            File.Delete(Path.Combine(project.ProjectRoot, "bin", "Debug", "debug.sqlite3"));
            File.Delete(Path.Combine(project.ProjectRoot, "bin", "Debug", "debug.sqlite3-shm"));
            File.Delete(Path.Combine(project.ProjectRoot, "bin", "Debug", "debug.sqlite3-wal"));

            // DB（このデータパターンの定義に従ったDBを作成）
            try {
                project.EnsureCreateDatabase();
            } catch (Exception ex) {
                throw new Exception("DB作成失敗", ex);
            }
        }

        /// <summary>
        /// テスト用プロジェクトにHTTPリクエストを送信し、結果を受け取ります。
        /// </summary>
        /// <param name="path">URLのうちドメインより後ろの部分</param>
        /// <returns>HTTPレスポンス</returns>
        public static async Task<HttpResponseMessage> Get(this HalappProject project, string path, Dictionary<string, string>? parameters = null) {
            var query = parameters == null
                ? string.Empty
                : $"?{await new FormUrlEncodedContent(parameters).ReadAsStringAsync()}";
            var uri = new Uri(project.GetDebugUrl(), path + query);

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
        public static async Task<HttpResponseMessage> Post(this HalappProject project, string path, object body) {
            var uri = new Uri(project.GetDebugUrl(), path);
            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            message.Content = new StringContent(body.ToJson(), Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            return await client.SendAsync(message);
        }
    }
}
