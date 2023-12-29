using Nijo.Core;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Architecture.Utility {
    internal class DotnetExtensions {

        internal static SourceFile Render(ICodeRenderingContext ctx) => new SourceFile {
            FileName = $"DotnetExtensions.cs",
            RenderContent = () => $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using System.Text.Json;
                    using Microsoft.AspNetCore.Mvc;

                    public static class DotnetExtensions {
                        public static IActionResult JsonContent<T>(this ControllerBase controller, T obj) {
                            var json = {{UtilityClass.CLASSNAME}}.{{UtilityClass.TO_JSON}}(obj);
                            return controller.Content(json, "application/json");
                        }
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
                    }
                }
                """,
        };
    }
}
