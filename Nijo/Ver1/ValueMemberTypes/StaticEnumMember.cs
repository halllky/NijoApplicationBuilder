using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
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
            _def = new StaticEnumDef(new RootAggregate(el, ctx));
        }
        private readonly StaticEnumDef _def;

        public string SchemaTypeName => _def.CsEnumName;
        public string CsDomainTypeName => _def.CsEnumName;
        public string CsPrimitiveTypeName => _def.CsEnumName;
        public string TsTypeName => _def.TsTypeName;

        public void GenerateCode(CodeRenderingContext ctx) {
            // 特になし
        }
    }
}
