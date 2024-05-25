using Nijo.Core;
using Nijo.Util.DotnetEx;
using Microsoft.AspNetCore.Mvc.Formatters.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;
using Nijo.Parts.Utility;

namespace Nijo.Features.Storing {
    /// <summary>
    /// DBに保存される、参照先の集約の情報。
    /// <see cref="TransactionScopeDataClass"/> のうち主キーのみピックアップされたもの。
    /// </summary>
    internal class DataClassForUpdateRefTarget {
        internal DataClassForUpdateRefTarget(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string CSharpClassName => $"{_aggregate.Item.ClassName}Keys";
        internal string TypeScriptTypeName => $"{_aggregate.Item.ClassName}Keys";

        /// <summary>
        /// このクラスで宣言されるメンバーのみを列挙する。
        /// つまり、親や参照先は入れ子オブジェクトとして列挙されるに留まり、親や参照先のキー等は列挙されない。
        /// </summary>
        internal IEnumerable<AggregateMember.AggregateMemberBase> GetOwnMembers() {
            return _aggregate
                .GetKeys()
                .Where(m => m is AggregateMember.Ref
                         || m is AggregateMember.Parent
                         || m is AggregateMember.ValueMember vm && vm.Declared.Owner == vm.Owner);
        }

        internal string RenderCSharpDeclaring() {
            static string GetAnnotations(AggregateMember.AggregateMemberBase member) {
                return member is AggregateMember.ValueMember v && v.IsKey
                    || member is AggregateMember.Ref r && r.Relation.IsPrimary()
                    || member is AggregateMember.Parent
                    ? "[Key]"
                    : string.Empty;
            }

            return $$"""
                public class {{CSharpClassName}} {
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                    {{GetAnnotations(member)}}
                    public {{member.CSharpTypeName}}? {{member.MemberName}} { get; set; }
                """)}}
                }
                """;
        }
        /// <summary>
        /// TODO: 冗長な定義
        /// </summary>
        internal string RenderTypeScriptDeclaring() {
            return $$"""
                export type {{TypeScriptTypeName}} = Util.ItemKey
                """;
        }

        #region C#用のJSONコンバータ
        internal string CsJsonConverterName => $"{CSharpClassName}JsonValueConverter";

        internal string RenderServerSideJsonConverter() {
            var vmKeys = _aggregate.AsEntry().GetKeys().OfType<AggregateMember.ValueMember>().Select((member, i) => new {
                Member = member,
                Index = i,
                VarName = $"{member.MemberName.ToCSharpSafe()}Value",
            }).ToArray();

            IEnumerable<string> RenderBody(GraphNode<Aggregate> agg) {
                foreach (var m in new DataClassForUpdateRefTarget(agg).GetOwnMembers()) {
                    if (m is AggregateMember.ValueMember vm) {
                        yield return $$"""
                            {{vm.MemberName}} = ({{vm.CSharpTypeName}}?){{vmKeys.Single(x => x.Member.Declared == vm.Declared).VarName}},
                            """;

                    } else if (m is AggregateMember.RelationMember rm) {
                        yield return $$"""
                            {{rm.MemberName}} = new() {
                                {{WithIndent(RenderBody(rm.MemberAggregate), "    ")}}
                            },
                            """;
                    }
                }
            }

            return $$"""
                /// <summary>
                /// <see cref="{{CSharpClassName}}"/> 型のプロパティの値が
                /// C#とHTTPリクエスト・レスポンスの間で変換されるときの処理を定義します。
                /// </summary>
                public class {{CsJsonConverterName}} : JsonConverter<{{CSharpClassName}}?> {
                    public override {{CSharpClassName}}? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                        var jsonArray = reader.GetString();
                        if (jsonArray == null) return null;
                        var objArray = Util.{{UtilityClass.PARSE_JSON_AS_OBJARR}}(jsonArray);

                {{vmKeys.SelectTextTemplate(x => $$"""
                        var {{x.VarName}} = objArray.ElementAtOrDefault({{x.Index}});
                        if ({{x.VarName}} != null && {{x.VarName}} is not {{x.Member.CSharpTypeName}})
                            throw new InvalidOperationException($"{{CSharpClassName}}の値の変換に失敗しました。{{x.Member.MemberName}}の位置の値が{{x.Member.CSharpTypeName}}型ではありません: {{{x.VarName}}}");

                """)}}
                        return new {{CSharpClassName}} {
                            {{WithIndent(RenderBody(_aggregate), "            ")}}
                        };
                    }

                    public override void Write(Utf8JsonWriter writer, {{CSharpClassName}}? value, JsonSerializerOptions options) {
                        if (value == null) {
                            writer.WriteNullValue();

                        } else {
                            object?[] objArray = [
                {{vmKeys.SelectTextTemplate(x => $$"""
                                value.{{x.Member.Declared.GetFullPath().Join("?.")}},
                """)}}
                            ];
                            var jsonArray = objArray.{{UtilityClass.TO_JSON}}();
                            writer.WriteStringValue(jsonArray);
                        }
                    }
                }
                """;
        }
        #endregion C#用のJSONコンバータ
    }
}
