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
    /// 参照先データ検索用コンボボックス
    /// </summary>
    internal class SearchComboBox {
        internal SearchComboBox(GraphNode<Aggregate> agg) {
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
