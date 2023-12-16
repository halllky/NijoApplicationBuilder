using Nijo.Features.InstanceHandling;
using Nijo.Features.KeywordSearching;
using Nijo.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.WebClient {
#pragma warning disable IDE1006 // 命名スタイル
    internal class types {
#pragma warning restore IDE1006 // 命名スタイル

        internal static string ImportName => Path.GetFileNameWithoutExtension(FILENAME);
        private const string FILENAME = "types.ts";

        internal static SourceFile Render() {
            return new SourceFile {
                FileName = FILENAME,
                RenderContent = ctx =>  $$"""
                    import { UUID } from "uuidjs"

                    {{ctx.Schema.RootAggregates().SelectTextTemplate(root => $$"""
                    // ------------------ {{root.Item.DisplayName}} ------------------
                    {{root.EnumerateThisAndDescendants().SelectTextTemplate(aggregate => new AggregateDetail(aggregate).RenderTypeScript(ctx))}}

                    {{root.EnumerateThisAndDescendants().SelectTextTemplate(aggregate => new TSInitializerFunction(aggregate).Render())}}

                    {{root.EnumerateThisAndDescendants().SelectTextTemplate(aggregate => new RefTargetKeyName(aggregate).RenderTypeScriptDeclaring())}}

                    {{new Searching.AggregateSearchFeature(root).GetMultiView().RenderTypeScriptTypeDef(ctx)}}

                    """)}}
                    """,
            };
        }
    }
}
