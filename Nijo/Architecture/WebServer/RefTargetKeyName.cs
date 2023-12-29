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

namespace Nijo.Architecture.WebServer {
    internal class RefTargetKeyName {
        internal RefTargetKeyName(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string CSharpClassName => $"{_aggregate.Item.ClassName}KeysAndNames";
        internal string TypeScriptTypeName => $"{_aggregate.Item.ClassName}KeysAndNames";

        internal IEnumerable<AggregateMember.AggregateMemberBase> GetMembers() {
            return GetKeyMembers().Union(GetNameMembers());
        }
        internal IEnumerable<AggregateMember.AggregateMemberBase> GetKeyMembers() {
            return _aggregate
                .GetKeys()
                .Where(m => m is AggregateMember.Ref
                         || m is AggregateMember.ValueMember vm && !vm.IsKeyOfRefTarget);
        }
        internal IEnumerable<AggregateMember.AggregateMemberBase> GetNameMembers() {
            return _aggregate
                .GetMembers()
                .OfType<AggregateMember.ValueMember>()
                .Where(member => member.IsDisplayName);
        }

        internal string RenderCSharpDeclaring() {
            static string GetAnnotations(AggregateMember.AggregateMemberBase member) {
                return member is AggregateMember.ValueMember v && v.IsKey
                    || member is AggregateMember.Ref r && r.Relation.IsPrimary()
                    ? "[Key]"
                    : string.Empty;
            }

            return $$"""
                public class {{CSharpClassName}} {
                {{GetMembers().SelectTextTemplate(member => $$"""
                    {{GetAnnotations(member)}}
                    public {{member.CSharpTypeName}}? {{member.MemberName}} { get; set; }
                """)}}
                }
                """;
        }
        internal string RenderTypeScriptDeclaring() {
            return $$"""
                export type {{TypeScriptTypeName}} = {
                {{GetMembers().SelectTextTemplate(member => $$"""
                  {{member.MemberName}}?: {{member.TypeScriptTypename}}
                """)}}
                }
                """;
        }
    }
}
