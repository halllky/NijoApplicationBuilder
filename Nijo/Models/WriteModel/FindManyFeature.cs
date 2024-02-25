using Nijo.Core;
using Nijo.Util.DotnetEx;
using Nijo.Util.CodeGenerating;
using Nijo.Parts.Utility;
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
        internal const string PARAM_FILTER = "filter";
        internal const string PARAM_SKIP = "skip";
        internal const string PARAM_TAKE = "take";

        internal string GetUrlStringForReact() {
            var controller = new Parts.WebClient.Controller(_aggregate.Item);
            return $"/{controller.SubDomain}/{ACTION_NAME}";
        }

        internal string RenderController() {
            var keys = _aggregate
                .GetKeys()
                .Where(m => m is AggregateMember.ValueMember)
                .ToArray();
            var controller = new Parts.WebClient.Controller(_aggregate.Item);

            return $$"""
                [HttpPost("{{ACTION_NAME}}")]
                public virtual IActionResult Load([FromBody]{{GetConditionClassName(_aggregate)}}? {{PARAM_FILTER}}, [FromQuery] int? {{PARAM_SKIP}}, [FromQuery] int? {{PARAM_TAKE}}) {
                    var instances = _applicationService.{{FindMethodName}}({{PARAM_FILTER}}, {{PARAM_SKIP}}, {{PARAM_TAKE}});
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
                public virtual {{FindMethodReturnType}} {{FindMethodName}}({{GetConditionClassName(_aggregate)}}? {{PARAM_FILTER}}, int? {{PARAM_SKIP}}, int? {{PARAM_TAKE}}) {

                    var query = (IQueryable<{{_aggregate.Item.EFCoreEntityClassName}}>){{appSrv.DbContext}}.{{_aggregate.Item.DbSetName}}
                        .AsNoTracking()
                {{paths.SelectTextTemplate(path => path.source == _aggregate ? $$"""
                        .Include(x => x.{{path.prop}})
                """ : $$"""
                        .ThenInclude(x => x.{{path.prop}})
                """)}}
                        ;

                    // 絞り込み
                    {{WithIndent(EnumerateSearchConditionMembers().SelectTextTemplate(RenderFilterSentence), "    ")}}

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


        #region 検索条件
        private static string GetConditionClassName(GraphNode<Aggregate> agg) {
            return $"{agg.Item.ClassName}SearchCondition";
        }
        private static string GetCSharpType(AggregateMember.ValueMember vm) {
            return vm.Options.MemberType.SearchBehavior == SearchBehavior.Range
                ? $"{FromTo.CLASSNAME}<{vm.CSharpTypeName}?>"
                : vm.CSharpTypeName;
        }
        private static string GetTypeScriptType(AggregateMember.ValueMember vm) {
            return vm.Options.MemberType.SearchBehavior == SearchBehavior.Range
                ? $"{{ {FromTo.FROM}?: {vm.TypeScriptTypename}, {FromTo.TO}?: {vm.TypeScriptTypename} }}"
                : vm.TypeScriptTypename;
        }

        internal string TypeScriptConditionClass => GetConditionClassName(_aggregate);
        internal string CsharpConditionClass => GetConditionClassName(_aggregate);

        internal IEnumerable<AggregateMember.ValueMember> EnumerateSearchConditionMembers() {
            static IEnumerable<AggregateMember.ValueMember> EnumerateRecursively(GraphNode<Aggregate> agg) {
                var thisAndChild = agg
                    .EnumerateThisAndDescendants()
                    .Where(agg => agg.EnumerateAncestors().All(anc => anc.Initial.IsChildMember()));

                foreach (var member in thisAndChild.SelectMany(agg => agg.GetMembers())) {
                    if (member is AggregateMember.ValueMember vm) {
                        if (vm.DeclaringAggregate != vm.Owner) continue;
                        if (vm.Options.InvisibleInGui) continue;
                        yield return vm;

                    } else if (member is AggregateMember.Ref @ref) {
                        foreach (var refMember in EnumerateRecursively(@ref.MemberAggregate)) {
                            yield return refMember;
                        }
                    }
                }
            }
            foreach (var vm in EnumerateRecursively(_aggregate)) {
                yield return vm;
            }
        }

        private static string RenderFilterSentence(AggregateMember.ValueMember vm) {
            var paramValueOrNull = $"{PARAM_FILTER}?.{vm.Declared.GetFullPath().Join("?.")}";
            var paramValue = $"{PARAM_FILTER}.{vm.Declared.GetFullPath().Join(".")}";
            var memberPath = vm.Declared.GetFullPath().Join(".");

            if (vm.Options.MemberType.SearchBehavior == SearchBehavior.Ambiguous) {
                return $$"""
                    if (!string.IsNullOrWhiteSpace({{paramValueOrNull}})) {
                        var trimmed = {{paramValue}}.Trim();
                        query = query.Where(x => x.{{memberPath}}.Contains(trimmed));
                    }
                    """;
            }

            if (vm.Options.MemberType.SearchBehavior == SearchBehavior.Range) {
                return $$"""
                    if ({{paramValueOrNull}}?.{{FromTo.FROM}} != default) {
                        query = query.Where(x => x.{{memberPath}} >= {{paramValue}}.{{FromTo.FROM}});
                    }
                    if ({{paramValueOrNull}}?.{{FromTo.TO}} != default) {
                        query = query.Where(x => x.{{memberPath}} <= {{paramValue}}.{{FromTo.TO}});
                    }
                    """;
            }

            var csType = vm.Options.MemberType.GetCSharpTypeName();
            return csType == "string" || csType == "string?"
                ? $$"""
                    if (!string.IsNullOrWhiteSpace({{paramValueOrNull}})) {
                        query = query.Where(x => x.{{memberPath}} == {{paramValue}});
                    }
                    """
                : $$"""
                    if ({{paramValueOrNull}} != default) {
                        query = query.Where(x => x.{{memberPath}} == {{paramValue}});
                    }
                    """;
        }

        internal string RenderSearchConditionCSharpDeclaring() {
            var aggregates = _aggregate
                .EnumerateThisAndDescendants()
                .Where(agg => agg.EnumerateAncestors().All(anc => anc.Initial.IsChildMember()));

            return aggregates.SelectTextTemplate(agg => {
                var members = agg
                    .GetMembers()
                    .Where(m => m.DeclaringAggregate == agg
                             && m is not AggregateMember.Children
                             && m is not AggregateMember.VariationItem);
                return $$"""
                    public class {{GetConditionClassName(agg)}} {
                    {{members.SelectTextTemplate(m =>
                    If(m is AggregateMember.ValueMember, () => $$"""
                        public {{GetCSharpType((AggregateMember.ValueMember)m)}}? {{m.MemberName}} { get; set; }
                    """).ElseIf(m is AggregateMember.RelationMember, () => $$"""
                        public {{GetConditionClassName(((AggregateMember.RelationMember)m).MemberAggregate)}}? {{m.MemberName}} { get; set; } = new();
                    """))}}
                    }
                    """;
            });
        }

        internal string RenderSearchConditionTypeScriptDeclaring() {
            var aggregates = _aggregate
                .EnumerateThisAndDescendants()
                .Where(agg => agg.EnumerateAncestors().All(anc => anc.Initial.IsChildMember()));

            return aggregates.SelectTextTemplate(agg => {
                var members = agg
                    .GetMembers()
                    .Where(m => m.DeclaringAggregate == agg
                             && m is not AggregateMember.Children
                             && m is not AggregateMember.VariationItem);
                return $$"""
                    export type {{GetConditionClassName(agg)}} = {
                    {{members.SelectTextTemplate(m =>
                    If(m is AggregateMember.ValueMember, () => $$"""
                      {{m.MemberName}}?: {{GetTypeScriptType((AggregateMember.ValueMember)m)}}
                    """).ElseIf(m is AggregateMember.RelationMember, () => $$"""
                      {{m.MemberName}}?: {{GetConditionClassName(((AggregateMember.RelationMember)m).MemberAggregate)}}
                    """))}}
                    }
                    """;
            });
        }
        #endregion 検索条件
    }
}
