using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Search {
    partial class SearchFeature {
        internal void RenderDbContextMethod(ITemplate template) {

            var selectClause = Members.Select(m => new {
                resultMemberName = m.SearchResultPropName,
                dbColumnPath = m.CorrespondingDbColumn.Owner
                    .PathFromEntry()
                    .Select(edge => edge.RelationName)
                    .Union(new[] { m.CorrespondingDbColumn.PropertyName })
                    .Join("."),
            });
            var instanceNameProp = Members.SingleOrDefault(m => m.IsInstanceName)?.SearchResultPropName;

            template.WriteLine($$"""
                namespace {{Context.Config.EntityNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.EntityFrameworkCore.Infrastructure;

                    partial class {{Context.Config.DbContextName}} {
                        /// <summary>
                        /// {{DisplayName}}の一覧検索を行います。
                        /// </summary>
                        public IEnumerable<{{SearchResultClassName}}> {{DbContextSearchMethodName}}({{SearchConditionClassName}} param) {
                            var query = this.{{DbEntity.Item.DbSetName}}.Select(e => new {{SearchResultClassName}} {
                {{selectClause.Select(x => $$"""
                                {{x.resultMemberName}} = e.{{x.dbColumnPath}},
                """)}}
                            });

                {{Members.Select(member => member.Type.SearchBehavior switch {
                SearchBehavior.Ambiguous => $$"""
                            if (!string.IsNullOrWhiteSpace(param.{{member.ConditionPropName}})) {
                                var trimmed = param.{{member.ConditionPropName}}.Trim();
                                query = query.Where(x => x.{{member.SearchResultPropName}}.Contains(trimmed));
                            }
                """,
                SearchBehavior.Range => $$"""
                            if (param.{{member.ConditionPropName}}.{{FromTo.FROM}} != default) {
                                query = query.Where(x => x.{{member.SearchResultPropName}} >= param.{{member.ConditionPropName}}.{{FromTo.FROM}});
                            }
                            if (param.{{member.ConditionPropName}}.{{FromTo.TO}} != default) {
                                query = query.Where(x => x.{{member.SearchResultPropName}} <= param.{{member.ConditionPropName}}.{{FromTo.TO}});";
                            }
                """,
                SearchBehavior.Strict => new[] { "string", "string?" }.Contains(member.Type.GetCSharpTypeName()) ? $$"""
                            if (!string.IsNullOrWhiteSpace(param.{{member.ConditionPropName}})) {
                                query = query.Where(x => x.{{member.SearchResultPropName}} == param.{{member.ConditionPropName}});
                            }
                """ : $$"""
                            if (param.{{member.ConditionPropName}} != default) {
                                query = query.Where(x => x.{{member.SearchResultPropName}} == param.{{member.ConditionPropName}});
                            }
                """,
                _ => string.Empty,
                })}}
                            if (param.{{SearchCondition.PAGE_PROP_NAME}} != null) {
                                const int PAGE_SIZE = 20;
                                var skip = param.{{SearchCondition.PAGE_PROP_NAME}}.Value * PAGE_SIZE;
                                query = query.Skip(skip).Take(PAGE_SIZE);
                            }

                            foreach (var item in query) {
                                item.{{SearchResult.INSTANCE_KEY_PROP_NAME}} = {{InstanceKey.CLASS_NAME}}.{{InstanceKey.CREATE}}(new object?[] {
                {{Members.Where(member => member.IsInstanceKey).Select(member => $$"""
                                    item.{{member.SearchResultPropName}},
                """)}}
                                }).ToString();

                {{(instanceNameProp == null
                ? $$"""
                                // 表示名に使用するプロパティが定義されていないため、キーを表示名に使用します。
                                item.<#=SearchResult.INSTANCE_NAME_PROP_NAME#> = item.<#=SearchResult.INSTANCE_KEY_PROP_NAME#>;
                """
                : $$"""
                                item.<#=SearchResult.INSTANCE_NAME_PROP_NAME#> = item.<#=_search.GetInstanceNamePropName()#>;
                """)}}

                                yield return item;
                            }
                        }
                    }
                }
                """);
        }
    }
}
