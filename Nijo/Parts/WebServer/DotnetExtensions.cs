using Nijo.Core;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebServer {
    /// <summary>
    /// 実行時の拡張メソッド。stringなど、.NETの基本クラスに対する拡張メソッドを記述することを想定。
    /// </summary>
    internal class DotnetExtensions {

        internal static SourceFile RenderCoreLibrary() => new SourceFile {
            FileName = $"DotnetExtensions.cs",
            RenderContent = context => $$"""
                namespace {{context.Config.RootNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using System.Text.Json;

                    public static class DotnetExtensions {

                        /// <summary>
                        /// 例外オブジェクトのメッセージを列挙します。InnerExceptionsも考慮します。
                        /// </summary>
                        public static IEnumerable<string> GetMessagesRecursively(this Exception ex, string indent = "") {
                            yield return indent + ex.Message;

                            if (ex is AggregateException aggregateException) {
                                var innerExceptions = aggregateException.InnerExceptions
                                    .SelectMany(inner => inner.GetMessagesRecursively(indent + "  "));
                                foreach (var inner in innerExceptions) {
                                    yield return inner;
                                }
                            }

                            if (ex.InnerException != null) {
                                foreach (var inner in ex.InnerException.GetMessagesRecursively(indent + "  ")) {
                                    yield return inner;
                                }
                            }
                        }

                        /// <summary>
                        /// 文字列の最大長を検査します。
                        /// サロゲートペアや結合文字列も1文字としてカウントします。
                        /// </summary>
                        public static bool IsStringWithinLimit(this string? str, int maxLength) {
                            if (str == null) return true;
                            var stringInfo = new System.Globalization.StringInfo(str);
                            return stringInfo.LengthInTextElements <= maxLength;
                        }
                    }
                }
                """,
        };

        internal static SourceFile RenderToWebApiProject() => new SourceFile {
            FileName = $"DotnetExtensions.cs",
            RenderContent = context => $$"""
                namespace {{context.Config.RootNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using System.Text.Json;
                    using Microsoft.AspNetCore.Mvc;

                    public static class DotnetExtensionsInWebApi {
                        public static IActionResult JsonContent<T>(this ControllerBase controller, T obj) {
                            var json = {{UtilityClass.CLASSNAME}}.{{UtilityClass.TO_JSON}}(obj);
                            var result = controller.Content(json, "application/json");
                            result.StatusCode = (int?)System.Net.HttpStatusCode.OK;
                            return result;
                        }
                    }
                }
                """,
        };
    }
}
