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
    /// モデルの属性のうち、外部参照。
    /// </summary>
    public class RefToMember : IRelationalMember {
        internal RefToMember(XElement xElement, SchemaParseContext ctx, PathStack path) {
            _xElement = xElement;
            _ctx = ctx;
            Path = path;
        }

        private readonly XElement _xElement;
        private readonly SchemaParseContext _ctx;
        public PathStack Path { get; }

        public string PhysicalName => _ctx.GetPhysicalName(_xElement);
        public string DisplayName => _ctx.GetDisplayName(_xElement);
        public decimal Order => _ctx.GetIndexInSiblings(_xElement);

        /// <summary>
        /// 参照元集約
        /// </summary>
        public AggregateBase Owner => _ctx.ToAggregateBase(_xElement.Parent ?? throw new InvalidOperationException(), Path);
        /// <summary>
        /// 参照先集約
        /// </summary>
        public AggregateBase RefTo => _ctx.ToAggregateBase(_ctx.FindRefTo(_xElement) ?? throw new InvalidOperationException(), Path);
        AggregateBase IRelationalMember.MemberAggregate => RefTo;

        #region モデル毎に定義される属性
        internal const string IS_KEY = "key";
        internal const string IS_REQUIRED = "required";

        /// <summary>キー属性か否か</summary>
        public bool IsKey => _ctx.ParseIsAttribute(_xElement).Any(attr => attr.Key == IS_KEY);
        /// <summary>必須か否か</summary>
        public bool IsRequired => _ctx.ParseIsAttribute(_xElement).Any(attr => attr.Key == IS_REQUIRED);
        #endregion モデル毎に定義される属性
    }
}
