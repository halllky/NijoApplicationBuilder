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
            var fullpath = ctx.Member.GetPathFromEntry().ToArray();
            var pathFromSearchCondition = fullpath.AsSearchConditionFilter(E_CsTs.CSharp).ToArray();
            var whereFullpath = fullpath.AsSearchResult().ToArray();
            var fullpathNullable = $"{ctx.SearchCondition}.{pathFromSearchCondition.Join("?.")}";
            var fullpathNotNull = $"{ctx.SearchCondition}.{pathFromSearchCondition.Join(".")}";
            var isArray = fullpath.Any(node => node is ChildrenAggregate);

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
            return new {{_ctx.GetPhysicalName(_xElement)}}(string.Concat(Enumerable.Range(0, member.MaxLength ?? 12).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[context.Random.Next(0, 36)])));
            """;
    }

    void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
        // 特になし
    }
}
