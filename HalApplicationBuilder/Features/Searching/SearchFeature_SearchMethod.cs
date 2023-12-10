using HalApplicationBuilder.Features.Util;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.Features.TemplateTextHelper;

namespace HalApplicationBuilder.Features.Searching {
    partial class SearchFeature {
        internal string RenderDbContextMethod() {
            var appSrv = new ApplicationService(Context.Config);
            var members = GetMembers().ToArray();
            var selectClause = members.Select(m => new {
                resultMemberName = m.SearchResultPropName,
                dbColumnPath = m.DbColumn.GetFullPath().Join("."),
            });

            return $$"""
                namespace {{Context.Config.RootNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.EntityFrameworkCore.Infrastructure;

                    partial class {{appSrv.ClassName}} {
                        /// <summary>
                        /// {{DisplayName}}の一覧検索を行います。
                        /// </summary>
                        public virtual IEnumerable<{{SearchResultClassName}}> {{AppServiceSearchMethodName}}({{SearchConditionClassName}} param) {
                            var query = {{appSrv.DbContext}}.{{DbEntity.Item.DbSetName}}.Select(e => new {{SearchResultClassName}} {
                {{selectClause.SelectTextTemplate(x => $$"""
                                {{x.resultMemberName}} = e.{{x.dbColumnPath}},
                """)}}
                            });

                {{members.SelectTextTemplate(member => 
                    If(member.DbColumn.Options.MemberType.SearchBehavior == SearchBehavior.Ambiguous, () => $$"""
                            if (!string.IsNullOrWhiteSpace(param.{{member.ConditionPropName}})) {
                                var trimmed = param.{{member.ConditionPropName}}.Trim();
                                query = query.Where(x => x.{{member.SearchResultPropName}}.Contains(trimmed));
                            }
                """).ElseIf(member.DbColumn.Options.MemberType.SearchBehavior == SearchBehavior.Range, () => $$"""
                            if (param.{{member.ConditionPropName}}.{{FromTo.FROM}} != default) {
                                query = query.Where(x => x.{{member.SearchResultPropName}} >= param.{{member.ConditionPropName}}.{{FromTo.FROM}});
                            }
                            if (param.{{member.ConditionPropName}}.{{FromTo.TO}} != default) {
                                query = query.Where(x => x.{{member.SearchResultPropName}} <= param.{{member.ConditionPropName}}.{{FromTo.TO}});
                            }
                """).ElseIf(member.DbColumn.Options.MemberType.SearchBehavior == SearchBehavior.Strict && new[] { "string", "string?" }.Contains(member.DbColumn.Options.MemberType.GetCSharpTypeName()), () => $$"""
                            if (!string.IsNullOrWhiteSpace(param.{{member.ConditionPropName}})) {
                                query = query.Where(x => x.{{member.SearchResultPropName}} == param.{{member.ConditionPropName}});
                            }
                """).ElseIf(member.DbColumn.Options.MemberType.SearchBehavior == SearchBehavior.Strict, () => $$"""
                            if (param.{{member.ConditionPropName}} != default) {
                                query = query.Where(x => x.{{member.SearchResultPropName}} == param.{{member.ConditionPropName}});
                            }
                """))}}
                            if (param.{{SEARCHCONDITION_PAGE_PROP_NAME}} != null) {
                                const int PAGE_SIZE = 20;
                                var skip = param.{{SEARCHCONDITION_PAGE_PROP_NAME}}.Value * PAGE_SIZE;
                                query = query.Skip(skip).Take(PAGE_SIZE);
                            }

                            return query.AsEnumerable();
                        }
                    }
                }
                """;
        }
    }
}