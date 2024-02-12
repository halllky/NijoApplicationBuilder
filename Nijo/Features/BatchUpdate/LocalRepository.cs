using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.BatchUpdate {
    internal class LocalRepository {
        internal static string GetDataTypeKey(GraphNode<Aggregate> agg) {
            return agg.Item.ClassName;
        }
    }
}
