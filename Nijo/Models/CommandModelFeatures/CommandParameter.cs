using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.CommandModelFeatures {
    /// <summary>
    /// <see cref="CommandModel"/> のパラメータの型
    /// </summary>
    internal class CommandParameter {
        internal CommandParameter(GraphNode<Aggregate> rootAggregate) {
            _rootAggregate = rootAggregate;
        }
        private readonly GraphNode<Aggregate> _rootAggregate;

        internal string CsClassName => $"{_rootAggregate.Item.PhysicalName}Parameter";
        internal string TsTypeName => $"{_rootAggregate.Item.PhysicalName}Parameter";

        internal string RenderCSharpDeclaring(CodeRenderingContext context) {
            return $$"""
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}処理のパラメータ
                /// </summary>
                public partial class {{CsClassName}} {
                    // TODO #3
                }
                """;
        }

        internal string RenderTsDeclaring(CodeRenderingContext context) {
            return $$"""
                /** {{_rootAggregate.Item.DisplayName}}処理のパラメータ */
                export type {{TsTypeName}} = {
                  // TODO #3
                }
                """;
        }
    }
}
