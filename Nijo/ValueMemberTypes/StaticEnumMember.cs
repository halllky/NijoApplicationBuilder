using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.QueryModelModules;
using Nijo.SchemaParsing;
using Nijo.Util.DotnetEx;
using Nijo.Models.StaticEnumModelModules;
using Nijo.CodeGenerating.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.ValueMemberTypes {
    /// <summary>
    /// 列挙体型のメンバー
    /// </summary>
    internal class StaticEnumMember : IValueMemberType {

        internal StaticEnumMember(XElement el, SchemaParseContext ctx) {
            _xElement = el;
            _parser = new EnumDefParser(el, ctx);
        }

        private readonly XElement _xElement;
        /// <inheritdoc cref="EnumDefParser"/>
        private readonly EnumDefParser _parser;

        public string TypePhysicalName => _xElement.Name.LocalName;
        public string SchemaTypeName => _xElement.Name.LocalName;
        public string CsDomainTypeName => _parser.CsEnumName;
        public string CsPrimitiveTypeName => _parser.CsEnumName;
        public string TsTypeName => _parser.TsTypeName;

        ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
            FilterCsTypeName = _parser.CsSearchConditionClassName,
            FilterTsTypeName = _parser.RenderTsSearchConditionType(),
            RenderTsNewObjectFunctionValue = () => "{}",
            RenderFiltering = ctx => {
                var query = ctx.Query.Root.Name;

                var pathFromSearchCondition = ctx.SearchCondition.GetPathFromInstance().Select(p => p.Metadata.PropertyName).ToArray();
                var fullpathNullable = $"{ctx.SearchCondition.Root.Name}.{pathFromSearchCondition.Join("?.")}";
                var fullpathNotNull = $"{ctx.SearchCondition.Root.Name}.{pathFromSearchCondition.Join(".")}";

                var whereFullpath = ctx.Query.GetFlattenArrayPath(E_CsTs.CSharp, out var isMany);

                return $$"""
                    if ({{fullpathNullable}} != null && {{fullpathNotNull}}.AnyChecked()) {
                        var array = new List<{{CsPrimitiveTypeName}}?>();
                    {{_parser.GetItemPhysicalNames().SelectTextTemplate(physicalName => $$"""
                        if ({{fullpathNotNull}}.{{physicalName}}) array.Add({{CsPrimitiveTypeName}}.{{physicalName}});
                    """)}}

                    {{If(isMany, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}}.Any(y => array.Contains(y.{{ctx.Query.Metadata.PropertyName}})));
                    """).Else(() => $$"""
                        {{query}} = {{query}}.Where(x => array.Contains(x.{{whereFullpath.Join(".")}}));
                    """)}}
                    }
                    """;
            }
        };
        UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.MemberConstraintBase;

        void IValueMemberType.Validate(XElement element, SchemaParseContext context, Action<XElement, string> addError) {
            // 列挙型の定義が正しいことを検証
            // EnumDefParserは既に初期化時に基本的な検証を行っているため、
            // 追加の検証が必要な場合はここに実装します
        }

        void IValueMemberType.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // 特になし
        }
        void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
            // 特になし
        }

        string IValueMemberType.RenderCreateDummyDataValueBody(CodeRenderingContext ctx) {
            var count = _parser.GetItemPhysicalNames().Count();

            if (count == 0) {
                return $$"""
                    return null;
                    """;
            } else {
                return $$"""
                    return Enum.GetValues<{{_parser.CsEnumName}}>().ElementAt(context.Random.Next(0, {{count - 1}}));
                    """;
            }
        }
    }
}
