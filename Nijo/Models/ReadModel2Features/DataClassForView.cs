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
    /// 画面表示用データクラス
    /// </summary>
    internal class DataClassForView {
        internal DataClassForView(GraphNode<Aggregate> agg) {
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
