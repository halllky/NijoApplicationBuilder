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
    string IValueMemberType.TsTypeName => $"Util.{_ctx.GetPhysicalName(_xElement)}";
    UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.StringMemberConstraint;
    string IValueMemberType.DisplayName => "値オブジェクト型";

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
            var query = ctx.Query.Root.Name;
            var fullpathNullable = ctx.SearchCondition.GetJoinedPathFromInstance(E_CsTs.CSharp, "?.");
            var fullpathNotNull = ctx.SearchCondition.GetJoinedPathFromInstance(E_CsTs.CSharp, ".");

            var queryFullPath = ctx.Query.GetFlattenArrayPath(E_CsTs.CSharp, out var isMany);
            var queryOwnerFullPath = queryFullPath.SkipLast(1);

            return $$"""
                if (!string.IsNullOrWhiteSpace({{fullpathNullable}})) {
                    var trimmed = {{fullpathNotNull}}.Trim();
                {{If(isMany, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{queryOwnerFullPath.Join("!.")}}.Any(y => y.{{ctx.Query.Metadata.PropertyName}}!.Contains(trimmed)));
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
            return member.IsKey
                ? new {{_ctx.GetPhysicalName(_xElement)}}($"VO_{context.GetNextSequence():D10}_{string.Concat(Enumerable.Range(0, 6).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[context.Random.Next(0, 36)]))}")
                : new {{_ctx.GetPhysicalName(_xElement)}}(string.Concat(Enumerable.Range(0, member.MaxLength ?? 12).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[context.Random.Next(0, 36)])));
            """;
    }

    void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
        // 特になし
    }
}
