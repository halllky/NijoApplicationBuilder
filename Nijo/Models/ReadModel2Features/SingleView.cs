using Nijo.Core;
using Nijo.Parts.WebClient;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 詳細画面。新規モード・閲覧モード・編集モードの3種類をもつ。
    /// </summary>
    internal class SingleView {
        /// <summary>
        /// 新規モード・閲覧モード・編集モードのうちいずれか
        /// </summary>
        internal enum E_Type {
            New,
            ReadOnly,
            Edit,
        }

        internal SingleView(GraphNode<Aggregate> agg, E_Type type) {
            _aggregate = agg;
            _type = type;
        }
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly E_Type _type;

        internal IReactPage Render() {
            throw new NotImplementedException("TODO");
        }
    }
}
