using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalApplicationBuilder.CodeRendering.Presentation;

namespace HalApplicationBuilder.CodeRendering.InstanceHandling {
    internal class AggregateCreateCommand : AggregateDetail {
        internal AggregateCreateCommand(GraphNode<Aggregate> aggregate) : base(aggregate) {
            if (!aggregate.IsRoot()) throw new ArgumentException();
        }

        internal override string ClassName => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}CreateCommand";

        internal override string RenderCSharp(CodeRenderingContext ctx) {
            return $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    
                    /// <summary>
                    /// {{_aggregate.Item.DisplayName}}のデータ作成コマンドです。
                    /// </summary>
                    public partial class {{ClassName}} : {{AggregateInstanceBase.CLASS_NAME}} {
                {{GetAggregateDetailMembers().SelectTextTemplate(prop => $$"""
                        public {{prop.CSharpTypeName}} {{prop.MemberName}} { get; set; }
                """)}}

                        {{WithIndent(ToDbEntity(ctx), "        ")}}
                    }
                }
                """;
        }
    }
}
