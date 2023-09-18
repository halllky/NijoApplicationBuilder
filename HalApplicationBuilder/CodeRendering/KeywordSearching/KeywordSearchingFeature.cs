using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;

namespace HalApplicationBuilder.CodeRendering.KeywordSearching {
    internal class KeywordSearchingFeature {
        internal KeywordSearchingFeature(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _aggregate = aggregate;
            _ctx = ctx;
        }
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly CodeRenderingContext _ctx;

        private string GetActionName() {
            return _aggregate.IsRoot()
                ? $"list-by-keyword"
                : $"list-by-keyword-{_aggregate.Item.UniqueId}";
        }
        internal string GetUri() {
            var controller = GetController();
            return $"/{controller.SubDomain}/{GetActionName()}";
        }

        private WebClient.Controller GetController() {
            var root = _aggregate.GetRoot();
            return new WebClient.Controller(root.Item, _ctx);
        }

        internal string DbcontextMeghodName => $"SearchByKeyword{_aggregate.Item.DisplayName.ToCSharpSafe()}";

        private const int LIST_BY_KEYWORD_MAX = 100;

        internal string RenderController() {
            var controller = GetController();
            return $$"""
                namespace {{_ctx.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc;
                    using {{_ctx.Config.EntityNamespace}};
                
                    partial class {{controller.ClassName}} {
                        [HttpGet("{{GetActionName()}}")]
                        public virtual IActionResult SearchByKeyword{{_aggregate.Item.UniqueId}}([FromQuery] string? keyword) {
                            var items = _dbContext.{{DbcontextMeghodName}}(keyword);
                            return this.JsonContent(items);
                        }
                    }
                }
                """;
        }

        internal string RenderDbContextMethod() {
            var members = _aggregate
                .GetMembers()
                .OfType<AggregateMember.ValueMember>()
                .Where(member => member.IsKey || member.IsDisplayName)
                .Select(item => new {
                    item.IsKey,
                    item.IsDisplayName,
                    QueryResultPropertyName = item.MemberName,
                    QueryResultPropertyNameAsString = item.Options.MemberType.GetCSharpTypeName() == "string"
                        ? item.MemberName
                        : $"{item.MemberName}.ToString()",
                    EFCorePropertyFullPath = item.GetDbColumn().GetFullPath(_aggregate.As<IEFCoreEntity>()).Join("."),
                })
                .ToArray();

            const string LIKE = "like";
            var select = members
                .Select(x => $"{x.QueryResultPropertyName} = e.{x.EFCorePropertyFullPath},");
            var where = members
                .Select(x => $"EF.Functions.Like(item.{x.QueryResultPropertyNameAsString}, {LIKE})")
                .Join(Environment.NewLine + " || ");
            var orderBy = members
                .Select((x, i) => i == 0
                    ? $".OrderBy(item => item.{x.QueryResultPropertyName})"
                    : $".ThenBy(item => item.{x.QueryResultPropertyName})");

            var instanceKey = AggregateInstanceKeyNamePair.RenderKeyJsonConverting(members
                .Where(x => x.IsKey)
                .Select(x => $"item.{x.QueryResultPropertyName}"));
            var instanceName = members
                .Where(x => x.IsDisplayName)
                .Select(x => $"item.{x.QueryResultPropertyNameAsString}").Join(" + ");

            return $$"""
                namespace {{_ctx.Config.EntityNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.EntityFrameworkCore.Infrastructure;
                
                    partial class {{_ctx.Config.DbContextName}} {
                        /// <summary>
                        /// {{_aggregate.Item.DisplayName}}をキーワードで検索します。
                        /// </summary>
                        public IEnumerable<{{AggregateInstanceKeyNamePair.CLASSNAME}}> {{DbcontextMeghodName}}(string? keyword) {
                            var query = this.{{_aggregate.Item.DbSetName}}.Select(e => new {
                                {{WithIndent(select, "                ")}}
                            });
                
                            if (!string.IsNullOrWhiteSpace(keyword)) {
                                var {{LIKE}} = $"%{keyword.Trim().Replace("%", "\\%")}%";
                                query = query.Where(item => {{WithIndent(where, "                            ")}});
                            }
                
                            query = query
                                {{WithIndent(orderBy, "                ")}}
                                .Take({{LIST_BY_KEYWORD_MAX + 1}});
                
                            return query
                                .AsEnumerable()
                                .Select(item => new {{AggregateInstanceKeyNamePair.CLASSNAME}} {
                                    {{AggregateInstanceKeyNamePair.KEY}} = {{WithIndent(instanceKey, "                    ")}},
                                    {{AggregateInstanceKeyNamePair.NAME}} = {{WithIndent(instanceName, "                    ")}},
                                });
                        }
                    }
                }
                """;
        }
    }
}
