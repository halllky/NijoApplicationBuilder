using HalApplicationBuilder.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Util {
    partial class DotnetExtensions : ITemplate {
        internal DotnetExtensions(Config config) {
            _namespace = config.RootNamespace;
        }
        private readonly string _namespace;

        public string FileName => $"DotnetExtensions.cs";
    }
}
