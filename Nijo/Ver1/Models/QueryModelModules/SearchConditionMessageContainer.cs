using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.QueryModelModules {
    /// <summary>
    /// <see cref="SearchCondition"/> のデータ構造と対応するメッセージの入れ物
    /// </summary>
    internal class SearchConditionMessageContainer : MessageContainer {
        public SearchConditionMessageContainer(AggregateBase aggregate) : base(aggregate) {
        }

        protected override IEnumerable<IMessageContainerMember> GetMembers() {
            yield break; // TODO ver.1
        }
    }
}
