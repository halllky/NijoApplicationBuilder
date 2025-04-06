using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.SchemaParsing;

/// <summary>
/// XML要素の種類
/// </summary>
[Flags]
internal enum E_NodeType {
    /// <summary>集約</summary>
    Aggregate = 1 << 0,
    /// <summary>集約メンバー</summary>
    AggregateMember = 1 << 1,
    /// <summary>Child, Children, Ref</summary>
    RelationMember = 1 << 2,

    /// <summary>ルート集約</summary>
    RootAggregate = Aggregate | 1 << 3,
    /// <summary>Child</summary>
    ChildAggregate = Aggregate | RelationMember | 1 << 4,
    /// <summary>Children</summary>
    ChildrenAggregate = Aggregate | RelationMember | 1 << 5,

    /// <summary>値メンバー</summary>
    ValueMember = AggregateMember | 1 << 6,
    /// <summary>外部参照（ref-to）</summary>
    Ref = AggregateMember | RelationMember | 1 << 7,

    /// <summary>静的区分の種類</summary>
    StaticEnumType = RootAggregate | 1 << 8,
    /// <summary>静的区分の値</summary>
    StaticEnumValue = 1 << 9,

    /// <summary>
    /// 未知の値
    /// </summary>
    Unknown = 1 << 20,
}
