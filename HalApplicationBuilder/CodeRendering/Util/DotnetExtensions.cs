using HalApplicationBuilder.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Util {
    partial class DotnetExtensions : TemplateBase {
        internal DotnetExtensions(Config config) {
            _namespace = config.RootNamespace;
        }
        private readonly string _namespace;

        public override string FileName => $"DotnetExtensions.cs";

        protected override string Template() {
            return $$"""
                namespace {{_namespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using System.Text.Json;
                    using Microsoft.AspNetCore.Mvc;

                    public static class DotnetExtensions {
                        public static IActionResult JsonContent<T>(this ControllerBase controller, T obj) {
                            var options = new JsonSerializerOptions {
                                // レスポンスに大文字が含まれるとき、大文字のまま返す。
                                // react hook form や ag-grid では大文字小文字を区別しているため
                                PropertyNameCaseInsensitive = true,
                            };
                            var json = JsonSerializer.Serialize(obj, options);

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
                """;
        }
    }
}
