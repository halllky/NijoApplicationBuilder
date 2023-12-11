using HalApplicationBuilder.Features.InstanceHandling;
using HalApplicationBuilder.Features.KeywordSearching;
using HalApplicationBuilder.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Features.WebClient {
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
                {{root.EnumerateThisAndDescendants().SelectTextTemplate(aggregate => new AggregateDetail(aggregate).RenderTypeScript(_ctx))}}

                {{root.EnumerateThisAndDescendants().SelectTextTemplate(aggregate => new TSInitializerFunction(aggregate).Render())}}

                {{root.EnumerateThisAndDescendants().SelectTextTemplate(aggregate => new RefTargetKeyName(aggregate).RenderTypeScriptDeclaring())}}

                {{new Searching.AggregateSearchFeature(root).GetMultiView().RenderTypeScriptTypeDef(_ctx)}}

                """)}}

                {{_ctx.Schema.DataViews().SelectTextTemplate(dataView => $$"""
                // ------------------ {{dataView.Item.DisplayName}} ------------------
                {{new DataViewRenderer(dataView).GetMultiView().RenderTypeScriptTypeDef(_ctx)}}
                """)}}
                """;
        }
    }
}
