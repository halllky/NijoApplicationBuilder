using HalApplicationBuilder.CodeRendering.Presentation;
using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                {{root.EnumerateThisAndDescendants().SelectTextTemplate(aggregate => $$"""
                    {{new AggregateInstanceInitializerFunction(aggregate).Render()}}
                """)}}

                {{new Searching.SearchFeature(root.As<IEFCoreEntity>(), _ctx).RenderTypescriptTypeDef()}}
                """);
        }

        private string Render(GraphNode<Aggregate> instance) {
            var builder = new StringBuilder();

            builder.AppendLine($"export type {instance.Item.TypeScriptTypeName} = {{");
            if (instance.IsRoot()) {
                builder.AppendLine($"  {AggregateInstanceBase.INSTANCE_KEY}?: string");
                builder.AppendLine($"  {AggregateInstanceBase.INSTANCE_NAME}?: string");
            }
            foreach (var prop in instance.GetSchalarProperties()) {
                builder.AppendLine($"  {prop.PropertyName}?: {prop.TypeScriptTypename}");
            }
            foreach (var member in instance.GetRefMembers()) {
                builder.AppendLine($"  {member.RelationName}?: {AggregateInstanceKeyNamePair.TS_DEF}");
            }
            foreach (var member in instance.GetChildMembers()) {
                builder.AppendLine($"  {member.RelationName}?: {member.Terminal.Item.TypeScriptTypeName}");
            }
            foreach (var member in instance.GetChildrenMembers()) {
                builder.AppendLine($"  {member.RelationName}?: {member.Terminal.Item.TypeScriptTypeName}[]");
            }
            foreach (var member in instance.GetVariationSwitchProperties(_ctx.Config)) {
                builder.AppendLine($"  {member.CorrespondingDbColumn.PropertyName}?: {member.TypeScriptTypename}");
            }
            foreach (var member in instance.GetVariationGroups().SelectMany(group => group.VariationAggregates.Values)) {
                builder.AppendLine($"  {member.RelationName}?: {member.Terminal.Item.TypeScriptTypeName}");
            }
            builder.AppendLine($"}}");

            return builder.ToString();
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
                    .GetChildrenMembers()
                    .Select(edge => new {
                        Key = edge.RelationName,
                        Value = $"[]",
                    });
                var child = _instance
                    .GetChildMembers()
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
