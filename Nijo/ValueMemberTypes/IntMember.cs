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
/// 整数型
/// </summary>
internal class IntMember : IValueMemberType {
    string IValueMemberType.TypePhysicalName => "Integer";
    string IValueMemberType.SchemaTypeName => "int";
    string IValueMemberType.CsDomainTypeName => "int";
    string IValueMemberType.CsPrimitiveTypeName => "int";
    string IValueMemberType.TsTypeName => "number";
    UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.NumberMemberConstraint;

    void IValueMemberType.Validate(XElement element, SchemaParseContext context, Action<XElement, string> addError) {
        // 整数型の検証
        // 必要に応じて最小値や最大値の制約を検証するコードをここに追加できます
    }

    ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
        FilterCsTypeName = $"{FromTo.CS_CLASS_NAME}<int?>",
        FilterTsTypeName = "{ from?: number; to?: number }",
        RenderTsNewObjectFunctionValue = () => "{ from: undefined, to: undefined }",
        RenderFiltering = ctx => RangeSearchRenderer.RenderRangeSearchFiltering(ctx),
    };

    void IValueMemberType.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
        // 特になし
    }

    string IValueMemberType.RenderCreateDummyDataValueBody(CodeRenderingContext ctx) {
        return $$"""
            return context.Random.Next(0, 1000);
            """;
    }

    void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
        // 特になし
    }
}
