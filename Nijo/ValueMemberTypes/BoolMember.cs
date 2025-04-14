using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.QueryModelModules;
using Nijo.Parts.CSharp;
using Nijo.SchemaParsing;
using Nijo.Util.DotnetEx;
using Nijo.CodeGenerating.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.ValueMemberTypes;

/// <summary>
/// 真偽値型
/// </summary>
internal class BoolMember : IValueMemberType {
    string IValueMemberType.TypePhysicalName => "Boolean";
    string IValueMemberType.SchemaTypeName => "bool";
    string IValueMemberType.CsDomainTypeName => "bool";
    string IValueMemberType.CsPrimitiveTypeName => "bool";
    string IValueMemberType.TsTypeName => "boolean";
    UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.MemberConstraintBase;

    void IValueMemberType.Validate(XElement element, SchemaParseContext context, Action<XElement, string> addError) {
        // 真偽値型の検証
        // 真偽値型の場合は特別な検証は必要ありません
    }

    ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
        FilterCsTypeName = "BooleanSearchCondition",
        FilterTsTypeName = "{ trueのみ?: boolean; falseのみ?: boolean }",
        RenderTsNewObjectFunctionValue = () => "{ trueのみ: false, falseのみ: false }",
        RenderFiltering = ctx => {
            var query = ctx.Query.Root.Name;

            var pathFromSearchCondition = ctx.SearchCondition.GetPathFromInstance().Select(p => p.Metadata.PropertyName).ToArray();
            var fullpathNullable = $"{ctx.SearchCondition.Root.Name}.{pathFromSearchCondition.Join("?.")}";
            var fullpathNotNull = $"{ctx.SearchCondition.Root.Name}.{pathFromSearchCondition.Join(".")}";

            var whereFullpath = ctx.Query.GetFlattenArrayPath(E_CsTs.CSharp, out var isMany);

            return $$"""
                if ({{fullpathNullable}} != null && ({{fullpathNotNull}}.Trueのみ || {{fullpathNotNull}}.Falseのみ)) {
                    if ({{fullpathNotNull}}.Trueのみ && !{{fullpathNotNull}}.Falseのみ) {
                    {{If(isMany, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}}.Any(y => y.{{ctx.Query.Metadata.PropertyName}} == true));
                    """).Else(() => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}} == true);
                    """)}}
                    } else if (!{{fullpathNotNull}}.Trueのみ && {{fullpathNotNull}}.Falseのみ) {
                    {{If(isMany, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}}.Any(y => y.{{ctx.Query.Metadata.PropertyName}} == false));
                    """).Else(() => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}} == false);
                    """)}}
                    }
                }
                """;
        }
    };

    void IValueMemberType.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
        // 特になし
    }

    string IValueMemberType.RenderCreateDummyDataValueBody(CodeRenderingContext ctx) {
        return $$"""
            return context.Random.Next(0, 2) == 0;
            """;
    }

    void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
        ctx.CoreLibrary(dir => {
            dir.Generate(new SourceFile {
                FileName = "BooleanSearchCondition.cs",
                Contents = $$"""
                using System;

                namespace MyApp.Core
                {
                    public class BooleanSearchCondition
                    {
                        public bool Trueのみ { get; set; }
                        public bool Falseのみ { get; set; }

                        public bool AnyChecked() => Trueのみ || Falseのみ;
                    }
                }
                """
            });
        });
    }
}
