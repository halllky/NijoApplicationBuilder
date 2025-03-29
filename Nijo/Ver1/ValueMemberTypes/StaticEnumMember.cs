using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models.QueryModelModules;
using Nijo.Ver1.Models.StaticEnumModelModules;
using Nijo.Ver1.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Ver1.ValueMemberTypes {
    /// <summary>
    /// 列挙体型のメンバー
    /// </summary>
    internal class StaticEnumMember : IValueMemberType {

        internal StaticEnumMember(XElement el, SchemaParseContext ctx) {
            _xElement = el;
            _parser = new EnumDefParser(el, ctx);
        }

        private readonly XElement _xElement;
        private readonly EnumDefParser _parser;

        string IValueMemberType.SchemaTypeName => _xElement.Name.LocalName;
        string IValueMemberType.CsDomainTypeName => _parser.CsEnumName;
        string IValueMemberType.CsPrimitiveTypeName => _parser.CsEnumName;
        string IValueMemberType.TsTypeName => _parser.TsTypeName;
        ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
            FilterCsTypeName = _parser.CsSearchConditionClassName,
            FilterTsTypeName = _parser.RenderTsSearchConditionType(),
            RenderTsNewObjectFunctionValue = () => "{}",
        };
        UiConstraint.E_Type IValueMemberType.UiConstraintType => throw new NotImplementedException();

        void IValueMemberType.GenerateCode(CodeRenderingContext ctx) {
            // 特になし
        }
    }
}
