using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features.ForRef {
    /// <summary>
    /// 参照先データ検索用検索ダイアログ
    /// </summary>
    internal class SearchDialog {
        internal SearchDialog(GraphNode<Aggregate> agg) {
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
