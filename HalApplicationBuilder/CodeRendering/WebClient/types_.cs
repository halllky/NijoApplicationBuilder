using HalApplicationBuilder.CodeRendering.InstanceHandling;
using HalApplicationBuilder.CodeRendering.KeywordSearching;
using HalApplicationBuilder.Core;
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
            return $$"""
                import { UUID } from "uuidjs"

                {{_ctx.Schema.RootAggregates().SelectTextTemplate(root => $$"""
                // ------------------ {{root.Item.DisplayName}} ------------------
                {{root.EnumerateThisAndDescendants().SelectTextTemplate(aggregate => new AggregateDetail(aggregate).RenderTypeScript(_ctx))}}

                {{root.EnumerateThisAndDescendants().SelectTextTemplate(aggregate => new TSInitializerFunction(aggregate).Render())}}

                {{root.EnumerateThisAndDescendants().SelectTextTemplate(aggregate => new AggregateKeyName(aggregate).RenderTypeScriptDeclaring())}}

                {{new Searching.SearchFeature(root.As<IEFCoreEntity>(), _ctx).RenderTypescriptTypeDef()}}

                """)}}
                """;
        }
    }
}
