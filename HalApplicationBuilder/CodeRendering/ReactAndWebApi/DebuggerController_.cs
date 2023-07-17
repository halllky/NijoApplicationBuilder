using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.ReactAndWebApi {
    partial class DebuggerController : ITemplate {
        internal DebuggerController(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        internal string RuntimeServerSettings => new Util.RuntimeSettings(_ctx).ServerSetiingTypeFullName;

        public string FileName => $"HalappDebugger.cs";
    }
}
