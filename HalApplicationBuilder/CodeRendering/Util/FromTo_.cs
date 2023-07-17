using HalApplicationBuilder.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Util {
    partial class FromTo : ITemplate {
        internal FromTo(Config config) {
            _namespace = config.RootNamespace;
        }
        private readonly string _namespace;

        internal const string CLASSNAME = "FromTo";
        internal const string FROM = "From";
        internal const string TO = "To";

        public string FileName => $"FromTo.cs";
    }
}
