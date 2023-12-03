using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.AspNetCore.Mvc.Formatters.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.InstanceHandling {
    internal class RefTargetKeyName {
        internal RefTargetKeyName(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string CSharpClassName => $"{_aggregate.Item.ClassName}KeysAndNames";
        internal string TypeScriptTypeName => $"{_aggregate.Item.ClassName}KeysAndNames";

        internal IEnumerable<Member> GetKeysAndNames() {
            return GetKeys().Union(GetNames());
        }
        internal IEnumerable<Member> GetKeys() {
            return _aggregate
                .GetKeys()
                .Where(m => m is not AggregateMember.ValueMember vm
                         || !vm.IsKeyOfRefTarget)
                .Select(m => new Member(m));
        }
        internal IEnumerable<Member> GetNames() {
            return _aggregate
                .GetMembers()
                .OfType<AggregateMember.ValueMember>()
                .Where(member => member.IsDisplayName)
                .Select(m => new Member(m));
        }

        internal string RenderCSharpDeclaring() {
            return $$"""
                public class {{CSharpClassName}} {
                {{GetKeysAndNames().SelectTextTemplate(member => $$"""
                    [{{member.GetAnnotations().Join(", ")}}]
                    public {{member.AggMember.CSharpTypeName}} {{member.AggMember.MemberName}} { get; set; }
                """)}}
                }
                """;
        }
        internal string RenderTypeScriptDeclaring() {
            return $$"""
                export type {{TypeScriptTypeName}} = {
                {{GetKeysAndNames().SelectTextTemplate(member => $$"""
                  {{member.AggMember.MemberName}}: {{member.AggMember.CSharpTypeName}}
                """)}}
                }
                """;
        }

        internal class Member : ValueObject {
            internal Member(AggregateMember.AggregateMemberBase source) {
                AggMember = source;
            }
            internal AggregateMember.AggregateMemberBase AggMember { get; }
            public string MemberName => AggMember.MemberName;

            internal IEnumerable<string> GetAnnotations() {
                var list = new List<string>();
                if (AggMember is AggregateMember.ValueMember v && v.IsKey) list.Add("Key");
                if (AggMember is AggregateMember.Ref r && r.Relation.IsPrimary()) list.Add("Key");
                if (AggMember is AggregateMember.ValueMember v2 && v2.IsDisplayName) list.Add("DisplayName");
                return list;
            }
            /// <summary>
            /// <see cref="GetKeysAndNames"/> での重複除去のために値オブジェクトにしている
            /// </summary>
            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return AggMember;
            }
            public override string ToString() {
                return AggMember.ToString();
            }
        }
    }
}
