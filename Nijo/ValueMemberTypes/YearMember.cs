using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.QueryModelModules;
using Nijo.Parts.CSharp;
using Nijo.SchemaParsing;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.ValueMemberTypes;

/// <summary>
/// 年型
/// </summary>
internal class YearMember : IValueMemberType {
    string IValueMemberType.TypePhysicalName => "Year";
    string IValueMemberType.SchemaTypeName => "year";
    string IValueMemberType.CsDomainTypeName => "int";
    string IValueMemberType.CsPrimitiveTypeName => "int";
    string IValueMemberType.TsTypeName => "number";
    UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.NumberMemberConstraint;

    void IValueMemberType.Validate(XElement element, SchemaParseContext context, Action<XElement, string> addError) {
        // 年型の検証
        // 必要に応じて年の範囲制約などを検証するコードをここに追加できます
    }

    ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
        FilterCsTypeName = $"{FromTo.CS_CLASS_NAME}<int?>",
        FilterTsTypeName = "{ from?: number; to?: number }",
        RenderTsNewObjectFunctionValue = () => "{ from: undefined, to: undefined }",
        RenderFiltering = ctx => {
            var fullpath = ctx.Member.GetPathFromEntry().ToArray();
            var pathFromSearchCondition = fullpath.AsSearchConditionFilter(E_CsTs.CSharp).ToArray();
            var whereFullpath = fullpath.AsSearchResult().ToArray();
            var fullpathNullable = $"{ctx.SearchCondition}.{pathFromSearchCondition.Join("?.")}";
            var fullpathNotNull = $"{ctx.SearchCondition}.{pathFromSearchCondition.Join(".")}";
            var isArray = fullpath.Any(node => node is ChildrenAggreagte);

            return $$"""
                if ({{fullpathNullable}}?.From != null && {{fullpathNullable}}?.To != null) {
                {{If(isArray, () => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{ctx.Member.PhysicalName}} >= {{fullpathNotNull}}.From && y.{{ctx.Member.PhysicalName}} <= {{fullpathNotNull}}.To));
                """).Else(() => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.Join(".")}} >= {{fullpathNotNull}}.From && x.{{whereFullpath.Join(".")}} <= {{fullpathNotNull}}.To);
                """)}}
                } else if ({{fullpathNullable}}?.From != null) {
                {{If(isArray, () => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{ctx.Member.PhysicalName}} >= {{fullpathNotNull}}.From));
                """).Else(() => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.Join(".")}} >= {{fullpathNotNull}}.From);
                """)}}
                } else if ({{fullpathNullable}}?.To != null) {
                {{If(isArray, () => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{ctx.Member.PhysicalName}} <= {{fullpathNotNull}}.To));
                """).Else(() => $$"""
                    {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.Join(".")}} <= {{fullpathNotNull}}.To);
                """)}}
                }
                """;
        },
    };

    void IValueMemberType.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
        // 特になし
    }

    string IValueMemberType.RenderCreateDummyDataValueBody(CodeRenderingContext ctx) {
        return $$"""
            return DateTime.Now.Year + context.Random.Next(-50, 50);
            """;
    }

    void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
        // 特になし
    }
}
