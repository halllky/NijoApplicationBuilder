using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models.QueryModelModules;
using Nijo.Ver1.Models.StaticEnumModelModules;
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

        internal StaticEnumMember(XElement el, SchemaParsing.SchemaParseContext ctx) {
            var rootAggregate = new RootAggregate(el, ctx, PathStack.Entry());
            _def = new StaticEnumDef(rootAggregate);
            _condition = new StaticEnumSearchCondition(rootAggregate);
        }
        private readonly StaticEnumDef _def;
        private readonly StaticEnumSearchCondition _condition;

        string IValueMemberType.SchemaTypeName => _def.CsEnumName;
        string IValueMemberType.CsDomainTypeName => _def.CsEnumName;
        string IValueMemberType.CsPrimitiveTypeName => _def.CsEnumName;
        string IValueMemberType.TsTypeName => _def.TsTypeName;
        ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
            FilterCsTypeName = _condition.CsClassName,
            FilterTsTypeName = _condition.TsTypeName,
            RenderTsNewObjectFunctionValue = () => "{}",
        };
        UiConstraint.E_Type IValueMemberType.UiConstraintType => throw new NotImplementedException();

        void IValueMemberType.GenerateCode(CodeRenderingContext ctx) {
            // 特になし
        }
    }
}
