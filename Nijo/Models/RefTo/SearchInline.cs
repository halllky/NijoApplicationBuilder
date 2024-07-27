using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.RefTo {
    /// <summary>
    /// フォームの一部として設置されるキーと主要項目のビュー
    /// </summary>
    internal class SearchInline {
        internal SearchInline(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        internal string Render(CodeRenderingContext context) {
            return $$"""
                TODO #35
                """;
        }
    }
}
