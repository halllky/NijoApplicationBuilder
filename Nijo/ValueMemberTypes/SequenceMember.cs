using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.QueryModelModules;
using Nijo.SchemaParsing;
using Nijo.Util.DotnetEx;
using Nijo.Parts.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.ValueMemberTypes {
    /// <summary>
    /// シーケンス。RDBMS登録時にデータベース側で採番処理がなされる整数型。
    /// </summary>
    internal class SequenceMember : IValueMemberType {
        string IValueMemberType.TypePhysicalName => "Sequence";
        string IValueMemberType.SchemaTypeName => "sequence";
        string IValueMemberType.CsDomainTypeName => "int";
        string IValueMemberType.CsPrimitiveTypeName => "int";
        string IValueMemberType.TsTypeName => "number";

        void IValueMemberType.Validate(XElement element, SchemaParseContext context, Action<XElement, string> addError) {
            // シーケンス型の検証
            // シーケンス型は通常データベース側で自動的に値が設定されるため、特別な検証は不要です
        }

        ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
            FilterCsTypeName = $"{FromTo.CS_CLASS_NAME}<int?>",
            FilterTsTypeName = "{ from?: number; to?: number }",
            RenderTsNewObjectFunctionValue = () => "{ from: undefined, to: undefined }",
            RenderFiltering = ctx => RangeSearchRenderer.RenderRangeSearchFiltering(ctx),
        };

        UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.NumberMemberConstraint;

        void IValueMemberType.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // 特になし
        }
        void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
            // 特になし
        }

        string IValueMemberType.RenderCreateDummyDataValueBody(CodeRenderingContext ctx) {
            return $$"""
                return member.IsKey
                    ? context.GetNextSequence()
                    : context.Random.Next(0, 1000);
                """;
        }

        #region 採番処理
        internal const string SET_METHOD = "GenerateAndSetSequenceValue";
        #endregion 採番処理
    }
}
