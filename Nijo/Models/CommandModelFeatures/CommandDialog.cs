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
    /// <see cref="CommandModel"/> 呼び出し用のモーダルダイアログのUI
    /// </summary>
    internal class CommandDialog {
        internal CommandDialog(GraphNode<Aggregate> rootAggregate) {
            _rootAggregate = rootAggregate;
        }
        private readonly GraphNode<Aggregate> _rootAggregate;

        internal string Render(CodeRenderingContext context) {
            return $$"""
                // TODO #3 コマンド実行ダイアログ
                """;
        }
    }
}
