using HalApplicationBuilder.CodeRendering;
using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal class AggregateInstanceTS {
        internal AggregateInstanceTS(GraphNode<AggregateInstance> aggregate, Config config) {
            _aggregate = aggregate;
            _config = config;
        }
        private readonly GraphNode<AggregateInstance> _aggregate;
        private readonly Config _config;

        internal string TypeName => _aggregate.Item.ClassName.ToCSharpSafe();
        internal void Render(ITemplate template) {

            void RenderBody(GraphNode<AggregateInstance> instance) {
                foreach (var prop in instance.GetSchalarProperties(_config)) {
                    template.WriteLine($"{prop.PropertyName}?: {prop.TypeScriptTypename}");
                }
                foreach (var member in instance.GetRefMembers()) {
                    template.WriteLine($"{member.RelationName}?: {AggregateInstanceKeyNamePairTS.DEF}");
                }
                foreach (var member in instance.GetChildMembers()) {
                    template.WriteLine($"{member.RelationName}?: {{");
                    template.PushIndent("  ");
                    RenderBody(member.Terminal);
                    template.PopIndent();
                    template.WriteLine($"}}");
                }
                foreach (var member in instance.GetChildrenMembers()) {
                    template.WriteLine($"{member.RelationName}?: {{");
                    template.PushIndent("  ");
                    RenderBody(member.Terminal);
                    template.PopIndent();
                    template.WriteLine($"}}[]");
                }
                foreach (var member in instance.GetVariationGroups().SelectMany(group => group.VariationAggregates.Values)) {
                    //template.WriteLine($"{group.Key}?: number"); // TODO GetSchalarPropertiesのほうでとれてきてしまう

                    template.WriteLine($"{member.RelationName}?: {{");
                    template.PushIndent("  ");
                    RenderBody(member.Terminal);
                    template.PopIndent();
                    template.WriteLine($"}}");
                }
            }

            template.WriteLine($"export type {TypeName} = {{");
            template.PushIndent("  ");
            RenderBody(_aggregate);
            template.PopIndent();
            template.WriteLine($"}}");
        }
    }
}
