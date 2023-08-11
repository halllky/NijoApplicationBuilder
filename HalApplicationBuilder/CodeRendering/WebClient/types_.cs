using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
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
        public const string FILENAME = "types.ts";

        private IEnumerable<(GraphNode<AggregateInstance>, SearchCondition, SearchResult)> GetAggregateInstances() {
            return _ctx.Schema
                .RootAggregates()
                .Select(root => (
                    root.GetInstanceClass(),
                    new SearchCondition(root.GetDbEntity().AsEntry()),
                    new SearchResult(root.GetDbEntity().AsEntry())
                ));
        }

        private void Render(GraphNode<AggregateInstance> aggregateInstance) {

            void RenderBody(GraphNode<AggregateInstance> instance) {
                foreach (var prop in instance.GetSchalarProperties(_ctx.Config)) {
                    WriteLine($"{prop.PropertyName}?: {prop.TypeScriptTypename}");
                }
                foreach (var member in instance.GetRefMembers()) {
                    WriteLine($"{member.RelationName}?: {AggregateInstanceKeyNamePairTS.DEF}");
                }
                foreach (var member in instance.GetChildMembers()) {
                    WriteLine($"{member.RelationName}?: {{");
                    PushIndent("  ");
                    RenderBody(member.Terminal);
                    PopIndent();
                    WriteLine($"}}");
                }
                foreach (var member in instance.GetChildrenMembers()) {
                    WriteLine($"{member.RelationName}?: {{");
                    PushIndent("  ");
                    RenderBody(member.Terminal);
                    PopIndent();
                    WriteLine($"}}[]");
                }
                foreach (var group in instance.GetVariationGroups()) {
                    //WriteLine($"{group.Key}?: number"); // TODO GetSchalarPropertiesのほうでとれてきてしまう

                    foreach (var member in group) {
                        WriteLine($"{member.RelationName}?: {{");
                        PushIndent("  ");
                        RenderBody(member.Terminal);
                        PopIndent();
                        WriteLine($"}}");
                    }
                }
            }

            WriteLine($"export type {aggregateInstance.Item.TypeScriptTypeName} = {{");
            PushIndent("  ");
            RenderBody(aggregateInstance);
            PopIndent();
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
    }
}
