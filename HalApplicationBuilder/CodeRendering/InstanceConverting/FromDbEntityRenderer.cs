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
            _aggregateInstance = aggregate.GetInstanceClass().AsEntry();
            _dbEntity = aggregate.GetDbEntity().AsEntry();
            _ctx = ctx;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<AggregateInstance> _aggregateInstance;
        private readonly GraphNode<IEFCoreEntity> _dbEntity;
        private readonly CodeRenderingContext _ctx;

        private const string METHODNAME = AggregateInstance.FROM_DB_ENTITY_METHOD_NAME;

        internal string Render() {
            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のデータベースから取得した内容を画面に表示する形に変換します。
                /// </summary>
                public static {{_aggregateInstance.Item.ClassName}} {{METHODNAME}}({{_ctx.Config.EntityNamespace}}.{{_dbEntity.Item.ClassName}} entity) {
                    var instance = new {{_aggregateInstance.Item.ClassName}} {
                        {{WithIndent(RenderBody(_aggregateInstance, "", "entity", 0), "        ")}}
                    };
                    instance.{{AggregateInstanceBase.INSTANCE_KEY}} = instance.{{AggregateRenderer.GETINSTANCEKEY_METHOD_NAME}}().ToString();
                    instance.{{AggregateInstanceBase.INSTANCE_NAME}} = instance.{{AggregateRenderer.GETINSTANCENAME_METHOD_NAME}}();
                    return instance;
                }
                """;
        }

        private IEnumerable<string> RenderBody(GraphNode<AggregateInstance> instance, string parentPath, string instancePath, int depth) {
            foreach (var prop in instance.GetProperties(_ctx.Config)) {
                if (prop is AggregateInstance.SchalarProperty schalarProp) {
                    yield return $$"""
                        {{schalarProp.PropertyName}} = {{instancePath}}.{{schalarProp.CorrespondingDbColumn.PropertyName}},
                        """;

                } else if (prop is AggregateInstance.VariationSwitchProperty switchProp) {
                    yield return $$"""
                        {{switchProp.PropertyName}} = {{instancePath}}.{{switchProp.CorrespondingDbColumn.PropertyName}},
                        """;

                } else if (prop is AggregateInstance.RefProperty refProp) {
                    var refTarget = refProp.Owner.GetDbEntity() == refProp.CorrespondingNavigationProperty.Principal.Owner
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

                } else if (prop is AggregateInstance.ChildrenProperty children) {
                    var item = depth == 0 ? "item" : $"item{depth}";
                    var childClass = children.ChildAggregateInstance.Item.ClassName;
                    var childInstance = children.ChildAggregateInstance.AsEntry();

                    var body = RenderBody(childInstance, instancePath, item, depth + 1);

                    yield return $$"""
                        {{children.PropertyName}} = {{instancePath}}.{{children.PropertyName}}.Select({{item}} => new {{childClass}} {
                            {{WithIndent(RenderBody(childInstance, instancePath, item, depth + 1), "    ")}}
                        }).ToList(),
                        """;

                } else if (prop is AggregateInstance.ChildProperty child) {
                    var childClass = child.ChildAggregateInstance.Item.ClassName;
                    var childInstance = child.ChildAggregateInstance;
                    var nestedInstancePath = $"{instancePath}.{child.CorrespondingNavigationProperty.Principal.PropertyName}";

                    yield return $$"""
                        {{child.PropertyName}} = new {{childClass}} {
                            {{WithIndent(RenderBody(childInstance, instancePath, nestedInstancePath, depth + 1), "    ")}}
                        },
                        """;

                } else if (prop is AggregateInstance.VariationProperty variation) {
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
