using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// データ登録更新時に変更されてはならない項目がどれかを定義する処理
    /// </summary>
    internal class SetReadOnly {
        private readonly GraphNode<Aggregate> _rootAggregate;

        internal SetReadOnly(GraphNode<Aggregate> rootAggregate) {
            _rootAggregate = rootAggregate;
        }

        /// <summary>
        /// データ登録更新時に変更されてはならない項目がどれかを定義するメソッドをレンダリングします。
        /// </summary>
        internal string Render(CodeRenderingContext context) {
            throw new NotImplementedException();
        }
    }
}
