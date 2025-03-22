using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Ver1.ImmutableSchema {
    /// <summary>
    /// モデルの属性。
    /// xxxID, xxx名, xxx日付, ... などの単一の値、
    /// ref-toによる外部参照、
    /// ChildやChildrenといった子要素のうちのいずれか。
    /// </summary>
    public interface IAggregateMember {
        /// <summary>
        /// 物理名
        /// </summary>
        string PhysicalName { get; }

        /// <summary>
        /// 表示用名称
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// この属性を保持している集約
        /// </summary>
        AggregateBase Owner { get; }

        /// <summary>
        /// スキーマ定義でこのメンバーが定義されている順番。
        /// ref-toを辿るごとに小数桁が1桁ずつ下がっていくためにdecimal
        /// </summary>
        decimal Order { get; }

        /// <summary>
        /// エントリーからこのメンバーまでのパス
        /// </summary>
        PathStack Path { get; }
    }
}
