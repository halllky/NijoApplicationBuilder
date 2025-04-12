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
            FilterCsTypeName = "FromTo<int?>",
            FilterTsTypeName = "{ from?: number, to?: number }",
            RenderTsNewObjectFunctionValue = () => "{}",
            RenderFiltering = ctx => {
                var fullpath = ctx.Member
                    .GetPathFromEntry()
                    .ToArray();
                var strArrayPath = fullpath
                    .AsSearchConditionFilter(E_CsTs.CSharp)
                    .ToArray();
                var nullableFullPathFrom = strArrayPath.Join("?.") + "?.From";
                var nullableFullPathTo = strArrayPath.Join("?.") + "?.To";
                var fullPathFrom = strArrayPath.Join(".") + ".From";
                var fullPathTo = strArrayPath.Join(".") + ".To";

                var isArray = fullpath.Any(node => node is ChildrenAggreagte);
                var whereFullpath = fullpath.AsSearchResult().ToArray();
                var query = ctx.Query;

                return $$"""
                    if ({{nullableFullPathFrom}} != null && {{nullableFullPathTo}} != null) {
                        var from = {{fullPathFrom}};
                        var to = {{fullPathTo}};
                    {{If(isArray, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{ctx.Member.PhysicalName}} >= from && y.{{ctx.Member.PhysicalName}} <= to));
                    """).Else(() => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}} >= from && x.{{whereFullpath.Join(".")}} <= to);
                    """)}}

                    } else if ({{nullableFullPathFrom}} != null) {
                        var from = {{fullPathFrom}};
                    {{If(isArray, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{ctx.Member.PhysicalName}} >= from));
                    """).Else(() => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}} >= from);
                    """)}}

                    } else if ({{nullableFullPathTo}} != null) {
                        var to = {{fullPathTo}};
                    {{If(isArray, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{ctx.Member.PhysicalName}} <= to));
                    """).Else(() => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}} <= to);
                    """)}}
                    }
                    """;
            },
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
                return context.Random.Next();
                """;
        }

        #region 採番処理
        internal const string SET_METHOD = "GenerateAndSetSequenceValue";
        #endregion 採番処理
    }
}
