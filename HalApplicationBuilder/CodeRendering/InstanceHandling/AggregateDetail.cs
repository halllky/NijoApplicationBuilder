using HalApplicationBuilder.CodeRendering.Presentation;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.InstanceHandling {
    internal class AggregateDetail {
        internal AggregateDetail(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        protected readonly GraphNode<Aggregate> _aggregate;

        internal virtual string ClassName => _aggregate.Item.ClassName;

        internal virtual IEnumerable<AggregateMember.AggregateMemberBase> GetMembers() {
            return _aggregate
                .GetMembers()
                .Where(m => m is not AggregateMember.KeyOfParent
                         && m is not AggregateMember.KeyOfRefTarget);
        }

        internal virtual string RenderCSharp(CodeRenderingContext ctx) {
            var fromDbEntity = new FromDbEntityRenderer(_aggregate, ctx);
            var toDbEntity = new ToDbEntityRenderer(_aggregate, ctx);

            return $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    
                    /// <summary>
                    /// {{_aggregate.Item.DisplayName}}のデータ1件の詳細を表すクラスです。
                    /// </summary>
                    public partial class {{ClassName}} : {{AggregateInstanceBase.CLASS_NAME}} {
                {{GetMembers().SelectTextTemplate(prop => $$"""
                        public {{prop.CSharpTypeName}} {{prop.MemberName}} { get; set; }
                """)}}

                {{If(_aggregate.IsRoot(), () => $$"""
                        {{WithIndent(toDbEntity.Render(), "        ")}}
                        {{WithIndent(fromDbEntity.Render(), "        ")}}
                """)}}
                    }
                }
                """;
        }

        internal virtual string RenderTypeScript(CodeRenderingContext ctx) {
            return $$"""
                export type {{_aggregate.Item.TypeScriptTypeName}} = {
                {{GetMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}?: {{m.TypeScriptTypename}}
                """)}}
                {{If(_aggregate.IsRoot(), () => $$"""
                  {{AggregateInstanceBase.INSTANCE_KEY}}?: string
                  {{AggregateInstanceBase.INSTANCE_NAME}}?: string
                """)}}
                  {{AggregateInstanceBase.IS_LOADED}}?: boolean
                }
                """;
        }
    }
}
