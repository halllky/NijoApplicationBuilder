using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.QueryModelModules;
using Nijo.Parts.CSharp;
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
/// 日付時刻型
/// </summary>
internal class DateTimeMember : IValueMemberType {
    string IValueMemberType.TypePhysicalName => "DateTime";
    string IValueMemberType.SchemaTypeName => "datetime";
    string IValueMemberType.CsDomainTypeName => "DateTime";
    string IValueMemberType.CsPrimitiveTypeName => "DateTime";
    string IValueMemberType.TsTypeName => "string";
    UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.MemberConstraintBase;
    string IValueMemberType.DisplayName => "日付時刻";

    void IValueMemberType.Validate(XElement element, SchemaParseContext context, Action<XElement, string> addError) {
        // 日付時刻型の検証
        // 必要に応じて日付範囲の制約などを検証するコードをここに追加できます
    }

    ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
        FilterCsTypeName = $"{FromTo.CS_CLASS_NAME}<DateTime?>",
        FilterTsTypeName = "{ from?: string; to?: string }",
        RenderTsNewObjectFunctionValue = () => "{ from: undefined, to: undefined }",
        RenderFiltering = ctx => RangeSearchRenderer.RenderRangeSearchFiltering(ctx),
    };

    void IValueMemberType.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
        // 特になし
    }

    string IValueMemberType.RenderCreateDummyDataValueBody(CodeRenderingContext ctx) {
        return $$"""
            return member.IsKey
                ? DateTime.Now.AddHours(context.GetNextSequence())
                : DateTime.Now.AddDays(context.Random.Next(-365, 365)).AddHours(context.Random.Next(-24, 24));
            """;
    }

    void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
        // 特になし
    }
}
