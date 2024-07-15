using Nijo.Core;
using Nijo.Parts.WebClient;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 一覧画面
    /// </summary>
    internal class MultiView {
        internal MultiView(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal IReactPage Render() {
            throw new NotImplementedException("TODO");
        }
    }
}
