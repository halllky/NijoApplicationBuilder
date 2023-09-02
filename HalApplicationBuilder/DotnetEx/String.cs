using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HalApplicationBuilder.DotnetEx {
    public static class StringExtension {
        public static string Join(this IEnumerable<string> values, string separator) => string.Join(separator, values);
        public static string ToCSharpSafe(this string str) => new Regex("[^\\w\\sぁ-んァ-ン一-龯]").Replace(str, string.Empty);
        public static string ToFileNameSafe(this string str) {
            foreach (var c in System.IO.Path.GetInvalidFileNameChars()) {
                str = str.Replace(c, '_');
            }
            return str;
        }
        public static string SelectTextTemplate<T>(this IEnumerable<T> values, Func<T, string> selector) {
            return values.Select(selector).Join(Environment.NewLine);
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
    }
}

