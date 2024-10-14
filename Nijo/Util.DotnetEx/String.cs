using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;
using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

namespace Nijo.Util.DotnetEx {
    public static class StringExtension {
        public static string Join(this IEnumerable<string> values, string separator) => string.Join(separator, values);
        public static string ToCSharpSafe(this string str) => new Regex("[^\\w\\sぁ-んァ-ン一-龯]").Replace(str, string.Empty);
        public static string ToFileNameSafe(this string str) {
            foreach (var c in System.IO.Path.GetInvalidFileNameChars()) {
                str = str.Replace(c, '_');
            }
            return str;
        }
        public static string ToUrlSafe(this string str) => System.Web.HttpUtility.UrlEncode(str);
        public static string ToKebabCase(this string str) => str.ToLower().Replace(" ", "-");
        public static string ToHashedString(this string str) {
            byte[] stringBytes = System.Text.Encoding.UTF8.GetBytes(str);
            byte[] hashedBytes = System.Security.Cryptography.MD5.Create().ComputeHash(stringBytes);
            byte[] guidBytes = new byte[16];
            Array.Copy(hashedBytes, 0, guidBytes, 0, 16);
            var guid = new Guid(guidBytes);
            return "x" + guid.ToString().Replace("-", "");
        }

        /// <summary>
        /// 半角文字を1、全角文字を2として横幅を算出する。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int CalculateCharacterWidth(this string str) {
            int totalWidth = 0;

            for (int i = 0; i < str.Length;) {
                var unicodeCategory = char.GetUnicodeCategory(str, i);

                if (unicodeCategory == UnicodeCategory.Surrogate || char.IsSurrogate(str[i])) {
                    totalWidth += 2; // サロゲートペア
                    i += 2;

                } else if (unicodeCategory == UnicodeCategory.OtherSymbol) {
                    totalWidth += 2; // 絵文字やIVS文字など
                    i += 2;

                } else if ((str[i] >= 0x3000 && str[i] <= 0xFF60)
                        || (str[i] >= 0xFFE0 && str[i] <= 0xFFE6)) {
                    totalWidth += 2; // 一般的な日本語の全角文字や記号
                    i += 1;

                } else {
                    totalWidth += 1;
                    i += 1;
                }
            }

            return totalWidth;
        }

        /// <summary>
        /// この文字列をJSONとして解釈し、型引数のインスタンスとして返します。
        /// </summary>
        public static T ParseAsJson<T>(this string str) {
            return JsonSerializer.Deserialize<T>(str, JsonSerializerOptions)
                ?? throw new InvalidOperationException("JSONパースに失敗しました。");
        }
        /// <summary>
        /// このオブジェクトをJSONシリアライズします。
        /// 日本語のエンコーディングをきちんとするなどの頻出するオプションの設定込み。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ConvertToJson<T>(this T obj) {
            return JsonSerializer.Serialize(obj, JsonSerializerOptions);
        }
        public static JsonSerializerOptions JsonSerializerOptions {
            get {
                if (_cachedOptions == null) {
                    _cachedOptions = new JsonSerializerOptions();
                    _cachedOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                    _cachedOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
                }
                return _cachedOptions;
            }
        }
        private static JsonSerializerOptions? _cachedOptions;
    }
}

