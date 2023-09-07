using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;

namespace HalApplicationBuilder.CodeRendering.InstanceConverting {
    internal class ToDbEntityRenderer {
        internal ToDbEntityRenderer(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _aggregate = aggregate;
            _aggregateInstance = aggregate.GetInstanceClass().AsEntry();
            _dbEntity = aggregate.GetDbEntity().AsEntry();
            _ctx = ctx;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<AggregateInstance> _aggregateInstance;
        private readonly GraphNode<EFCoreEntity> _dbEntity;
        private readonly CodeRenderingContext _ctx;

        private const string METHODNAME = AggregateInstance.TO_DB_ENTITY_METHOD_NAME;

        internal string Render() {
            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のオブジェクトをデータベースに保存する形に変換します。
                /// </summary>
                public {{_ctx.Config.EntityNamespace}}.{{_dbEntity.Item.ClassName}} {{METHODNAME}}() {
                    return new {{_ctx.Config.EntityNamespace}}.{{_dbEntity.Item.ClassName}} {
                        {{WithIndent(RenderBody(_aggregateInstance, "", "this", 0), "        ")}}
                    };
                }
                """;
        }

        private IEnumerable<string> RenderBody(GraphNode<AggregateInstance> instance, string parentPath, string instancePath, int depth) {
            foreach (var prop in instance.GetProperties(_ctx.Config)) {
                if (prop is AggregateInstance.SchalarProperty schalarProp) {
                    var path = schalarProp.CorrespondingDbColumn is EFCoreEntity.ParentTablePrimaryKey
                        ? parentPath
                        : instancePath;

                    yield return $$"""
                        {{schalarProp.CorrespondingDbColumn.PropertyName}} = {{path}}.{{schalarProp.PropertyName}},
                        """;

                } else if (prop is AggregateInstance.VariationSwitchProperty switchProp) {

                    yield return $$"""
                        {{switchProp.CorrespondingDbColumn.PropertyName}} = {{instancePath}}.{{switchProp.PropertyName}},
                        """;

                } else if (prop is AggregateInstance.RefProperty refProp) {
                    for (int i = 0; i < refProp.CorrespondingDbColumns.Length; i++) {
                        var col = refProp.CorrespondingDbColumns[i];

                        yield return $$"""
                            {{col.PropertyName}} = ({{col.MemberType.GetCSharpTypeName()}}){{InstanceKey.CLASS_NAME}}.{{InstanceKey.PARSE}}({{instancePath}}.{{prop.PropertyName}}.{{AggregateInstanceKeyNamePair.KEY}}).{{InstanceKey.OBJECT_ARRAY}}[{{i}}],
                            """;
                    }

                } else if (prop is AggregateInstance.ChildrenProperty children) {
                    var item = depth == 0 ? "item" : $"item{depth}";
                    var childProp = children.CorrespondingNavigationProperty.Principal.PropertyName;
                    var childInstance = children.ChildAggregateInstance.AsEntry();
                    var childDbEntityClass = $"{_ctx.Config.EntityNamespace}.{children.CorrespondingNavigationProperty.Relevant.Owner.Item.ClassName}";

                    yield return $$"""
                        {{children.PropertyName}} = this.{{childProp}}.Select({{item}} => new {{childDbEntityClass}} {
                            {{WithIndent(RenderBody(childInstance, instancePath, item, depth + 1), "    ")}}
                        }).ToList(),
                        """;

                } else if (prop is AggregateInstance.ChildProperty child) {
                    var childProp = child.CorrespondingNavigationProperty.Principal.PropertyName;
                    var childInstance = child.ChildAggregateInstance;
                    var childDbEntityClass = $"{_ctx.Config.EntityNamespace}.{child.CorrespondingNavigationProperty.Relevant.Owner.Item.ClassName}";
                    var nestedInstancePath = $"{instancePath}.{child.ChildAggregateInstance.Source!.RelationName}";

                    yield return $$"""
                        {{child.PropertyName}} = new {{childDbEntityClass}} {
                            {{WithIndent(RenderBody(childInstance, instancePath, nestedInstancePath, depth + 1), "    ")}}
                        },
                        """;

                } else if (prop is AggregateInstance.VariationProperty variation) {
                    var childProp = variation.CorrespondingNavigationProperty.Principal.PropertyName;
                    var childInstance = variation.ChildAggregateInstance;
                    var childDbEntityClass = $"{_ctx.Config.EntityNamespace}.{variation.CorrespondingNavigationProperty.Relevant.Owner.Item.ClassName}";
                    var nestedInstancePath = $"{instancePath}.{variation.ChildAggregateInstance.Source!.RelationName}";

                    yield return $$"""
                        {{variation.PropertyName}} = new {{childDbEntityClass}} {
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
