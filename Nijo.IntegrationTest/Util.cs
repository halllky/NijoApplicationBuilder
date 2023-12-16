using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nijo.IntegrationTest {
    internal static class Util {

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
                Assert.True(httpResponseMessage.IsSuccessStatusCode);
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
