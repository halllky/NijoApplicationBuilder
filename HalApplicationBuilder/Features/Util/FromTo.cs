using HalApplicationBuilder.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Features.Util {
    partial class FromTo : TemplateBase {
        internal FromTo(Config config) {
            _namespace = config.RootNamespace;
        }
        private readonly string _namespace;

        internal const string CLASSNAME = "FromTo";
        internal const string FROM = "From";
        internal const string TO = "To";

        public override string FileName => $"FromTo.cs";

        protected override string Template() {
            return $$"""
                using System;
                namespace {{_namespace}} {
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
                """;
        }
    }
}
