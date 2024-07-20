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
    /// 既存データ更新処理
    /// </summary>
    internal class UpdateMethod {
        internal UpdateMethod(GraphNode<Aggregate> rootAggregate) {
            _rootAggregate = rootAggregate;
        }

        private readonly GraphNode<Aggregate> _rootAggregate;

        internal object MethodName => $"Update{_rootAggregate.Item.PhysicalName}";

        /// <summary>
        /// データ更新処理をレンダリングします。
        /// </summary>
        internal string Render(CodeRenderingContext context) {
            return $$"""
                TODO #35
                """;
        }
    }
}
