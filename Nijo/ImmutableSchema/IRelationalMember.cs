using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.ImmutableSchema {
    /// <summary>
    /// Child, Children, RefTo の3種類
    /// </summary>
    public interface IRelationalMember : IAggregateMember {
        /// <summary>
        /// 子集約または参照先集約
        /// </summary>
        AggregateBase MemberAggregate { get; }
    }
}
