using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    internal class AggregateInstanceInitializerFunction {
        internal AggregateInstanceInitializerFunction(GraphNode<Aggregate> instance) {
            _instance = instance;
        }
        private readonly GraphNode<Aggregate> _instance;

        internal string FunctionName => $"create{_instance.Item.TypeScriptTypeName}";

        internal string Render() {
            var children = _instance
                .GetChildrenEdges()
                .Select(edge => new {
                    Key = edge.RelationName,
                    Value = $"[]",
                });
            var child = _instance
                .GetChildEdges()
                .Select(edge => new {
                    Key = edge.RelationName,
                    Value = $"{new AggregateInstanceInitializerFunction(edge.Terminal).FunctionName}()",
                });
            var variation = _instance
                .GetVariationGroups()
                .SelectMany(group => group.VariationAggregates.Values)
                .Select(edge => new {
                    Key = edge.RelationName,
                    Value = $"{new AggregateInstanceInitializerFunction(edge.Terminal).FunctionName}()",
                });
            var variationSwitch = _instance
                .GetVariationGroups()
                .Select(group => new {
                    Key = group.GroupName,
                    Value = $"'{group.VariationAggregates.First().Key}'",
                });

            return $$"""
                    export const {{FunctionName}} = (): {{_instance.Item.TypeScriptTypeName}} => ({
                    {{children.Concat(child).Concat(variation).Concat(variationSwitch).SelectTextTemplate(item => $$"""
                      {{item.Key}}: {{item.Value}},
                    """)}}
                    })
                    """;
        }
    }
}
