using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HalApplicationBuilder.CodeRendering {
    internal class AggregateKey {
        internal AggregateKey(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string CSharpClassName => $"{_aggregate.Item.ClassName}Key";
        internal string TypeScriptTypeName => $"{_aggregate.Item.ClassName}Key";

        internal const string DISPLAYNAME_CS = "__DisplayName";
        internal const string DISPLAYNAME_TS = "__displayName";

        internal IEnumerable<AggregateMember.AggregateMemberBase> GetMembers() {
            foreach (var member in _aggregate.GetMembers()) {
                if (member is AggregateMember.ValueMember vMember) {
                    if (!vMember.IsKey) continue;
                    if (member is AggregateMember.KeyOfRefTarget) continue; // 子集約は親集約のプロパティに含まれているため親集約のキーは除外
                    if (member is AggregateMember.KeyOfParent) continue; // Refは参照先の各メンバーではなく参照先のキークラスをもつので除外

                    yield return member;

                } else if (member is AggregateMember.Ref refMember) {
                    if (!refMember.Relation.IsPrimary()) continue;

                    yield return member;
                }
            }
        }

        internal string RenderCSharpDeclaring() {
            return $$"""
                public class {{CSharpClassName}} {
                {{GetMembers().SelectTextTemplate(member => $$"""
                    public {{member.CSharpTypeName}} {{member.MemberName}} { get; set; }
                """)}}

                    [JsonPropertyName("{{DISPLAYNAME_TS}}")]
                    public string? {{DISPLAYNAME_CS}} { get; set; }
                }
                """;
        }
        internal string RenderTypeScriptDeclaring() {
            return $$"""
                export type {{TypeScriptTypeName}} = {
                {{GetMembers().SelectTextTemplate(member => $$"""
                  {{member.MemberName}}: {{member.CSharpTypeName}}
                """)}}
                  {{DISPLAYNAME_TS}}?: string
                }
                """;
        }

        internal string RenderConvertingFromDbEntity(Func<AggregateMember.ValueMember, string> right) {
            //var names = refProp.MemberAggregate
            //    .GetInstanceNameMembers()
            //    .Select(member => member.GetDbColumn().GetFullPath(rootInstance.As<IEFCoreEntity>()).Join("."))
            //    .Select(fullpath => $"{rootInstanceName}.{fullpath}?.ToString()");

            string RenderBody(AggregateMember.AggregateMemberBase member) {
                if (member is AggregateMember.ValueMember valueMember) {
                    return $"{member.MemberName} = {right(valueMember)}";

                } else if (member is AggregateMember.Ref refMember) {
                    return $"{member.MemberName} = {new AggregateKey(refMember.MemberAggregate).RenderConvertingFromDbEntity(right)}";

                } else {
                    throw new NotImplementedException();
                }
            }

            return $$"""
                new {{CSharpClassName}} {
                {{GetMembers().SelectTextTemplate(member => $$"""
                    {{WithIndent(RenderBody(member), "    ")}},
                """)}}
                    {{DISPLAYNAME_CS}} = string.Empty,
                }
                """;
        }

        internal string RenderConvertingToDbEntity(string path) {

            IEnumerable<string> RenderBody(AggregateMember.AggregateMemberBase member) {
                if (member is AggregateMember.ValueMember valueMember) {
                    yield return $"{valueMember.GetDbColumn().Options.MemberName} = {path}?.{valueMember.MemberName},";

                } else if (member is AggregateMember.Ref refMember) {
                    foreach (var fk in refMember.GetForeignKeys()) {
                        yield return $"{fk.GetDbColumn().Options.MemberName} = {path}.{refMember.MemberName}?.{fk.MemberName}";
                    }

                } else {
                    throw new NotImplementedException();
                }
            }

            return GetMembers()
                .SelectMany(RenderBody)
                .SelectTextTemplate(line => line);
        }
    }
}
