using static HalApplicationBuilder.Features.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Features {
    internal class EnumDefs {

        internal static SourceFile Render() => new SourceFile {
            FileName = "Enum.cs",
            RenderContent = ctx => $$"""
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
