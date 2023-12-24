using Nijo.Core;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Util {
    internal class FromTo {
        internal const string CLASSNAME = "FromTo";
        internal const string FROM = "From";
        internal const string TO = "To";

        internal static SourceFile Render() => new SourceFile {
            FileName = $"FromTo.cs",
            RenderContent = ctx => $$"""
                using System;
                namespace {{ctx.Config.RootNamespace}} {
                    public partial class {{CLASSNAME}} {
                        public virtual object? {{FROM}} { get; set; }
                        public virtual object? {{TO}} { get; set; }
                    }
                    public partial class {{CLASSNAME}}<T> : {{CLASSNAME}} {
                        public new T? {{FROM}} {
                            get => (T?)base.{{FROM}};
                            set => base.{{FROM}} = value;
                        }
                        public new T? {{TO}} {
                            get => (T?)base.{{TO}};
                            set => base.{{TO}} = value;
                        }
                    }
                }
                """,
        };
    }
}
