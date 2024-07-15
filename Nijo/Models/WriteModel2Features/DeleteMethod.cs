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
    /// 既存データ削除処理
    /// </summary>
    internal class DeleteMethod {
        internal DeleteMethod(GraphNode<Aggregate> rootAggregate) {
            _rootAggregate = rootAggregate;
        }

        private readonly GraphNode<Aggregate> _rootAggregate;

        /// <summary>
        /// データ削除処理をレンダリングします。
        /// </summary>
        internal string Render(CodeRenderingContext context) {
            return $$"""
                TODO #35
                """;
        }
    }
}
