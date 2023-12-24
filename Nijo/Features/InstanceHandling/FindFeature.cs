using Nijo.Features.Util;
using Nijo.Core;
using Nijo.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nijo.Util.CodeGenerating.TemplateTextHelper;
using Nijo.Util.CodeGenerating;

namespace Nijo.Features.InstanceHandling {
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

        internal string RenderController() {
            var keys = _aggregate
                .GetKeys()
                .Where(m => m is AggregateMember.ValueMember)
                .ToArray();
            var controller = new WebClient.Controller(_aggregate.Item);

            return $$"""
                [HttpGet("{{ACTION_NAME}}/{{keys.Select(m => "{" + m.MemberName + "}").Join("/")}}")]
                public virtual IActionResult Find({{keys.Select(m => $"{m.CSharpTypeName}? {m.MemberName}").Join(", ")}}) {
                {{keys.SelectTextTemplate(m => $$"""
                    if ({{m.MemberName}} == null) return BadRequest();
                """)}}
                    var instance = _applicationService.{{FindMethodName}}({{keys.Select(m => m.MemberName).Join(", ")}});
                    if (instance == null) {
                        return NotFound();
                    } else {
                        return this.JsonContent(instance);
                    }
                }
                """;
        }

        internal string RenderAppSrvMethod() {
            var appSrv = new ApplicationService();
            var args = GetEFCoreMethodArgs().ToArray();

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のキー情報から対象データの詳細を検索して返します。
                /// </summary>
                public virtual {{FindMethodReturnType}}? {{FindMethodName}}({{args.Select(m => $"{m.CSharpTypeName}? {m.MemberName}").Join(", ")}}) {

                    {{WithIndent(RenderDbEntityLoading(appSrv.DbContext, "entity", args.Select(a => a.MemberName).ToArray(), tracks: false, includeRefs: true), "    ")}}

                    if (entity == null) return null;

                    var aggregateInstance = {{_aggregate.Item.ClassName}}.{{AggregateDetail.FROM_DBENTITY}}(entity);
                    return aggregateInstance;
                }
                """;
        }

        private IEnumerable<AggregateMember.AggregateMemberBase> GetEFCoreMethodArgs() {
            return _aggregate
                .GetKeys()
                .Where(m => m is AggregateMember.ValueMember);
        }

        internal string RenderDbEntityLoading(string dbContextVarName, string entityVarName, IList<string> searchKeys, bool tracks, bool includeRefs) {

            // Include
            var includeEntities = _aggregate
                .EnumerateThisAndDescendants()
                .ToList();
            if (includeRefs) {
                var refEntities = _aggregate
                    .EnumerateThisAndDescendants()
                    .SelectMany(agg => agg.GetMembers())
                    .Select(m => m.DeclaringAggregate);
                foreach (var entity in refEntities) {
                    includeEntities.Add(entity);
                }
            }
            var paths = includeEntities
                .Select(entity => entity.PathFromEntry())
                .Distinct()
                .SelectMany(edge => edge)
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
                .Where(m => m is AggregateMember.ValueMember)
                .SelectTextTemplate((m, i) => $"x.{m.GetFullPath().Join(".")} == {searchKeys.ElementAtOrDefault(i)}");

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
    }
}
