using HalApplicationBuilder.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Features.Util {
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
                            var json = {{Utility.CLASSNAME}}.{{Utility.TO_JSON}}(obj);
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
