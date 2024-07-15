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
    /// ほかの集約から参照されるときのためのデータクラス
    /// </summary>
    internal class DataClassForSaveRefTarget {
        internal DataClassForSaveRefTarget(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        /// <summary>
        /// データ構造を定義します（C#）
        /// </summary>
        internal string RenderCSharp(CodeRenderingContext context) {
            throw new NotImplementedException();
        }
        /// <summary>
        /// データ構造を定義します（TypeScript）
        /// </summary>
        internal string RenderTypeScript(CodeRenderingContext context) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 画面表示用データクラスを登録更新用データクラスに変換します。
        /// </summary>
        private string RenderFromDisplayData() {
            throw new NotImplementedException("TODO");
        }

        /// <summary>
        /// 登録更新用データクラスの値をEFCoreEntityにマッピングします。
        /// </summary>
        private string ToDbEntity() {
            throw new NotImplementedException("TODO");
        }
    }
}
