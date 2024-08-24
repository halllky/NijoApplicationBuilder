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

        /// <summary>
        /// React Query でキャッシュや最適化を行うために、同一のクエリごとに設定するキー
        /// </summary>
        internal string RenderReactQueryKeyString(string? keywordVarName = null) {
            return keywordVarName == null
                ? $"`combo-{_aggregate.Item.UniqueId}::`"
                : $"`combo-{_aggregate.Item.UniqueId}::${{{keywordVarName} ?? ''}}`";
        }

        internal string Render(CodeRenderingContext context) {
            return $$"""
                // TODO #35 SearchComboBox
                """;
        }
    }
}
