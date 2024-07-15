using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 検索条件クラス
    /// </summary>
    internal class SearchCondition {
        internal SearchCondition(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string RenderCSharpDeclaring(CodeRenderingContext context) {
            return $$"""
                TODO #35
                """;
        }

        internal string RenderTypeScriptDeclaring(CodeRenderingContext context) {
            return $$"""
                TODO #35
                """;
        }
    }
}
