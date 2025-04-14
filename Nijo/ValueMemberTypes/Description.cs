using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.QueryModelModules;
using Nijo.SchemaParsing;
using Nijo.Util.DotnetEx;
using Nijo.CodeGenerating.Helpers;
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
                var query = ctx.Query.Root.Name;

                var pathFromSearchCondition = ctx.SearchCondition.GetPathFromInstance().Select(p => p.Metadata.PropertyName).ToArray();
                var fullpathNullable = $"{ctx.SearchCondition.Root.Name}.{pathFromSearchCondition.Join("?.")}";
                var fullpathNotNull = $"{ctx.SearchCondition.Root.Name}.{pathFromSearchCondition.Join(".")}";

                var whereFullpath = ctx.Query.GetFlattenArrayPath(E_CsTs.CSharp, out var isMany);

                return $$"""
                    if (!string.IsNullOrWhiteSpace({{fullpathNullable}})) {
                        var trimmed = {{fullpathNotNull}}.Trim();
                    {{If(isMany, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}}.Any(y => y.{{ctx.Query.Metadata.PropertyName}}!.Contains(trimmed)));
                    """).Else(() => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}}!.Contains(trimmed));
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
