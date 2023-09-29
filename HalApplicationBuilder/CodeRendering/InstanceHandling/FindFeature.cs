using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;

namespace HalApplicationBuilder.CodeRendering.InstanceHandling {
    internal class FindFeature {
        internal FindFeature(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        internal string FindMethodReturnType => _aggregate.Item.ClassName;
        internal string FindMethodName => $"Find{_aggregate.Item.DisplayName.ToCSharpSafe()}";
        private const string ACTION_NAME = "detail";

        internal string GetUrlStringForReact(IEnumerable<string> keyVariables) {
            var controller = new WebClient.Controller(_aggregate.Item);
            var encoded = keyVariables.Select(key => $"${{window.encodeURI({key})}}");
            return $"`/{controller.SubDomain}/{ACTION_NAME}/{encoded.Join("/")}`";
        }

        internal string RenderController(CodeRenderingContext _ctx) {
            var keys = _aggregate.GetKeys();
            var controller = new WebClient.Controller(_aggregate.Item);

            return $$"""
            namespace {{_ctx.Config.RootNamespace}} {
                using Microsoft.AspNetCore.Mvc;
                using {{_ctx.Config.EntityNamespace}};
            
                partial class {{controller.ClassName}} {
                    [HttpGet("{{ACTION_NAME}}/{{keys.Select(m => "{" + m.MemberName + "}").Join("/")}}")]
                    public virtual IActionResult Find({{keys.Select(m => $"{m.CSharpTypeName.ToNullable()} {m.MemberName}").Join(", ")}}) {
            {{_aggregate.GetKeys().SelectTextTemplate(m => $$"""
                        if ({{m.MemberName}} == null) return BadRequest();
            """)}}
                        var instance = _dbContext.{{FindMethodName}}({{keys.Select(m => m.MemberName).Join(", ")}});
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

        internal string RenderEFCoreMethod(CodeRenderingContext _ctx) {

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
                        public {{FindMethodReturnType}}? {{FindMethodName}}({{_aggregate.GetKeys().Select(m => $"{m.CSharpTypeName} {m.MemberName}").Join(", ")}}) {

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
                .GetKeys()
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
                .GetKeys()
                .Select(member => nameSelector(member))
                .Join($",{Environment.NewLine}    ");

            return $$"""
                {{FindMethodName}}(
                    {{members}})
                """;
        }
    }
}
