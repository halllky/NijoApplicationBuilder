using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Util {
    partial class HttpResponseExceptionFilter : ITemplate {
        internal HttpResponseExceptionFilter(string rootnamespace) {
            _namespace = rootnamespace;
        }

        private readonly string _namespace;
        public string FileName => "HttpResponseExceptionFilter.cs";
        public const string CLASSNAME = "HttpResponseExceptionFilter";
        public string ClassFullName => $"{_namespace}.{CLASSNAME}";
    }
}
