using System;
using System.Text.RegularExpressions;

namespace HalApplicationBuilder.DotnetEx {
    public static class StringExtension {

        public static string ToCSharpSafe(this string str) => new Regex("[^\\w\\sぁ-んァ-ン一-龯]").Replace(str, string.Empty);
    }
}

