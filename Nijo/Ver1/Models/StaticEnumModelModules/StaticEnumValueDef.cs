using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Ver1.Models.StaticEnumModelModules {
    /// <summary>
    /// 静的区分の値の定義
    /// </summary>
    internal class StaticEnumValueDef : IAggregateMember {

        private const string ATTR_KEY = "key";

        internal StaticEnumValueDef(XElement xElement, SchemaParseContext ctx, PathStack path) {
            _xElement = xElement;
            _ctx = ctx;
            Path = path;
        }
        private readonly XElement _xElement;
        private readonly SchemaParseContext _ctx;
        public PathStack Path { get; }

        public string PhysicalName => _ctx.GetPhysicalName(_xElement);
        public string DisplayName => _ctx.GetDisplayName(_xElement);
        public AggregateBase Owner => _ctx.ToAggregateBase(_xElement.Parent ?? throw new InvalidOperationException(), Path);
        public decimal Order => _ctx.GetIndexInSiblings(_xElement);

        public int EnumValue => int.Parse(_xElement.Attribute(ATTR_KEY)?.Value ?? throw new InvalidOperationException());
    }
}
