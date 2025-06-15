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
        public string TsTypeName => $"EnumDefs.{_parser.TsTypeName}";
        public string DisplayName => "列挙型";

        ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
            FilterCsTypeName = _parser.CsSearchConditionClassName,
            FilterTsTypeName = _parser.RenderTsSearchConditionType(),
            RenderTsNewObjectFunctionValue = () => "{}",
            RenderFiltering = ctx => {
                var query = ctx.Query.Root.Name;
                var fullpathNullable = ctx.SearchCondition.GetJoinedPathFromInstance(E_CsTs.CSharp, "?.");
                var fullpathNotNull = ctx.SearchCondition.GetJoinedPathFromInstance(E_CsTs.CSharp, ".");

                var queryFullPath = ctx.Query.GetFlattenArrayPath(E_CsTs.CSharp, out var isMany);
                var queryOwnerFullPath = queryFullPath.SkipLast(1);

                return $$"""
                    if ({{fullpathNullable}}?.{{StaticEnumSearchCondition.ANY_CHECKED}}() == true) {
                        var array = new List<{{CsPrimitiveTypeName}}>();
                    {{_parser.GetItemPhysicalNames().SelectTextTemplate(physicalName => $$"""
                        if ({{fullpathNotNull}}!.{{physicalName}}) array.Add({{CsPrimitiveTypeName}}.{{physicalName}});
                    """)}}
                    {{If(isMany, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{queryOwnerFullPath.Join("!.")}}.Any(y => y.{{ctx.Query.Metadata.GetPropertyName(E_CsTs.CSharp)}} != null && array.Contains(y.{{ctx.Query.Metadata.GetPropertyName(E_CsTs.CSharp)}}.Value)));
                    """).Else(() => $$"""
                        {{query}} = {{query}}.Where(x => x.{{queryFullPath.Join("!.")}} != null && array.Contains(x.{{queryFullPath.Join("!.")}}.Value));
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
                    return member.IsKey && Enum.GetValues<{{_parser.CsEnumName}}>().Length > 0
                        ? Enum.GetValues<{{_parser.CsEnumName}}>()[context.GetNextSequence() % Enum.GetValues<{{_parser.CsEnumName}}>().Length]
                        : Enum.GetValues<{{_parser.CsEnumName}}>().ElementAt(context.Random.Next(0, {{count - 1}}));
                    """;
            }
        }
    }
}
