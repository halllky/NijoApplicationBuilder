using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HalApplicationBuilder.DotnetEx {
    public static class StringExtension {
        public static string Join(this IEnumerable<string> values, string separator) => string.Join(separator, values);
        public static string ToCSharpSafe(this string str) => new Regex("[^\\w\\sぁ-んァ-ン一-龯]").Replace(str, string.Empty);
    }
}

