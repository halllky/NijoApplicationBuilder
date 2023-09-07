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
    partial class types : ITemplate {
        internal types(CodeRenderingContext ctx) {
            _ctx = ctx;
        }

        private readonly CodeRenderingContext _ctx;

        public string FileName => FILENAME;
        public static string ImportName => Path.GetFileNameWithoutExtension(FILENAME);
        public const string FILENAME = "types.ts";

        private void Render(GraphNode<IAggregateInstance> instance) {
            WriteLine($"export type {instance.Item.TypeScriptTypeName} = {{");
            if (instance.IsRoot()) {
                WriteLine($"  {AggregateInstanceBase.INSTANCE_KEY}?: string");
                WriteLine($"  {AggregateInstanceBase.INSTANCE_NAME}?: string");
            }
            foreach (var prop in instance.GetSchalarProperties()) {
                WriteLine($"  {prop.PropertyName}?: {prop.TypeScriptTypename}");
            }
            foreach (var member in instance.GetRefMembers()) {
                WriteLine($"  {member.RelationName}?: {AggregateInstanceKeyNamePair.TS_DEF}");
            }
            foreach (var member in instance.GetChildMembers()) {
                WriteLine($"  {member.RelationName}?: {member.Terminal.Item.TypeScriptTypeName}");
            }
            foreach (var member in instance.GetChildrenMembers()) {
                WriteLine($"  {member.RelationName}?: {member.Terminal.Item.TypeScriptTypeName}[]");
            }
            foreach (var member in instance.GetVariationSwitchProperties(_ctx.Config)) {
                WriteLine($"  {member.CorrespondingDbColumn.PropertyName}?: {member.TypeScriptTypename}");
            }
            foreach (var member in instance.GetVariationGroups().SelectMany(group => group.VariationAggregates.Values)) {
                WriteLine($"  {member.RelationName}?: {member.Terminal.Item.TypeScriptTypeName}");
            }
            WriteLine($"}}");
        }
        private void Render(GraphNode<IEFCoreEntity> dbEntity) {
            var searchFeature = new Searching.SearchFeature(dbEntity, _ctx);
            searchFeature.RenderTypescriptTypeDef(this);
        }


        #region Initializer function
        internal class AggregateInstanceInitializerFunction {
            internal AggregateInstanceInitializerFunction(GraphNode<IAggregateInstance> instance) {
                _instance = instance;
            }
            private readonly GraphNode<IAggregateInstance> _instance;

            internal string FunctionName => $"create{_instance.Item.TypeScriptTypeName}";

            internal void Render(ITemplate template) {
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

                template.WriteLine($"export const {FunctionName} = (): {_instance.Item.TypeScriptTypeName} => ({{");
                foreach (var item in children.Concat(child).Concat(variation).Concat(variationSwitch)) {
                    template.WriteLine($"  {item.Key}: {item.Value},");
                }
                template.WriteLine($"}})");
            }
        }
        #endregion Initializer function
    }
}
