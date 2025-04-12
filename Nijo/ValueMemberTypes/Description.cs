using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.QueryModelModules;
using Nijo.SchemaParsing;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.ValueMemberTypes {
    /// <summary>
    /// 文章型
    /// </summary>
    internal class Description : IValueMemberType {
        string IValueMemberType.TypePhysicalName => "Description";
        string IValueMemberType.SchemaTypeName => "description";
        string IValueMemberType.CsDomainTypeName => "string";
        string IValueMemberType.CsPrimitiveTypeName => "string";
        string IValueMemberType.TsTypeName => "string";
        UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.StringMemberConstraint;

        void IValueMemberType.Validate(XElement element, SchemaParseContext context, Action<XElement, string> addError) {
            // 文章型の検証
            // 必要に応じて最大長などの制約を検証するコードをここに追加できます
        }

        ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
            FilterCsTypeName = "string",
            FilterTsTypeName = "string",
            RenderTsNewObjectFunctionValue = () => "undefined",
            RenderFiltering = ctx => {
                var fullpath = ctx.Member.GetPathFromEntry().ToArray();
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

        string IValueMemberType.RenderCreateDummyDataValueBody(CodeRenderingContext ctx) {
            return $$"""
                return string.Concat(Enumerable.Range(0, member.MaxLength ?? 12).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+-=[]{}\\\"|;:,.<>?"[context.Random.Next(0, 63)]));
                """;
        }

        void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
            // 特になし
        }
    }
}
