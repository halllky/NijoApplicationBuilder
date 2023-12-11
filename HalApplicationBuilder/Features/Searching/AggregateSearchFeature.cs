using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using HalApplicationBuilder.Features.InstanceHandling;
using static HalApplicationBuilder.Features.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Features.Searching {
    internal class AggregateSearchFeature {
        public AggregateSearchFeature(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        private string AppServiceSearchMethodName => $"Search{_aggregate.Item.DisplayName.ToCSharpSafe()}";

        private IReadOnlyDictionary<DbColumn, MultiViewField> GetFields() {

            var descendantColumns = _aggregate
                .EnumerateThisAndDescendants()
                .Where(x => x.EnumerateAncestorsAndThis()
                .All(ancestor => !ancestor.IsChildrenMember()
                              && !ancestor.IsVariationMember()))
                .SelectMany(entity => entity.GetColumns());

            // 参照先のキーはdescendantColumnsの中に入っているが、名前は入っていないので、別途取得の必要あり
            var pkRefTargets = _aggregate
                .EnumerateThisAndDescendants()
                .Where(x => x.EnumerateAncestorsAndThis()
                             .All(ancestor => !ancestor.IsChildrenMember()
                                           && !ancestor.IsVariationMember()))
                .SelectMany(entity => entity.GetMembers().OfType<AggregateMember.Ref>());
            var refTargetColumns = pkRefTargets
                .SelectMany(member => member.MemberAggregate.GetMembers())
                .OfType<AggregateMember.ValueMember>()
                .Where(member => !member.IsKey && member.IsDisplayName)
                .Select(member => member.GetDbColumn());

            var fields = descendantColumns
                .Concat(refTargetColumns)
                .ToDictionary(
                    col => col,
                    col => new MultiViewField {
                        MemberType = col.Options.MemberType,
                        VisibleInGui = !col.Options.InvisibleInGui,
                        PhysicalName = col.GetFullPath().Join("_"),
                    });
            return fields;
        }

        internal string RenderDbContextMethod(CodeRenderingContext ctx) {
            return _aggregate.IsStored()
                ? RenderSearchMethod(ctx)
                : RenderNotImplementedSearchMethod(ctx);
        }
        /// <summary>
        /// Viewなどオーバーライド前提のものの検索処理
        /// </summary>
        private string RenderNotImplementedSearchMethod(CodeRenderingContext ctx) {
            var appSrv = new ApplicationService(ctx.Config);
            var multiView = GetMultiView();

            return $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    partial class {{appSrv.ClassName}} {
                        public virtual IEnumerable<{{multiView.SearchResultClassName}}> {{AppServiceSearchMethodName}}({{multiView.SearchConditionClassName}} conditions) {
                            // このメソッドは自動生成の対象外です。
                            // {{appSrv.ConcreteClass}}クラスでこのメソッドをオーバーライドして実装してください。
                            return Enumerable.Empty<{{multiView.SearchResultClassName}}>();
                        }
                    }
                }
                """;
        }
        /// <summary>
        /// 標準の検索処理
        /// </summary>
        private string RenderSearchMethod(CodeRenderingContext ctx) {
            var appSrv = new ApplicationService(ctx.Config);
            var fields = GetFields();
            var selectClause = fields.Select(field => new {
                resultMemberName = field.Value.PhysicalName,
                dbColumnPath = field.Key.GetFullPath().Join("."),
            });
            var multiView = GetMultiView();

            return $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.EntityFrameworkCore.Infrastructure;

                    partial class {{appSrv.ClassName}} {
                        /// <summary>
                        /// {{_aggregate.Item.DisplayName}}の一覧検索を行います。
                        /// </summary>
                        public virtual IEnumerable<{{multiView.SearchResultClassName}}> {{AppServiceSearchMethodName}}({{multiView.SearchConditionClassName}} param) {
                            var query = {{appSrv.DbContext}}.{{_aggregate.Item.DbSetName}}.Select(e => new {{multiView.SearchResultClassName}} {
                {{selectClause.SelectTextTemplate(x => $$"""
                                {{x.resultMemberName}} = e.{{x.dbColumnPath}},
                """)}}
                            });

                {{fields.Values.SelectTextTemplate(field =>
                If(field.MemberType.SearchBehavior == SearchBehavior.Ambiguous, () => $$"""
                            if (!string.IsNullOrWhiteSpace(param.{{field.PhysicalName}})) {
                                var trimmed = param.{{field.PhysicalName}}.Trim();
                                query = query.Where(x => x.{{field.PhysicalName}}.Contains(trimmed));
                            }
                """).ElseIf(field.MemberType.SearchBehavior == SearchBehavior.Range, () => $$"""
                            if (param.{{field.PhysicalName}}.{{Util.FromTo.FROM}} != default) {
                                query = query.Where(x => x.{{field.PhysicalName}} >= param.{{field.PhysicalName}}.{{Util.FromTo.FROM}});
                            }
                            if (param.{{field.PhysicalName}}.{{Util.FromTo.TO}} != default) {
                                query = query.Where(x => x.{{field.PhysicalName}} <= param.{{field.PhysicalName}}.{{Util.FromTo.TO}});
                            }
                """).ElseIf(field.MemberType.SearchBehavior == SearchBehavior.Strict && new[] { "string", "string?" }.Contains(field.MemberType.GetCSharpTypeName()), () => $$"""
                            if (!string.IsNullOrWhiteSpace(param.{{field.PhysicalName}})) {
                                query = query.Where(x => x.{{field.PhysicalName}} == param.{{field.PhysicalName}});
                            }
                """).ElseIf(field.MemberType.SearchBehavior == SearchBehavior.Strict, () => $$"""
                            if (param.{{field.PhysicalName}} != default) {
                                query = query.Where(x => x.{{field.PhysicalName}} == param.{{field.PhysicalName}});
                            }
                """))}}
                            if (param.{{MultiView2.SEARCHCONDITION_PAGE_PROP_NAME}} != null) {
                                const int PAGE_SIZE = 20;
                                var skip = param.{{MultiView2.SEARCHCONDITION_PAGE_PROP_NAME}}.Value * PAGE_SIZE;
                                query = query.Skip(skip).Take(PAGE_SIZE);
                            }

                            return query.AsEnumerable();
                        }
                    }
                }
                """;
        }

        internal MultiView2 GetMultiView() {
            var fields = GetFields();

            var createView = new SingleView(_aggregate, SingleView.E_Type.Create);
            var singleView = new SingleView(_aggregate, SingleView.E_Type.View);

            var keys = _aggregate
                .GetKeys()
                .OfType<AggregateMember.ValueMember>()
                .Select(m => m.GetDbColumn().GetFullPath().Join("_"));

            return new MultiView2 {
                DisplayName = _aggregate.Item.DisplayName,
                Fields = fields.Values.ToArray(),
                AppSrvMethodName = AppServiceSearchMethodName,
                CreateViewUrl = _aggregate.IsCreatable()
                    ? createView.GetUrlStringForReact()
                    : null,
                SingleViewUrlFunctionBody = _aggregate.IsStored()
                    ? (data => singleView.GetUrlStringForReact(keys.Select(field => $"{data}.{field}")))
                    : null,
            };
        }
    }
}
