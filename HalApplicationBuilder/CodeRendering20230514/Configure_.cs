using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514 {
    partial class Configure : ITemplate {
        internal Configure(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public string FileName => "HalappDefualtConfigurer.cs";
        private string RuntimeServerSettings => new Util.RuntimeSettings(_ctx).ServerSetiingTypeFullName;

    }
}
