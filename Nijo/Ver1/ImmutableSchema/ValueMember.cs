using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.Models.QueryModelModules;
using Nijo.Ver1.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Ver1.ImmutableSchema {
    /// <summary>
    /// モデルの属性のうち、xxxID, xxx名, xxx日付, ... などのような単一の値。
    /// </summary>
    public sealed class ValueMember : IAggregateMember, IUiConstraintValue {
        internal ValueMember(XElement xElement, SchemaParseContext ctx, ISchemaPathNode? previous) {
            _xElement = xElement;
            _ctx = ctx;
            PreviousNode = previous;
        }
        private readonly XElement _xElement;
        private readonly SchemaParseContext _ctx;

        XElement ISchemaPathNode.XElement => _xElement;
        public ISchemaPathNode? PreviousNode { get; }

        public string PhysicalName => _ctx.GetPhysicalName(_xElement);
        public string DisplayName => _ctx.GetDisplayName(_xElement);
        public string DbName => _ctx.GetDbName(_xElement);
        public decimal Order => _ctx.GetIndexInSiblings(_xElement);

        public AggregateBase Owner => _xElement.Parent == PreviousNode?.XElement
            ? ((AggregateBase?)PreviousNode ?? throw new InvalidOperationException()) // パスの巻き戻しの場合
            : _ctx.ToAggregateBase(_xElement.Parent ?? throw new InvalidOperationException(), this);

        /// <summary>
        /// この属性の型
        /// </summary>
        public IValueMemberType Type => _ctx.TryResolveMemberType(_xElement, out var type)
            ? type
            : throw new InvalidOperationException();

        #region メンバー毎に定義される制約
        internal const string IS_KEY = "key";
        internal const string IS_REQUIRED = "required";
        internal const string IS_MAX_LENGTH = "max-length";
        internal const string IS_CHARACTER_TYPE = "character-type";
        internal const string IS_TOTAL_DIGIT = "total-digit";
        internal const string IS_DECIMAL_PLACE = "decimal-place";
        internal const string IS_SEQUENCE_NAME = "sequence-name";

        /// <summary>キー属性か否か</summary>
        public bool IsKey => _ctx.ParseIsAttribute(_xElement).Any(attr => attr.Key == IS_KEY);
        /// <summary>必須か否か</summary>
        public bool IsRequired => _ctx.ParseIsAttribute(_xElement).Any(attr => attr.Key == IS_REQUIRED);
        /// <summary>文字種。半角、半角英数、など</summary>
        public string? CharacterType => _ctx.ParseIsAttribute(_xElement).SingleOrDefault(attr => attr.Key == IS_CHARACTER_TYPE)?.Value;
        /// <summary>文字列系属性の最大長</summary>
        public int? MaxLength => int.TryParse(_ctx.ParseIsAttribute(_xElement).SingleOrDefault(attr => attr.Key == IS_MAX_LENGTH)?.Value, out var v) ? v : null;
        /// <summary>数値系属性の整数部桁数 + 小数部桁数</summary>
        public int? TotalDigit => int.TryParse(_ctx.ParseIsAttribute(_xElement).SingleOrDefault(attr => attr.Key == IS_TOTAL_DIGIT)?.Value, out var v) ? v : null;
        /// <summary>数値系属性の小数部桁数</summary>
        public int? DecimalPlace => int.TryParse(_ctx.ParseIsAttribute(_xElement).SingleOrDefault(attr => attr.Key == IS_DECIMAL_PLACE)?.Value, out var v) ? v : null;
        /// <summary>シーケンス物理名</summary>
        public string? SequenceName => _ctx.ParseIsAttribute(_xElement).SingleOrDefault(attr => attr.Key == IS_SEQUENCE_NAME)?.Value;
        #endregion メンバー毎に定義される制約

        #region 等価比較
        public override int GetHashCode() {
            return _xElement.GetHashCode();
        }
        public override bool Equals(object? obj) {
            return obj is ValueMember vm
                && vm._xElement == this._xElement;
        }
        public static bool operator ==(ValueMember? left, ValueMember? right) => ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.Equals(right);
        public static bool operator !=(ValueMember? left, ValueMember? right) => !(left == right);
        #endregion 等価比較

        public override string ToString() {
            // デバッグ用
            return $"{GetType().Name}({this.GetFullPath().Select(x => x.XElement.Name.LocalName).Join(">")})";
        }
    }
}
