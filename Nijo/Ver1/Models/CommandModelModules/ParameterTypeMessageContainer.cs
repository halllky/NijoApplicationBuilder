using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.CommandModelModules {
    /// <summary>
    /// <see cref="MessageContainer"/> の拡張。
    /// CommandModelの場合はメッセージコンテナの構造定義に使われる集約とクラス名に使われる集約が別
    /// </summary>
    internal class ParameterTypeMessageContainer : MessageContainer {
        internal ParameterTypeMessageContainer(RootAggregate rootAggregate) : base(rootAggregate.GetCommandModelParameterChild()) {
            _rootAggregate = rootAggregate;
        }

        private readonly RootAggregate _rootAggregate;

        internal override string CsClassName => $"{_rootAggregate.PhysicalName}ParameterMessages";
        internal override string TsTypeName => $"{_rootAggregate.PhysicalName}ParameterMessages";
    }
}
