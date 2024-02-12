using Nijo.Core;
using Nijo.Util.DotnetEx;
using Nijo.Util.CodeGenerating;
using Nijo.Parts.WebServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel {

    /// <summary>
    /// グリッドでのデータ一括編集のため、集約を検索してその集約全体を返す。
    /// <see cref="FindFeature"/> をコピーして作成。
    /// コピー元の機能との違いは、キーを受け取って1件返すか、検索条件を受け取って複数件返すかだけ。
    /// TODO: ソート
    /// TODO: 検索条件
    /// </summary>
    internal class FindManyFeature {
        internal FindManyFeature(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        // ApplicationServiceのメソッドのシグネチャ
        private string FindMethodReturnType => $"IEnumerable<{_aggregate.Item.ClassName}>";
        private string FindMethodName => $"Load{_aggregate.Item.DisplayName.ToCSharpSafe()}";

        private const string ACTION_NAME = "load";
        private const string PARAM_SKIP = "skip";
        private const string PARAM_TAKE = "take";

        internal string GetUrlStringForReact() {
            var controller = new Parts.WebClient.Controller(_aggregate.Item);
            return $"`/{controller.SubDomain}/{ACTION_NAME}`";
        }

        internal string RenderController() {
            var keys = _aggregate
                .GetKeys()
                .Where(m => m is AggregateMember.ValueMember)
                .ToArray();
            var controller = new Parts.WebClient.Controller(_aggregate.Item);

            return $$"""
                [HttpGet("{{ACTION_NAME}}")]
                public virtual IActionResult Load([FromQuery] int? {{PARAM_SKIP}}, [FromQuery] int? {{PARAM_TAKE}}) {
                    var instances = _applicationService.{{FindMethodName}}({{PARAM_SKIP}}, {{PARAM_TAKE}});
                    return this.JsonContent(instances.ToArray());
                }
                """;
        }

        internal string RenderAppSrvMethod() {
            var appSrv = new ApplicationService();

            // Include
            var includeEntities = _aggregate
                .EnumerateThisAndDescendants()
                .ToList();
            var refEntities = _aggregate
                .EnumerateThisAndDescendants()
                .SelectMany(agg => agg.GetMembers())
                .Select(m => m.DeclaringAggregate);
            foreach (var entity in refEntities) {
                includeEntities.Add(entity);
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

            // OrderBy
            var orderColumns = _aggregate
              .GetKeys()
              .OfType<AggregateMember.ValueMember>();

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}を検索して返します。
                /// </summary>
                public virtual {{FindMethodReturnType}} {{FindMethodName}}(int? {{PARAM_SKIP}}, int? {{PARAM_TAKE}}) {

                    var query = (IQueryable<{{_aggregate.Item.EFCoreEntityClassName}}>){{appSrv.DbContext}}.{{_aggregate.Item.DbSetName}}
                        .AsNoTracking()
                {{paths.SelectTextTemplate(path => path.source == _aggregate ? $$"""
                        .Include(x => x.{{path.prop}})
                """ : $$"""
                        .ThenInclude(x => x.{{path.prop}})
                """)}}
                        ;

                    // 順番
                    query = query
                        .OrderBy(x => x.{{orderColumns.First().Declared.GetFullPath().Join(".")}})
                {{orderColumns.Skip(1).SelectTextTemplate(vm => $$"""
                        .ThenBy(x => x.{{vm.Declared.GetFullPath().Join(".")}})
                """)}}
                        ;

                    // ページング
                    if ({{PARAM_SKIP}} != null) query = query.Skip({{PARAM_SKIP}}.Value);

                    const int DEFAULT_PAGE_SIZE = 20;
                    var pageSize = {{PARAM_TAKE}} ?? DEFAULT_PAGE_SIZE;
                    query = query.Take(pageSize);

                    return query
                        .AsEnumerable()
                        .Select(entity => {{_aggregate.Item.ClassName}}.{{AggregateDetail.FROM_DBENTITY}}(entity));
                }
                """;
        }
    }
}
