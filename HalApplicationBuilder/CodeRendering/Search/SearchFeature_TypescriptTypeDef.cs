using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Search {
    partial class SearchFeature {
        internal void RenderTypescriptTypeDef(ITemplate template) {
            template.WriteLine($$"""
                export type {{SearchConditionClassName}} = {
                {{Members.Select(member => $$"""
                  {{member.ConditionPropName}}?: {{member.Type.GetTypeScriptTypeName()}}
                """)}}
                }
                export type {{SearchResultClassName}} = {
                {{Members.Select(member => $$"""
                  {{member.SearchResultPropName}}?: {{member.Type.GetTypeScriptTypeName()}}
                """)}}
                }
                """);
        }
    }
}
