using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Searching {
    partial class SearchFeature {
        internal string RenderTypescriptTypeDef() {
            return $$"""
                export type {{SearchConditionClassName}} = {
                {{Members.SelectTextTemplate(member => $$"""
                  {{member.ConditionPropName}}?: {{member.DbColumn.Options.MemberType.GetTypeScriptTypeName()}}
                """)}}
                }
                export type {{SearchResultClassName}} = {
                {{Members.SelectTextTemplate(member => $$"""
                  {{member.SearchResultPropName}}?: {{member.DbColumn.Options.MemberType.GetTypeScriptTypeName()}}
                """)}}
                }
                """;
        }
    }
}
