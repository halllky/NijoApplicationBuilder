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

namespace Nijo.ValueMemberTypes;

/// <summary>
/// 値オブジェクト型
/// 値オブジェクトを表すC#のクラスを参照する型
/// </summary>
internal class ValueObjectMember : IValueMemberType {
    string IValueMemberType.TypePhysicalName => _ctx.GetPhysicalName(_xElement);
    string IValueMemberType.SchemaTypeName => _ctx.GetPhysicalName(_xElement);
    string IValueMemberType.CsDomainTypeName => _ctx.GetPhysicalName(_xElement);
    string IValueMemberType.CsPrimitiveTypeName => "string";
    string IValueMemberType.TsTypeName => _ctx.GetPhysicalName(_xElement);
    UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.StringMemberConstraint;

    private readonly XElement _xElement;
    private readonly SchemaParseContext _ctx;

    public ValueObjectMember(XElement xElement, SchemaParseContext ctx) {
        _xElement = xElement;
        _ctx = ctx;
    }

    void IValueMemberType.Validate(XElement element, SchemaParseContext context, Action<XElement, string> addError) {
        // 値オブジェクト型の検証
        // 必要に応じて値オブジェクトの存在確認などを検証するコードをここに追加できます
    }

    ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
        FilterCsTypeName = "string",
        FilterTsTypeName = "string",
        RenderTsNewObjectFunctionValue = () => "undefined",
        RenderFiltering = ctx => {
            // TODO 部分一致検索以外も作る
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
            return new {{_ctx.GetPhysicalName(_xElement)}}(string.Concat(Enumerable.Range(0, member.MaxLength ?? 12).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[context.Random.Next(0, 36)])));
            """;
    }

    void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
        // 特になし
    }
}
