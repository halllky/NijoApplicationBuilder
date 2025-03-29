using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Ver1.SchemaParsing {
    /// <summary>
    /// スキーマ定義の列挙体定義を解釈する。
    /// スキーマ編集時とコード自動生成時の両方から利用される。
    /// </summary>
    internal class EnumDefParser {
        internal EnumDefParser(XElement xElement, SchemaParseContext ctx) {
            _xElement = xElement;
            _ctx = ctx;
        }
        private readonly XElement _xElement;
        private readonly SchemaParseContext _ctx;

        internal string DisplayName => _ctx.GetDisplayName(_xElement);
        internal string CsEnumName => _ctx.GetPhysicalName(_xElement);
        internal string TsTypeName => _ctx.GetPhysicalName(_xElement);

        internal string CsSearchConditionClassName => $"{CsEnumName}SearchCondition";

        internal string RenderTsSearchConditionType() {
            return $$"""
                { '{{_xElement.Elements().Select(el => $"'{_ctx.GetDisplayName(el).Replace("'", "\\'")}'?: boolean").Join(", ")}} }
                """;
        }
    }
}
