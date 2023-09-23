using HalApplicationBuilder.CodeRendering.InstanceHandling;
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
            return new WebClient.Controller(root.Item);
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
            const string LIKE = "like";
            var keyName = new AggregateKeyName(_aggregate);

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
                        public IEnumerable<{{keyName.CSharpClassName}}> {{DbcontextMeghodName}}(string? keyword) {
                            var query = this.{{_aggregate.Item.DbSetName}}.Select(e => new {{keyName.CSharpClassName}} {
                {{keyName.GetKeysAndNames().SelectTextTemplate(m => $$"""
                                {{m.MemberName}} = e.{{m.GetDbColumn().GetFullPath(_aggregate.As<IEFCoreEntity>()).Join(".")}},
                """)}}
                            });
                
                            if (!string.IsNullOrWhiteSpace(keyword)) {
                                var {{LIKE}} = $"%{keyword.Trim().Replace("%", "\\%")}%";
                                query = query.Where(item => {{WithIndent(keyName.GetKeysAndNames().SelectTextTemplate(m => $"EF.Functions.Like(item.{m.MemberName}, {LIKE})"), "                                         || ")}});
                            }

                            query = query
                                .OrderBy(m => m.{{keyName.GetKeysAndNames().First().MemberName}})
                                .Take({{LIST_BY_KEYWORD_MAX + 1}});

                            return query
                                .AsEnumerable();
                        }
                    }
                }
                """;
        }
    }
}
