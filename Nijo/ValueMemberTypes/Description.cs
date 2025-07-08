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

namespace Nijo.ValueMemberTypes {
    /// <summary>
    /// 文章型
    /// </summary>
    internal class Description : IValueMemberType {
        string IValueMemberType.TypePhysicalName => "Description";
        string IValueMemberType.SchemaTypeName => "description";
        string IValueMemberType.CsDomainTypeName => "string";
        string IValueMemberType.CsPrimitiveTypeName => "string";
        string IValueMemberType.TsTypeName => "string";
        UiConstraint.E_Type IValueMemberType.UiConstraintType => UiConstraint.E_Type.StringMemberConstraint;
        string IValueMemberType.DisplayName => "文章型";

        void IValueMemberType.Validate(XElement element, SchemaParseContext context, Action<XElement, string> addError) {
            // 文章型の検証
            // 必要に応じて最大長などの制約を検証するコードをここに追加できます
        }

        ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => new() {
            FilterCsTypeName = "string",
            FilterTsTypeName = "string",
            RenderTsNewObjectFunctionValue = () => "undefined",
            RenderFiltering = ctx => {
                var query = ctx.Query.Root.Name;
                var fullpathNullable = ctx.SearchCondition.GetJoinedPathFromInstance(E_CsTs.CSharp, "?.");
                var fullpathNotNull = ctx.SearchCondition.GetJoinedPathFromInstance(E_CsTs.CSharp, "!.");

                var queryFullPath = ctx.Query.GetFlattenArrayPath(E_CsTs.CSharp, out var isMany);
                var queryOwnerFullPath = queryFullPath.SkipLast(1);

                return $$"""
                    if (!string.IsNullOrWhiteSpace({{fullpathNullable}})) {
                        var trimmed = {{fullpathNotNull}}!.Trim();
                    {{If(isMany, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{queryOwnerFullPath.Join("!.")}}!.Any(y => y.{{ctx.Query.Metadata.GetPropertyName(E_CsTs.CSharp)}}!.Contains(trimmed)));
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
                if (member.IsKey) {
                    string prefix = "DESC_";
                    string seqValue = context.GetNextSequence().ToString("D10");
                    string separator = "_";

                    // 接頭辞とシーケンス値と区切り文字の長さを計算
                    int fixedPartLength = prefix.Length + seqValue.Length + separator.Length;

                    // 利用可能な残り文字数を計算（最大長から固定部分の長さを引く）
                    int availableLength = Math.Max(1, (member.MaxLength ?? 50) - fixedPartLength);

                    // 残りの文字数分だけランダム文字を生成
                    string randomPart = string.Concat(Enumerable.Range(0, availableLength)
                        .Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[context.Random.Next(0, 36)]));

                    return $"{prefix}{seqValue}{separator}{randomPart}";
                }
                else {
                    return string.Concat(Enumerable.Range(0, member.MaxLength ?? 12)
                        .Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+-=[]{}\\\"|;:,.<>?"[context.Random.Next(0, 63)]));
                }
                """;
        }

        void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
            // 特になし
        }
    }
}
