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
    }

    /// <summary>
    /// Child, Children, RefTo の3種類
    /// </summary>
    public interface IRelationalMember : IAggregateMember {
        /// <summary>
        /// 元集約（親 or 参照元集約）との間の関係性の名前の物理名
        /// </summary>
        string RelationPhysicalName { get; }
    }

    /// <summary>
    /// モデルの属性のうち、xxxID, xxx名, xxx日付, ... などのような単一の値。
    /// </summary>
    public sealed class ValueMember : IAggregateMember {
        internal ValueMember(XElement xElement, SchemaParseContext ctx) {
            _xElement = xElement;
            _ctx = ctx;
        }
        private readonly XElement _xElement;
        private readonly SchemaParseContext _ctx;

        public string PhysicalName => _ctx.GetPhysicalName(_xElement);
        public string DisplayName => _ctx.GetDisplayName(_xElement);
        public decimal Order => _ctx.GetIndexInSiblings(_xElement);

        public AggregateBase Owner => AggregateBase.Parse(_xElement.Parent!, _ctx);

        /// <summary>
        /// この属性の型
        /// </summary>
        public IValueMemberType Type => _ctx.TryResolveMemberType(_xElement, out var type)
            ? type
            : throw new InvalidOperationException();
    }

    /// <summary>
    /// モデルの属性のうち、外部参照。
    /// </summary>
    public class RefToMember : IRelationalMember {
        internal RefToMember(XElement xElement, SchemaParseContext ctx) {
            _xElement = xElement;
            _ctx = ctx;
        }

        private readonly XElement _xElement;
        private readonly SchemaParseContext _ctx;

        public string PhysicalName => _ctx.GetPhysicalName(_xElement);
        public string DisplayName => _ctx.GetDisplayName(_xElement);
        public string RelationPhysicalName => throw new NotImplementedException("GraphEdgeの属性で定義されている物理名を返す");
        public decimal Order => _ctx.GetIndexInSiblings(_xElement);

        /// <summary>
        /// 参照元集約
        /// </summary>
        public AggregateBase Owner => AggregateBase.Parse(_xElement.Parent!, _ctx);
        /// <summary>
        /// 参照先集約
        /// </summary>
        public AggregateBase RefTo => AggregateBase.Parse(_ctx.FindRefTo(_xElement) ?? throw new InvalidOperationException(), _ctx);
    }

    /// <summary>
    /// 静的区分の値
    /// </summary>
    internal class StaticEnumValueDef : IAggregateMember {

        private const string ATTR_KEY = "key";

        internal StaticEnumValueDef(XElement xElement, SchemaParseContext ctx) {
            _xElement = xElement;
            _ctx = ctx;
        }
        private readonly XElement _xElement;
        private readonly SchemaParseContext _ctx;

        public string PhysicalName => _ctx.GetPhysicalName(_xElement);
        public string DisplayName => _ctx.GetDisplayName(_xElement);
        public AggregateBase Owner => AggregateBase.Parse(_xElement.Parent!, _ctx);
        public decimal Order => _ctx.GetIndexInSiblings(_xElement);

        public int EnumValue => int.Parse(_xElement.Attribute(ATTR_KEY)?.Value ?? throw new InvalidOperationException());
    }
}
