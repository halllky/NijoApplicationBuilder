using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Util {
    partial class DefaultLogger : ITemplate {
        internal DefaultLogger(string @namespace) {
            _namespace = @namespace;
        }
        private readonly string _namespace;
        public string FileName => "DefaultLogger.cs";
        internal const string CLASSNAME = "DefaultLogger";
    }
}
