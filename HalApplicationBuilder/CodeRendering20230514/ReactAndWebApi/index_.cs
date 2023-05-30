using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.ReactAndWebApi {
#pragma warning disable IDE1006 // 命名スタイル
    partial class index : ITemplate {
#pragma warning restore IDE1006 // 命名スタイル

        internal index(CodeRenderingContext ctx, string reactPageDir) {
            _ctx = ctx;
            _reactPageDir = reactPageDir;
        }
        private readonly CodeRenderingContext _ctx;
        private readonly string _reactPageDir;

        public string FileName => "index.ts";

        private IEnumerable<ReactComponent> GetReactComponents() {
            return ReactComponent.All(_ctx);
        }
    }
}
