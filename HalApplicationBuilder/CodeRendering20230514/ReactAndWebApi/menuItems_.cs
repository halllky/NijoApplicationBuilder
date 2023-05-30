using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.ReactAndWebApi {
    partial class menuItems : ITemplate {
        internal const string FILE_NAME = "menuItems.tsx";
        internal static string IMPORT_NAME => Path.GetFileNameWithoutExtension(FILE_NAME);

        public string FileName => FILE_NAME;

        internal menuItems(CodeRenderingContext ctx, string reactPageDir) {
            _ctx = ctx;
            _reactPageDir = reactPageDir;
        }
        private readonly CodeRenderingContext _ctx;
        private readonly string _reactPageDir;

        private IEnumerable<ReactComponent> GetReactComponents() {
            return ReactComponent.All(_ctx);
        }
    }
}
