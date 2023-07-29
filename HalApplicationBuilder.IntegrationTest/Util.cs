using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HalApplicationBuilder.IntegrationTest {
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
    }
}
