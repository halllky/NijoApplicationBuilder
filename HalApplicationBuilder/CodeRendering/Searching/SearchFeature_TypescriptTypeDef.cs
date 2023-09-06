using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Searching {
    partial class SearchFeature {
        internal void RenderTypescriptTypeDef(ITemplate template) {
            template.WriteLine($$"""
                export type {{SearchConditionClassName}} = {
                {{Members.SelectTextTemplate(member => $$"""
                  {{member.ConditionPropName}}?: {{member.Type.GetTypeScriptTypeName()}}
                """)}}
                }
                export type {{SearchResultClassName}} = {
                {{Members.SelectTextTemplate(member => $$"""
                  {{member.SearchResultPropName}}?: {{member.Type.GetTypeScriptTypeName()}}
                """)}}
                }
                """);
        }
    }
}
