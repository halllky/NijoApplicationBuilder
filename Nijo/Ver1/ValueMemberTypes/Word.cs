using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models.QueryModelModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.ValueMemberTypes {
    /// <summary>
    /// 単語型
    /// </summary>
    internal class Word : IValueMemberType {
        string IValueMemberType.SchemaTypeName => "word";

        string IValueMemberType.CsDomainTypeName => "string";
        string IValueMemberType.CsPrimitiveTypeName => "string";
        string IValueMemberType.TsTypeName => "string";
        UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.StringMemberConstraint;

        ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
            FilterCsTypeName = "string",
            FilterTsTypeName = "string",
            RenderTsNewObjectFunctionValue = () => "undefined",
            RenderFiltering = ctx => {
                // TODO ver.1 範囲検索以外も作る
                var fullpath = ctx.Member.GetFullPath().ToArray();
                var pathFromSearchCondition = fullpath.AsSearchConditionFilter(E_CsTs.CSharp).ToArray();
                var whereFullpath = fullpath.AsSearchResult().ToArray();
                var fullpathNullable = $"{ctx.SearchCondition}.{pathFromSearchCondition.Join("?.")}";
                var fullpathNotNull = $"{ctx.SearchCondition}.{pathFromSearchCondition.Join(".")}";
                var isArray = fullpath.Any(node => node is ChildrenAggreagte);

                return $$"""
                    if (!string.IsNullOrWhiteSpace({{fullpathNullable}})) {
                        var trimmed = {{fullpathNotNull}}.Trim();
                    {{If(isArray, () => $$"""
                        {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{ctx.Member.PhysicalName}}!.Contains(trimmed)));
                    """).Else(() => $$"""
                        {{ctx.Query}} = {{ctx.Query}}.Where(x => x.{{whereFullpath.Join("!.")}}!.Contains(trimmed));
                    """)}}
                    }
                    """;
            },
        };

        void IValueMemberType.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // 特になし
        }
        void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
            // 特になし
        }
    }
}
