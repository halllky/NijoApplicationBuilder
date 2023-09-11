using HalApplicationBuilder.CodeRendering.Presentation;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class types : TemplateBase {
        internal types(CodeRenderingContext ctx) {
            _ctx = ctx;
        }

        private readonly CodeRenderingContext _ctx;

        public override string FileName => FILENAME;
        public static string ImportName => Path.GetFileNameWithoutExtension(FILENAME);
        public const string FILENAME = "types.ts";

        protected override string Template() {
            return _ctx.Schema.RootAggregates().SelectTextTemplate(root => $$"""
                // ------------------ {{root.Item.DisplayName}} ------------------
                {{root.EnumerateThisAndDescendants().SelectTextTemplate(Render)}}

                {{root.EnumerateThisAndDescendants().SelectTextTemplate(aggregate => new AggregateInstanceInitializerFunction(aggregate).Render())}}

                {{new Searching.SearchFeature(root.As<IEFCoreEntity>(), _ctx).RenderTypescriptTypeDef()}}
                """);
        }

        private string Render(GraphNode<Aggregate> aggregate) {
            if (aggregate.IsRoot()) {
                return $$"""
                    export type {{aggregate.Item.TypeScriptTypeName}} = {
                    {{aggregate.GetMembers().Where(m => m is not AggregateMember.ParentPK && m is not AggregateMember.RefTargetMember).SelectTextTemplate(m => $$"""
                      {{m.PropertyName}}?: {{m.TypeScriptTypename}}
                    """)}}
                      {{AggregateInstanceBase.INSTANCE_KEY}}?: string
                      {{AggregateInstanceBase.INSTANCE_NAME}}?: string
                      {{AggregateInstanceBase.IS_LOADED}}?: boolean
                    }
                    """;

            } else {
                return $$"""
                   export type {{aggregate.Item.TypeScriptTypeName}} = {
                   {{aggregate.GetMembers().Where(m => m is not AggregateMember.ParentPK && m is not AggregateMember.RefTargetMember).SelectTextTemplate(m => $$"""
                     {{m.PropertyName}}?: {{m.TypeScriptTypename}}
                   """)}}
                     {{AggregateInstanceBase.IS_LOADED}}?: boolean
                   }
                   """;
            }
        }


        #region Initializer function
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
        #endregion Initializer function
    }
}
