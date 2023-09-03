using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering {
    internal class EnumDefs : TemplateBase {
        internal EnumDefs(IReadOnlyCollection<EnumDefinition> enumDefinitions, CodeRenderingContext ctx) {
            _enumDefinitions = enumDefinitions;
            _ctx = ctx;
        }

        private readonly IReadOnlyCollection<EnumDefinition> _enumDefinitions;
        private readonly CodeRenderingContext _ctx;

        public override string FileName => "Enum.cs";

        protected override string Template() {
            return $$"""
                namespace {{_ctx.Config.RootNamespace}} {
                    using System.ComponentModel.DataAnnotations;

                {{_enumDefinitions.SelectTextTemplate(def => $$"""
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
                """;
        }
    }
}
