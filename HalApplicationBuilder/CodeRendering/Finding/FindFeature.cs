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

namespace HalApplicationBuilder.CodeRendering.Finding {
    internal class FindFeature {
        internal FindFeature(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _aggregate = aggregate;
            _ctx = ctx;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly CodeRenderingContext _ctx;

        internal string FindMethodReturnType => _aggregate.Item.ClassName;
        internal string FindMethodName => $"Find{_aggregate.Item.DisplayName.ToCSharpSafe()}";

        internal string RenderController() {
            var _controller = new WebClient.Controller(_aggregate.Item, _ctx);

            return $$"""
            namespace {{_ctx.Config.RootNamespace}} {
                using Microsoft.AspNetCore.Mvc;
                using {{_ctx.Config.EntityNamespace}};
            
                partial class {{_controller.ClassName}} {
                    [HttpGet("{{WebClient.Controller.FIND_ACTION_NAME}}/")]
                    public virtual IActionResult Find({{_aggregate.GetKeyMembers().Select(m => $"[FromQuery] {m.CSharpTypeName.ToNullable()} {m.MemberName}").Join(", ")}}) {
            {{_aggregate.GetKeyMembers().SelectTextTemplate(m => $$"""
                        if ({{m.MemberName}} == null) return BadRequest();
            """)}}
                        var instance = _dbContext.{{FindMethodName}}({{_aggregate.GetKeyMembers().Select(m => m.MemberName).Join(", ")}});
                        if (instance == null) {
                            return NotFound();
                        } else {
                            return this.JsonContent(instance);
                        }
                    }
                }
            }
            """;
        }

        internal string RenderEFCoreFindMethod() {

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
                        /// {{_aggregate.Item.DisplayName}}のキー情報から対象データの詳細を検索して返します。
                        /// </summary>
                        public {{FindMethodReturnType}}? {{FindMethodName}}({{_aggregate.GetKeyMembers().Select(m => $"{m.CSharpTypeName} {m.MemberName}").Join(", ")}}) {

                            {{WithIndent(RenderDbEntityLoading("this", "entity", m => m.MemberName, tracks: false, includeRefs: true), "            ")}}

                            if (entity == null) return null;

                            var aggregateInstance = {{_aggregate.Item.ClassName}}.{{AggregateDetail.FROM_DBENTITY}}(entity);
                            return aggregateInstance;
                        }
                    }
                }
                """;
        }

        internal string RenderDbEntityLoading(string dbContextVarName, string entityVarName, Func<AggregateMember.ValueMember, string> memberSelector, bool tracks, bool includeRefs) {

            // Include
            var includeEntities = _aggregate
                .EnumerateThisAndDescendants()
                .ToList();
            if (includeRefs) {
                var refEntities = _aggregate
                    .EnumerateThisAndDescendants()
                    .SelectMany(entity => entity.GetRefEdge())
                    .Select(edge => edge.Terminal);
                var refTargetAncestors = refEntities
                    .SelectMany(refTarget => refTarget.EnumerateAncestors())
                    .Select(edge => edge.Initial);
                var refTargetDescendants = refEntities
                    .SelectMany(refTarget => refTarget.EnumerateDescendants());

                includeEntities.AddRange(refEntities);
                includeEntities.AddRange(refTargetAncestors);
                includeEntities.AddRange(refTargetDescendants);
            }
            var paths = includeEntities
                .SelectMany(entity => entity.PathFromEntry())
                .Select(edge => edge.As<Aggregate>())
                .Select(edge => {
                    var source = edge.Source.As<Aggregate>();
                    var nav = new NavigationProperty(edge);
                    var prop = edge.Source.As<Aggregate>() == nav.Principal.Owner
                        ? nav.Principal.PropertyName
                        : nav.Relevant.PropertyName;
                    return new { source, prop };
                });

            // SingleOrDefault
            var keys = _aggregate
                .GetKeyMembers()
                .SelectTextTemplate(m => $"x.{m.GetDbColumn().Options.MemberName} == {memberSelector(m)}");

            return $$"""
                var {{entityVarName}} = {{dbContextVarName}}.{{_aggregate.Item.DbSetName}}
                {{If(tracks == false, () => $$"""
                    .AsNoTracking()
                """)}}
                {{paths.SelectTextTemplate(path => path.source == _aggregate ? $$"""
                    .Include(x => x.{{path.prop}})
                """ : $$"""
                    .ThenInclude(x => x.{{path.prop}})
                """)}}
                    .SingleOrDefault(x => {{WithIndent(keys, "                       && ")}});
                """;
        }

        internal string RenderCaller(Func<AggregateMember.ValueMember, string> nameSelector) {
            var members = _aggregate
                .GetKeyMembers()
                .Select(member => nameSelector(member))
                .Join($",{Environment.NewLine}    ");

            return $$"""
                {{FindMethodName}}(
                    {{members}})
                """;
        }
    }
}
