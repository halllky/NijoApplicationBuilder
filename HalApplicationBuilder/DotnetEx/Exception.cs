using System;
using System.Collections.Generic;
using System.Linq;

namespace HalApplicationBuilder.DotnetEx {
    public static class ExceptionExtensions {
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
