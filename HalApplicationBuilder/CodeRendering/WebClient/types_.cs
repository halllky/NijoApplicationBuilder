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

        private void Render(GraphNode<AggregateInstance> instance) {
            WriteLine($"export type {instance.Item.TypeScriptTypeName} = {{");
            if (instance.IsRoot()) {
                WriteLine($"  {AggregateInstanceBase.INSTANCE_KEY}?: string");
                WriteLine($"  {AggregateInstanceBase.INSTANCE_NAME}?: string");
            }
            foreach (var prop in instance.GetSchalarProperties(_ctx.Config)) {
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
            foreach (var member in instance.GetVariationGroups().SelectMany(group => group.VariationAggregates.Values)) {
                WriteLine($"  {member.RelationName}?: {member.Terminal.Item.TypeScriptTypeName}");
            }
            WriteLine($"}}");
        }
        private void Render(SearchCondition searchCondition) {

            WriteLine($"export type {searchCondition.TypeScriptTypeName} = {{");
            foreach (var member in searchCondition.GetMembers()) {
                WriteLine($"  {member.Name}?: {member.Type.GetTypeScriptTypeName()}");
            }
            WriteLine($"}}");
        }
        private void Render(SearchResult searchResult) {

            WriteLine($"export type {searchResult.TypeScriptTypeName} = {{");
            foreach (var member in searchResult.GetMembers()) {
                WriteLine($"  {member.Name}?: {member.Type.GetTypeScriptTypeName()}");
            }
            WriteLine($"}}");
        }


        #region Initializer function
        internal class AggregateInstanceInitializerFunction {
            internal AggregateInstanceInitializerFunction(GraphNode<AggregateInstance> instance) {
                _instance = instance;
            }
            private readonly GraphNode<AggregateInstance> _instance;

            internal string FunctionName => $"create{_instance.Item.TypeScriptTypeName}";

            internal void Render(ITemplate template) {
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
                var children = _instance
                    .GetChildrenMembers()
                    .Select(edge => new {
                        Key = edge.RelationName,
                        Value = $"[]",
                    });

                template.WriteLine($"export const {FunctionName} = (): {_instance.Item.TypeScriptTypeName} => ({{");
                foreach (var item in child.Concat(variation).Concat(children)) {
                    template.WriteLine($"  {item.Key}: {item.Value},");
                }
                template.WriteLine($"}})");
            }
        }
        #endregion Initializer function
    }
}
