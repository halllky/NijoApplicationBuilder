using Microsoft.AspNetCore.Http;
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

        /// <summary>
        /// テスト用プロジェクトにHTTPリクエストを送信し、結果を受け取ります。
        /// </summary>
        /// <param name="path">URLのうちドメインより後ろの部分</param>
        /// <returns>HTTPレスポンス</returns>
        public static async Task<HttpResponseMessage> HttpGet(string path, Dictionary<string, string>? parameters = null) {
            var query = parameters == null
                ? string.Empty
                : $"?{await new FormUrlEncodedContent(parameters).ReadAsStringAsync()}";
            var uri = new Uri(Project.GetDebugUrl(), path + query);

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
        public static async Task<HttpResponseMessage> HttpPost(string path, object body) {
            var uri = new Uri(Project.GetDebugUrl(), path);
            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
            });
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            return await client.SendAsync(message);
        }

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
}
