using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.KeywordSearching {
    internal class AggregateKeyName {
        internal AggregateKeyName(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string CSharpClassName => $"{_aggregate.Item.ClassName}KeyName";
        internal string TypeScriptTypeName => $"{_aggregate.Item.ClassName}KeyName";

        internal IEnumerable<AggregateMember.ValueMember> GetMembers() {
            return _aggregate.GetKeyMembers()
                .Concat(_aggregate.GetInstanceNameMembers())
                .DistinctBy(member => member.MemberName);
        }

        internal string RenderCSharpDeclaring() {
            return $$"""
                public class {{CSharpClassName}} {
                {{GetMembers().SelectTextTemplate(member => $$"""
                    public {{member.CSharpTypeName}} {{member.MemberName}} { get; set; }
                """)}}
                }
                """;
        }
        internal string RenderTypeScriptDeclaring() {
            return $$"""
                export type {{TypeScriptTypeName}} = {
                {{GetMembers().SelectTextTemplate(member => $$"""
                  {{member.MemberName}}: {{member.CSharpTypeName}}
                """)}}
                }
                """;
        }
    }
}
