using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Features.Searching {
    partial class SearchFeature {
        internal string RenderTypescriptTypeDef() {
            var members = GetMembers().ToArray();

            return $$"""
                export type {{SearchConditionClassName}} = {
                {{members.SelectTextTemplate(member => $$"""
                  {{member.ConditionPropName}}?: {{member.DbColumn.Options.MemberType.GetTypeScriptTypeName()}}
                """)}}
                }
                export type {{SearchResultClassName}} = {
                {{members.SelectTextTemplate(member => $$"""
                  {{member.SearchResultPropName}}?: {{member.DbColumn.Options.MemberType.GetTypeScriptTypeName()}}
                """)}}
                }
                """;
        }
    }
}
