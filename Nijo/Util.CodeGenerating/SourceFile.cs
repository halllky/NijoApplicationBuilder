using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.CodeGenerating {
    public class SourceFile {
        public required string FileName { get; init; }
        public required Func<CodeRenderingContext, string> RenderContent { get; init; }
    }
}
