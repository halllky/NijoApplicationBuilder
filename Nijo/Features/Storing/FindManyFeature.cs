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

namespace Nijo.Features.Storing {

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
        private string FindMethodReturnType => $"IEnumerable<{new DataClassForDisplay(_aggregate).CsClassName}>";
        private string FindMethodName => $"Load{_aggregate.Item.PhysicalName}";

        private const string ACTION_NAME = "load";
        internal const string PARAM_FILTER = "filter";
        internal const string PARAM_SKIP = "skip";
        internal const string PARAM_TAKE = "take";

        internal string GetUrlStringForReact() {
            var controller = new Controller(_aggregate.Item);
            return $"/{controller.SubDomain}/{ACTION_NAME}";
        }

        internal string RenderController() {
            return $$"""
                /// <summary>
                /// 既存の{{_aggregate.Item.DisplayName}}を一覧検索する Web API
                /// </summary>
                [HttpPost("{{ACTION_NAME}}")]
                public virtual IActionResult Load([FromBody]{{GetConditionClassName(_aggregate)}}? {{PARAM_FILTER}}, [FromQuery] int? {{PARAM_SKIP}}, [FromQuery] int? {{PARAM_TAKE}}) {
                    var instances = _applicationService.{{FindMethodName}}({{PARAM_FILTER}}, {{PARAM_SKIP}}, {{PARAM_TAKE}});
                    return this.JsonContent(instances.ToArray());
                }
                """;
        }

        internal string RenderAppSrvMethod(CodeRenderingContext context) {
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
                {{If(context.Config.DiscardSearchLimit, () => $$"""
                    if ({{PARAM_TAKE}} != null) query = query.Take({{PARAM_TAKE}}.Value);

                """).Else(() => $$"""

                    const int DEFAULT_PAGE_SIZE = 20;
                    var pageSize = {{PARAM_TAKE}} ?? DEFAULT_PAGE_SIZE;
                    query = query.Take(pageSize);

                """)}}
                    return query
                        .AsEnumerable()
                        .Select(entity => {{new DataClassForDisplay(_aggregate).CsClassName}}.{{DataClassForDisplay.FROM_DBENTITY}}(entity));
                }
                """;
        }


        #region 絞り込み
        internal string TypeScriptConditionClass => GetConditionClassName(_aggregate);
        internal string TypeScriptConditionInitializerFn => GetConditionClassInitializerName(_aggregate);
        internal string CsharpConditionClass => GetConditionClassName(_aggregate);

        private string GetConditionClassName(GraphNode<Aggregate> agg) {
            if (agg.IsInTreeOf(_aggregate)) {
                return $"{agg.Item.PhysicalName}SearchCondition";
            } else {
                var paths = new List<string>();
                foreach (var edge in agg.PathFromEntry()) {
                    var terminal = edge.Terminal.As<Aggregate>();
                    var initial = edge.Initial.As<Aggregate>();
                    if (terminal.IsInTreeOf(_aggregate)) continue;
                    if (initial.IsInTreeOf(_aggregate)) paths.Add(initial.Item.PhysicalName);
                    paths.Add(edge.IsParentChild() ? "Parent" : edge.RelationName);
                }
                return $"{paths.Join("_")}SearchCondition";
            }
        }
        private string GetConditionClassInitializerName(GraphNode<Aggregate> agg) {
            return $"create{GetConditionClassName(agg)}";
        }

        /// <summary>
        /// Variationはどの種別のデータを結果に含めるかをtrue/falseで選択する形のため種別ごとのプロパティ名を列挙する
        /// </summary>
        internal static IEnumerable<KeyValuePair<AggregateMember.VariationItem, string>> VariationMemberProps(AggregateMember.Variation variation) {
            foreach (var item in variation.GetGroupItems()) {
                yield return KeyValuePair.Create(item, $"{variation.MemberName}_{item.MemberName}");
            }
        }

        /// <summary>
        /// 検索条件のメンバーを列挙
        /// </summary>
        internal IEnumerable<AggregateMember.ValueMember> EnumerateSearchConditionMembers() {
            IEnumerable<AggregateMember.ValueMember> EnumerateRecursively(GraphNode<Aggregate> agg) {
                var thisAndChild = agg
                    .EnumerateThisAndDescendants()
                    // ChildrenやVariationの項目を検索条件にするとSQLを組み立てるのが大変なので除外
                    .Where(a => a.EnumerateAncestors().All(anc => anc.Initial.IsChildMember())
                             || a == agg); // ChildrenであってもRefされた参照先の場合は除外しない

                foreach (var member in thisAndChild.SelectMany(agg => agg.GetMembers())) {
                    if (member is AggregateMember.ValueMember vm) {
                        // 後述の else if で列挙されるメンバーと重複してしまうので除外する
                        if (vm.DeclaringAggregate != vm.Owner) continue;

                        yield return vm;

                    } else if (member is AggregateMember.Ref @ref) {
                        // 1:1で紐づく参照先の項目でも検索できるようにしたい
                        foreach (var refVm in EnumerateRecursively(@ref.RefTo)) {
                            yield return refVm;
                        }

                    } else if (member is AggregateMember.Parent parent
                             && !member.Owner.IsInTreeOf(_aggregate)) {
                        // 参照先が子孫集約の場合、親の属性でも検索できるようにしたい
                        foreach (var parentVm in EnumerateRecursively(parent.ParentAggregate)) {
                            yield return parentVm;
                        }
                    }
                }
            }
            foreach (var vm in EnumerateRecursively(_aggregate)) {
                yield return vm;
            }
        }
        /// <summary>
        /// 自身のメンバーを列挙
        /// </summary>
        private IEnumerable<AggregateMember.AggregateMemberBase> EnumerateSearchConditionOwnMembers(GraphNode<Aggregate> aggregate) {
            return aggregate.GetMembers().Where(m => {
                if (m is AggregateMember.ValueMember
                    && m.DeclaringAggregate == aggregate)
                    return true;

                if (m is AggregateMember.Ref)
                    return true;

                if (m is AggregateMember.Child child
                    && child.ChildAggregate.IsInTreeOf(_aggregate))
                    return true;

                if (m is AggregateMember.VariationItem vi
                    && vi.VariationAggregate.IsInTreeOf(_aggregate))
                    return true;

                if (m is AggregateMember.Parent parent
                    && !parent.ParentAggregate.IsInTreeOf(_aggregate))
                    return true;

                return false;
            });
        }

        private static string RenderFilterSentence(AggregateMember.ValueMember vm) {
            var paramValueOrNull = $"{PARAM_FILTER}?.{vm.Declared.GetFullPath().Join("?.")}";
            var paramValue = $"{PARAM_FILTER}.{vm.Declared.GetFullPath().Join(".")}";
            var memberPath = vm.Declared.GetFullPath().Join(".");

            if (vm is AggregateMember.Variation variation) {
                var checkedArray = $"checked{variation.GetFullPath().Join("_")}";
                var paramValues = VariationMemberProps(variation)
                    .Select(x => new {
                        key = x.Key.Relation.RelationName,
                        fullPath = $"{PARAM_FILTER}?.{vm.Declared.GetFullPath().SkipLast(1).Concat(new[] { x.Value }).Join("?.")}",
                    });

                return $$"""
                    var {{checkedArray}} = new[] {
                    {{paramValues.SelectTextTemplate(x => $$"""
                        {{x.fullPath}},
                    """)}}
                    };
                    if (!{{checkedArray}}.All(check => check == true)
                     && !{{checkedArray}}.All(check => check == false || check == null)) {

                        var keyList = new List<{{variation.VariationGroup.CsEnumType}}>();
                    {{paramValues.SelectTextTemplate(x => $$"""
                        if ({{x.fullPath}} == true) keyList.Add({{variation.VariationGroup.CsEnumType}}.{{x.key}});
                    """)}}

                        query = query.Where(x => x.{{memberPath}} != null && keyList.Contains(x.{{memberPath}}.Value));
                    }
                    """;
            }

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

        private static string GetCSharpType(AggregateMember.ValueMember vm) {
            if (vm is AggregateMember.Variation) {
                throw new Exception("Renderメソッドの中で直で指定するのでこの分岐にはこない");

            } else if (vm.Options.MemberType.SearchBehavior == SearchBehavior.Range) {
                return $"{FromTo.CLASSNAME}<{vm.CSharpTypeName}?>";

            } else {
                return vm.CSharpTypeName;
            }
        }
        internal string RenderSearchConditionTypeDeclaring(bool csharp) {
            return EnumerateSearchConditionAggregates().SelectTextTemplate(agg => {
                if (csharp) {
                    // C#
                    string RenderMember(AggregateMember.AggregateMemberBase member) {
                        if (member is AggregateMember.Variation variation) {
                            return VariationMemberProps(variation).SelectTextTemplate(x => $$"""
                                public bool? {{x.Value}} { get; set; }
                                """);

                        } else if (member is AggregateMember.ValueMember vm) {
                            return vm.Options.MemberType.SearchBehavior == SearchBehavior.Range
                                ? $"public {FromTo.CLASSNAME}<{vm.CSharpTypeName}?> {vm.MemberName} {{ get; set; }} = new();"
                                : $"public {vm.CSharpTypeName}? {vm.MemberName} {{ get; set; }}";

                        } else if (member is AggregateMember.RelationMember rel) {
                            return $"public {GetConditionClassName(rel.MemberAggregate)} {rel.MemberName} {{ get; set; }} = new();";

                        } else {
                            throw new InvalidOperationException();
                        }
                    }

                    return $$"""
                        /// <summary>
                        /// {{agg.Item.DisplayName}}の一覧検索条件
                        /// </summary>
                        public class {{GetConditionClassName(agg)}} {
                        {{EnumerateSearchConditionOwnMembers(agg).SelectTextTemplate(m => $$"""
                            {{WithIndent(RenderMember(m), "    ")}}
                        """)}}
                        }
                        """;
                } else {
                    // TypeScript
                    string RenderMember(AggregateMember.AggregateMemberBase member) {
                        if (member is AggregateMember.Variation variation) {
                            return VariationMemberProps(variation).SelectTextTemplate(x => $$"""
                                {{x.Value}}?: boolean
                                """);

                        } else if (member is AggregateMember.ValueMember vm) {
                            return vm.Options.MemberType.SearchBehavior == SearchBehavior.Range
                                ? $"{vm.MemberName}: {{ {FromTo.FROM}?: {vm.TypeScriptTypename}, {FromTo.TO}?: {vm.TypeScriptTypename} }}"
                                : $"{vm.MemberName}?: {vm.TypeScriptTypename}";

                        } else if (member is AggregateMember.RelationMember rel) {
                            return $"{rel.MemberName}: {GetConditionClassName(rel.MemberAggregate)}";
                             
                        } else {
                            throw new InvalidOperationException();
                        }
                    }

                    return $$"""
                        /** {{agg.Item.DisplayName}}の一覧検索条件 */
                        export type {{GetConditionClassName(agg)}} = {
                        {{EnumerateSearchConditionOwnMembers(agg).SelectTextTemplate(m => $$"""
                          {{WithIndent(RenderMember(m), "  ")}}
                        """)}}
                        }
                        """;
                }
            });
        }

        internal string RenderTypeScriptConditionInitializerFn() {
            return EnumerateSearchConditionAggregates().SelectTextTemplate(agg => $$"""
                export const {{GetConditionClassInitializerName(agg)}} = (): {{GetConditionClassName(agg)}} => ({
                {{EnumerateSearchConditionOwnMembers(agg).SelectTextTemplate(m => $$"""
                {{If(m is AggregateMember.ValueMember vm && vm.Options.MemberType.SearchBehavior == SearchBehavior.Range, () => $$"""
                    {{m.MemberName}}: {},
                """).ElseIf(m is AggregateMember.RelationMember, () => $$"""
                    {{m.MemberName}}: create{{GetConditionClassName(((AggregateMember.RelationMember)m).MemberAggregate)}}(),
                """)}}
                """)}}
                })
                """);
        }

        private IEnumerable<GraphNode<Aggregate>> EnumerateSearchConditionAggregates() {
            var aggregates = _aggregate
                .EnumerateThisAndDescendants()
                .ToList();
            var foreignAggregates = aggregates
                .SelectMany(agg => agg.GetRefsAndTheirAncestorsRecursively())
                .ToArray();
            aggregates.AddRange(foreignAggregates);

            return aggregates;
        }
        #endregion 絞り込み
    }
}
