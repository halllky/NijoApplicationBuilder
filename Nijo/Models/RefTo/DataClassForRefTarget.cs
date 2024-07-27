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
    /// ほかの集約から参照されるときのためのデータクラス
    /// </summary>
    internal class DataClassForRefTarget {
        internal DataClassForRefTarget(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }

        /// <summary>
        /// 必ずしもルート集約とは限らない
        /// </summary>
        private readonly GraphNode<Aggregate> _aggregate;

        internal string CsClassName => "TODO #35";
        internal string TsTypeName => "TODO #35";

        /// <summary>
        /// データ構造を定義します（C#）
        /// </summary>
        internal string RenderCSharp(CodeRenderingContext context) {
            return $$"""
                TODO #35
                """;
        }
        /// <summary>
        /// データ構造を定義します（TypeScript）
        /// </summary>
        internal string RenderTypeScript(CodeRenderingContext context) {
            return $$"""
                TODO #35
                """;
        }
    }
}
