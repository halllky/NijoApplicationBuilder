using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.Utility {
    internal class AggregateUpdateEvent {
        internal static SourceFile Render(CodeRenderingContext ctx) => new SourceFile {
            FileName = "AggregateUpdateEvent.cs",
            RenderContent = context => $$"""
                using System;
                using System.Collections;
                using System.Collections.Generic;

                namespace {{ctx.Config.RootNamespace}} {
                    public class AggregateUpdateEvent<T> : IEnumerable<T> {
                        public IReadOnlyCollection<T> Created { get; init; } = new HashSet<T>();
                        public IReadOnlyCollection<T> Deleted { get; init; } = new HashSet<T>();
                        public IReadOnlyCollection<AggregateBeforeAfter<T>> Modified { get; init; } = new HashSet<AggregateBeforeAfter<T>>();

                        IEnumerator IEnumerable.GetEnumerator() {
                            return ((IEnumerable<T>)this).GetEnumerator();
                        }
                        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
                            foreach (var item in Created) {
                                yield return item;
                            }
                            foreach (var item in Modified) {
                                yield return item.Before;
                                yield return item.After;
                            }
                            foreach (var item in Deleted) {
                                yield return item;
                            }
                        }
                    }
                    public class AggregateBeforeAfter<T> {
                        public required T Before { get; init; }
                        public required T After { get; init; }
                    }
                }
                """,
        };
    }
}
