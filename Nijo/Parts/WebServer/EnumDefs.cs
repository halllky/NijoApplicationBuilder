using static Nijo.Util.CodeGenerating.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;

namespace Nijo.Parts.WebServer {
    internal class EnumDefs {

        internal static SourceFile Render(ICodeRenderingContext ctx) => new SourceFile {
            FileName = "Enum.cs",
            RenderContent = () => $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using System.ComponentModel.DataAnnotations;

                {{ctx.Schema.EnumDefinitions.SelectTextTemplate(def => $$"""
                    public enum {{def.Name}} {
                {{def.Items.SelectTextTemplate(item => $$"""
                {{If(!string.IsNullOrWhiteSpace(item.DisplayName), () => $$"""
                        [Display(Name = "{{item.DisplayName}}")]
                """)}}
                        {{item.PhysicalName}} = {{item.Value}},
                """)}}
                    }
                """)}}
                }
                """,
        };
    }
}
