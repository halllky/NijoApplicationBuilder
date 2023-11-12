using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
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

        internal IEnumerable<AggregateMember.AggregateMemberBase> GetKeysAndNames() {
            var keys = GetKeys().ToDictionary(x => x.MemberName);
            var names = _aggregate
                .GetMembers()
                .OfType<AggregateMember.ValueMember>()
                .Where(m => m.IsDisplayName && !keys.ContainsKey(m.MemberName));
            return keys.Values.Concat(names);
        }
        internal IEnumerable<AggregateMember.AggregateMemberBase> GetKeys() {
            return _aggregate
                .GetKeys()
                .Where(m => m is not AggregateMember.KeyOfRefTarget);
        }
        internal IEnumerable<AggregateMember.AggregateMemberBase> GetNames() {
            return _aggregate
                .GetMembers()
                .OfType<AggregateMember.ValueMember>()
                .Where(member => member.IsDisplayName);
        }

        internal string RenderCSharpDeclaring() {
            return $$"""
                public class {{CSharpClassName}} {
                {{GetKeysAndNames().SelectTextTemplate(member => $$"""
                    public {{member.CSharpTypeName}} {{member.MemberName}} { get; set; }
                """)}}
                }
                """;
        }
        internal string RenderTypeScriptDeclaring() {
            return $$"""
                export type {{TypeScriptTypeName}} = {
                {{GetKeysAndNames().SelectTextTemplate(member => $$"""
                  {{member.MemberName}}: {{member.CSharpTypeName}}
                """)}}
                }
                """;
        }
    }
}
