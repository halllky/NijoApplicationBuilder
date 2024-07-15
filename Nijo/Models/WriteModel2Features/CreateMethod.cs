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
    /// 新規データ登録処理
    /// </summary>
    internal class CreateMethod {
        internal CreateMethod(GraphNode<Aggregate> rootAggregate) {
            _rootAggregate = rootAggregate;
        }

        private readonly GraphNode<Aggregate> _rootAggregate;

        /// <summary>
        /// データ新規登録処理をレンダリングします。
        /// </summary>
        internal string Render(CodeRenderingContext context) {
            throw new NotImplementedException();
        }
    }
}
