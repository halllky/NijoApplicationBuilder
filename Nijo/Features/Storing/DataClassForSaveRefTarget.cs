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
    /// <see cref="DataClassForSave"/> のうち主キーのみピックアップされたもの。
    /// </summary>
    internal class DataClassForSaveRefTarget {
        internal DataClassForSaveRefTarget(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string CSharpClassName => $"{_aggregate.Item.PhysicalName}Keys";
        /// <summary>
        /// 登録更新リクエスト時に参照先のインスタンスがDB未登録で主キーが定まっていない可能性があるため、
        /// 参照先インスタンスのUUIDで特定できるように文字列で参照する。
        /// </summary>
        internal string TypeScriptTypeName => $"Util.ItemKey";

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
                /// <summary>
                /// ほかの集約が{{_aggregate.Item.DisplayName}}を参照するときに必要になる、どの{{_aggregate.Item.DisplayName}}を指し示すかのキー情報。
                /// </summary>
                public class {{CSharpClassName}} {
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                    {{GetAnnotations(member)}}
                    public {{member.CSharpTypeName}}? {{member.MemberName}} { get; set; }
                """)}}
                }
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
                foreach (var m in new DataClassForSaveRefTarget(agg).GetOwnMembers()) {
                    if (m is AggregateMember.ValueMember vm) {
                        yield return $$"""
                            {{vm.MemberName}} = {{vmKeys.Single(x => x.Member.Declared == vm.Declared).VarName}},
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

            string ConvertOrThrow(string element, string varName, string csharpTypeName, string memberName) {
                if (csharpTypeName == "string") {
                    return $$"""
                        var {{varName}} = {{element}}.GetString();
                        """;

                } else if (csharpTypeName == "int") {
                    return $$"""
                        if (!{{element}}.TryGetInt32(out var {{varName}}))
                            throw new InvalidOperationException($"{{_aggregate.Item.DisplayName}}のキーの変換に失敗しました。{{memberName}}の位置の値が{{csharpTypeName}}型ではありません: {{{element}}.GetString()}");
                        """;

                } else if (csharpTypeName == "decimal") {
                    return $$"""
                        if (!{{element}}.TryGetDecimal(out var {{varName}}))
                            throw new InvalidOperationException($"{{_aggregate.Item.DisplayName}}のキーの変換に失敗しました。{{memberName}}の位置の値が{{csharpTypeName}}型ではありません: {{{element}}.GetString()}");
                        """;

                } else if (csharpTypeName == "DateTime") {
                    return $$"""
                        if (!{{element}}.TryGetDateTime(out var {{varName}}))
                            throw new InvalidOperationException($"{{_aggregate.Item.DisplayName}}のキーの変換に失敗しました。{{memberName}}の位置の値が{{csharpTypeName}}型ではありません: {{{element}}.GetString()}");
                        """;

                } else if (csharpTypeName == "bool") {
                    return $$"""
                        var {{varName}} = {{element}}.GetBoolean();
                        """;

                } else {
                    throw new NotImplementedException();
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
                        var objArray = Util.{{UtilityClass.PARSE_JSON}}<JsonElement[]>(jsonArray);

                        if (objArray.Length != {{vmKeys.Length}})
                            throw new InvalidOperationException($"{{_aggregate.Item.DisplayName}}のキーの変換に失敗しました。キーの数は{{vmKeys.Length}}個であるべきところ、実際は{objArray.Length}個でした。");

                {{vmKeys.SelectTextTemplate(x => $$"""
                        {{WithIndent(ConvertOrThrow($"objArray[{x.Index}]", x.VarName, x.Member.CSharpTypeName, x.Member.MemberName), "        ")}}

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
