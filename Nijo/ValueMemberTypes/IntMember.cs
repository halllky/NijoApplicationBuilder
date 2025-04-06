using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.QueryModelModules;
using Nijo.Parts.CSharp;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.ValueMemberTypes;

/// <summary>
/// 整数型
/// </summary>
internal class IntMember : IValueMemberType {
    string IValueMemberType.TypePhysicalName => "Integer";
    string IValueMemberType.SchemaTypeName => "int";
    string IValueMemberType.CsDomainTypeName => "int";
    string IValueMemberType.CsPrimitiveTypeName => "int";
    string IValueMemberType.TsTypeName => "number";
    UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.NumberMemberConstraint;

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
            return context.Random.Next(0, 1000);
            """;
    }

    void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
        // 特になし
    }
}
