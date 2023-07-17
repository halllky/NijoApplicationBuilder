using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Util {
    partial class InstanceKey : ITemplate {
        internal InstanceKey(CodeRenderingContext ctx) {
            _ctx = ctx;
            _namespace = ctx.Config.RootNamespace;
        }
        private readonly CodeRenderingContext _ctx;

        private readonly string _namespace;

        public string FileName => "InstanceKey.cs";

        internal const string CLASS_NAME = "InstanceKey";
        internal const string CREATE = "Create";
        internal const string TRY_PARSE = "TryParse";
        internal const string OBJECT_ARRAY = "ObjectArray";
    }
}
