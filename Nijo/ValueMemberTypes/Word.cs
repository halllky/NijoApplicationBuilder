using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
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

namespace Nijo.ValueMemberTypes;

/// <summary>
/// 単語型
/// </summary>
internal class Word : IValueMemberType {
    string IValueMemberType.TypePhysicalName => "Word";
    string IValueMemberType.SchemaTypeName => "word";
    string IValueMemberType.CsDomainTypeName => "string";
    string IValueMemberType.CsPrimitiveTypeName => "string";
    string IValueMemberType.TsTypeName => "string";
    UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.StringMemberConstraint;

    void IValueMemberType.Validate(XElement element, SchemaParseContext context, Action<XElement, string> addError) {
        // 特に追加の検証はありません。
        // 必要に応じて、属性の最大長や最小長などの制約を検証するコードをここに追加できます。
    }

    ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
        FilterCsTypeName = "string",
        FilterTsTypeName = "string",
        RenderTsNewObjectFunctionValue = () => "undefined",
        RenderFiltering = ctx => {
            var query = ctx.Query.Root.Name;
            var fullpathNullable = ctx.SearchCondition.GetJoinedPathFromInstance("?.");
            var fullpathNotNull = ctx.SearchCondition.GetJoinedPathFromInstance(".");

            var queryFullPath = ctx.Query.GetFlattenArrayPath(E_CsTs.CSharp, out var isMany);
            var queryOwnerFullPath = queryFullPath.SkipLast(1);

            return $$"""
                if (!string.IsNullOrWhiteSpace({{fullpathNullable}})) {
                    var trimmed = {{fullpathNotNull}}.Trim();
                {{If(isMany, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{queryOwnerFullPath.Join("!.")}}!.Any(y => y.{{ctx.Query.Metadata.PropertyName}}!.Contains(trimmed)));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{queryFullPath.Join("!.")}}!.Contains(trimmed));
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
