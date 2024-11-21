using Nijo.Core;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.BothOfClientAndServer {
    internal class FromTo {
        internal const string CLASSNAME = "FromTo";
        internal const string FROM = "From";
        internal const string TO = "To";

        internal const string FROM_TS = "from";
        internal const string TO_TS = "to";

        internal static SourceFile Render(CodeRenderingContext ctx) => new SourceFile {
            FileName = $"FromTo.cs",
            RenderContent = context => $$"""
                using System;
                using System.Text.Json.Serialization;

                namespace {{ctx.Config.RootNamespace}} {
                    public partial class {{CLASSNAME}} {
                        [JsonPropertyName("{{FROM_TS}}")]
                        public virtual object? {{FROM}} { get; set; }
                        [JsonPropertyName("{{TO_TS}}")]
                        public virtual object? {{TO}} { get; set; }
                    }
                    public partial class {{CLASSNAME}}<T> : {{CLASSNAME}} {
                        [JsonPropertyName("{{FROM_TS}}")]
                        public new T {{FROM}} {
                            get => (T)base.{{FROM}}!;
                            set => base.{{FROM}} = value;
                        }
                        [JsonPropertyName("{{TO_TS}}")]
                        public new T {{TO}} {
                            get => (T)base.{{TO}}!;
                            set => base.{{TO}} = value;
                        }
                    }
                }
                """,
        };
    }
}
