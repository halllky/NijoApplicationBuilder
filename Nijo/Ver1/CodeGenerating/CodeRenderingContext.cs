using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.CodeGenerating {
    /// <summary>
    /// ソースコード自動生成のコンテキスト情報
    /// </summary>
    public sealed class CodeRenderingContext {
        internal CodeRenderingContext() { }

        public MemberTypeResolver MemberTypeResolver => throw new NotImplementedException();
    }
}
