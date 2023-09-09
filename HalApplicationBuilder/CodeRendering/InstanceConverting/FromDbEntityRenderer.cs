using HalApplicationBuilder.CodeRendering.Presentation;
using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;

namespace HalApplicationBuilder.CodeRendering.InstanceConverting {
    internal class FromDbEntityRenderer {
        internal FromDbEntityRenderer(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _aggregate = aggregate;
            _ctx = ctx;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly CodeRenderingContext _ctx;

        private const string METHODNAME = AggregateMember.FROM_DB_ENTITY_METHOD_NAME;

        internal string Render() {
            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のデータベースから取得した内容を画面に表示する形に変換します。
                /// </summary>
                public static {{_aggregate.Item.ClassName}} {{METHODNAME}}({{_ctx.Config.EntityNamespace}}.{{_aggregate.Item.EFCoreEntityClassName}} entity) {
                    var instance = new {{_aggregate.Item.ClassName}} {
                        {{WithIndent(RenderBody(_aggregate, "", "entity", 0), "        ")}}
                    };
                    instance.{{AggregateInstanceBase.INSTANCE_KEY}} = instance.{{AggregateRenderer.GETINSTANCEKEY_METHOD_NAME}}().ToString();
                    instance.{{AggregateInstanceBase.INSTANCE_NAME}} = instance.{{AggregateRenderer.GETINSTANCENAME_METHOD_NAME}}();
                    return instance;
                }
                """;
        }

        private IEnumerable<string> RenderBody(GraphNode<Aggregate> instance, string parentPath, string instancePath, int depth) {
            foreach (var prop in instance.GetProperties()) {
                if (prop is AggregateMember.SchalarProperty schalarProp) {
                    yield return $$"""
                        {{schalarProp.PropertyName}} = {{instancePath}}.{{schalarProp.CorrespondingDbColumn.PropertyName}},
                        """;

                } else if (prop is AggregateMember.VariationSwitchProperty switchProp) {
                    yield return $$"""
                        {{switchProp.PropertyName}} = {{instancePath}}.{{switchProp.CorrespondingDbColumn.PropertyName}},
                        """;

                } else if (prop is AggregateMember.RefProperty refProp) {
                    var refTarget = (IEFCoreEntity)refProp.Owner.Item == refProp.CorrespondingNavigationProperty.Principal.Owner.Item
                        ? refProp.CorrespondingNavigationProperty.Principal
                        : refProp.CorrespondingNavigationProperty.Relevant;
                    var nameColumn = refTarget.Owner
                        .GetColumns()
                        .Where(col => col.IsInstanceName)
                        .SingleOrDefault()?
                        .PropertyName;
                    var foreignKeys = refTarget.ForeignKeys
                        .Select(fk => $"{instancePath}.{fk.PropertyName}")
                        .Join(", ");
                    var joinedFk = refTarget.ForeignKeys
                        .Select(fk => $"{fk.PropertyName}?.ToString()")
                        .Join(" + ");

                    yield return $$"""
                        {{refProp.PropertyName}} = new() {
                            {{AggregateInstanceKeyNamePair.KEY}} = {{Utility.CLASSNAME}}.{{Utility.TO_JSON}}(new object?[] { {{foreignKeys}} }),
                            {{AggregateInstanceKeyNamePair.NAME}} = {{instancePath}}.{{nameColumn ?? joinedFk}},
                        },
                        """;

                } else if (prop is AggregateMember.ChildrenProperty children) {
                    var item = depth == 0 ? "item" : $"item{depth}";
                    var childClass = children.ChildAggregateInstance.Item.ClassName;
                    var childInstance = children.ChildAggregateInstance.AsEntry();

                    var body = RenderBody(childInstance, instancePath, item, depth + 1);

                    yield return $$"""
                        {{children.PropertyName}} = {{instancePath}}.{{children.PropertyName}}.Select({{item}} => new {{childClass}} {
                            {{WithIndent(RenderBody(childInstance, instancePath, item, depth + 1), "    ")}}
                        }).ToList(),
                        """;

                } else if (prop is AggregateMember.ChildProperty child) {
                    var childClass = child.ChildAggregateInstance.Item.ClassName;
                    var childInstance = child.ChildAggregateInstance;
                    var nestedInstancePath = $"{instancePath}.{child.CorrespondingNavigationProperty.Principal.PropertyName}";

                    yield return $$"""
                        {{child.PropertyName}} = new {{childClass}} {
                            {{WithIndent(RenderBody(childInstance, instancePath, nestedInstancePath, depth + 1), "    ")}}
                        },
                        """;

                } else if (prop is AggregateMember.VariationProperty variation) {
                    var childClass = variation.ChildAggregateInstance.Item.ClassName;
                    var childInstance = variation.ChildAggregateInstance;
                    var nestedInstancePath = $"{instancePath}.{variation.CorrespondingNavigationProperty.Principal.PropertyName}";

                    yield return $$"""
                        {{variation.PropertyName}} = new {{childClass}} {
                            {{WithIndent(RenderBody(childInstance, instancePath, nestedInstancePath, depth + 1), "    ")}}
                        },
                        """;

                } else {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
