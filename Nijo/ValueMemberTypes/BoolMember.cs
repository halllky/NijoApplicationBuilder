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
            var cast = ctx.SearchCondition.Metadata.Type.RenderCastToPrimitiveType();

            var fullpathNullable = ctx.SearchCondition.GetJoinedPathFromInstance("?.");
            var fullpathNotNull = ctx.SearchCondition.GetJoinedPathFromInstance(".");

            var queryFullPath = ctx.Query.GetFlattenArrayPath(E_CsTs.CSharp, out var isMany);
            var queryOwnerFullPath = queryFullPath.SkipLast(1);

            return $$"""
                if ({{fullpathNullable}}?.AnyChecked() == true) {
                    if ({{fullpathNotNull}}.Trueのみ) {
                {{If(isMany, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{queryOwnerFullPath.Join(".")}}.Any(y => y.{{ctx.Query.Metadata.PropertyName}} == true));
                """).Else(() => $$"""
                        {{query}} = {{query}}.Where(x => x.{{queryFullPath.Join(".")}} == true);
                """)}}
                    } else {
                {{If(isMany, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{queryOwnerFullPath.Join(".")}}.Any(y => y.{{ctx.Query.Metadata.PropertyName}} != true));
                """).Else(() => $$"""
                        {{query}} = {{query}}.Where(x => x.{{queryFullPath.Join(".")}} != true);
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

                    namespace {{ctx.Config.RootNamespace}} {
                        public class BooleanSearchCondition {
                            public bool Trueのみ { get; set; }
                            public bool Falseのみ { get; set; }

                            public bool AnyChecked() => Trueのみ || Falseのみ;
                        }
                    }
                    """,
            });
        });
    }
}
