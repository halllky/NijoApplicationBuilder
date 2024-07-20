using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.CodeGenerating {
    /// <summary>
    /// 複数のソース生成処理が1つのファイルにまとめて書き込むような形のファイル。
    /// </summary>
    internal interface ISummarizedFile {

        /// <summary>
        /// コード生成処理の最後に呼ばれます。
        /// このメソッドの中でファイルレンダリングを行なってください。
        /// </summary>
        /// <param name="context">コード生成コンテキスト</param>
        void OnEndGenerating(CodeRenderingContext context);

        /// <summary>
        /// レンダリングされる順番（値が小さいほうが先）。
        /// <see cref="OnEndGenerating(CodeRenderingContext)"/> の中で別の <see cref="ISummarizedFile"/> の要素に変更をかけたりするので苦肉の策
        /// </summary>
        int RenderingOrder => 0;
    }
}
