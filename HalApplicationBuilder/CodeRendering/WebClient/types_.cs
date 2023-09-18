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
            return $$"""
                import { UUID } from "uuidjs"

                {{_ctx.Schema.RootAggregates().SelectTextTemplate(root => $$"""
                // ------------------ {{root.Item.DisplayName}} ------------------
                {{root.EnumerateThisAndDescendants().SelectTextTemplate(Render)}}

                {{root.EnumerateThisAndDescendants().SelectTextTemplate(aggregate => new AggregateInstanceInitializerFunction(aggregate).Render())}}

                {{new Searching.SearchFeature(root.As<IEFCoreEntity>(), _ctx).RenderTypescriptTypeDef()}}

                """)}}
                """;
        }

        private string Render(GraphNode<Aggregate> aggregate) {
            if (aggregate.IsRoot()) {
                return $$"""
                    export type {{aggregate.Item.TypeScriptTypeName}} = {
                    {{aggregate.GetMembers().Where(m => m is not AggregateMember.KeyOfParent && m is not AggregateMember.KeyOfRefTarget).SelectTextTemplate(m => $$"""
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
                   {{aggregate.GetMembers().Where(m => m is not AggregateMember.KeyOfParent && m is not AggregateMember.KeyOfRefTarget).SelectTextTemplate(m => $$"""
                     {{m.PropertyName}}?: {{m.TypeScriptTypename}}
                   """)}}
                     {{AggregateInstanceBase.IS_LOADED}}?: boolean
                   }
                   """;
            }
        }
    }
}
