using Nijo.Util.DotnetEx;
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

        public string SchemaTypeName => _xElement.Name.LocalName;
        public string CsDomainTypeName => _parser.CsEnumName;
        public string CsPrimitiveTypeName => _parser.CsEnumName;
        public string TsTypeName => _parser.TsTypeName;

        ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
            FilterCsTypeName = _parser.CsSearchConditionClassName,
            FilterTsTypeName = _parser.RenderTsSearchConditionType(),
            RenderTsNewObjectFunctionValue = () => "{}",
            RenderFiltering = ctx => {
                var fullpath = ctx.Member
                    .GetFullPath()
                    .ToArray();
                var strArrayPath = fullpath
                    .AsSearchConditionFilter(E_CsTs.CSharp)
                    .ToArray();
                var fullpathNullable = strArrayPath.Join("?.");
                var fullpathNotNull = strArrayPath.Join(".");

                var isArray = fullpath.Any(node => node is ChildrenAggreagte);
                var whereFullpath = fullpath.AsSearchResult().ToArray();
                var query = ctx.Query;

                return $$"""
                    if ({{fullpathNullable}} != null && {{fullpathNotNull}}.AnyChecked()) {
                        var array = new List<{{CsPrimitiveTypeName}}?>();
                    {{_parser.GetItemPhysicalNames().SelectTextTemplate(physicalName => $$"""
                        if ({{fullpathNotNull}}.{{physicalName}}) array.Add({{CsPrimitiveTypeName}}.{{physicalName}});
                    """)}}

                    {{If(isArray, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => array.Contains(y.{{ctx.Member.PhysicalName}})));
                    """).Else(() => $$"""
                        {{query}} = {{query}}.Where(x => array.Contains(x.{{whereFullpath.Join(".")}}));
                    """)}}
                    }
                    """;
            }
        };
        UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.MemberConstraintBase;

        void IValueMemberType.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // 特になし
        }
        void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
            // 特になし
        }
    }
}
