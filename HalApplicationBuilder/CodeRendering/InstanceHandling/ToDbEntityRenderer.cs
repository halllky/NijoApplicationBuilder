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

namespace HalApplicationBuilder.CodeRendering.InstanceHandling {
    internal class ToDbEntityRenderer {
        internal ToDbEntityRenderer(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _aggregate = aggregate;
            _ctx = ctx;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly CodeRenderingContext _ctx;

        internal const string METHODNAME = "ToDbEntity";

        internal string Render() {
            var context = new BodyRenderingContext(_aggregate, "this");

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のオブジェクトをデータベースに保存する形に変換します。
                /// </summary>
                public {{_ctx.Config.EntityNamespace}}.{{_aggregate.Item.EFCoreEntityClassName}} {{METHODNAME}}() {
                    return new {{_ctx.Config.EntityNamespace}}.{{_aggregate.Item.EFCoreEntityClassName}} {
                        {{WithIndent(RenderBody(context), "        ")}}
                    };
                }
                """;
        }

        private IEnumerable<string> RenderBody(BodyRenderingContext context) {
            foreach (var prop in context.RenderingAggregate.GetMembers()) {
                if (prop is AggregateMember.ParentPK parentPK) {
                    var instanceName = context.FindOwnerInstanceName(parentPK);
                    var declaringMember = parentPK.GetDeclaringMember();
                    var instancePath = declaringMember.GetFullPath(declaringMember.Owner).Join(".");
                    yield return $$"""
                        {{parentPK.GetDbColumn().PropertyName}} = {{instanceName}}.{{instancePath}},
                        """;

                } else if (prop is AggregateMember.RefTargetMember) {
                    continue; // Refの分岐でレンダリングするので

                } else if (prop is AggregateMember.ValueMember valueMember) {
                    yield return $$"""
                        {{valueMember.GetDbColumn().PropertyName}} = {{context.ValueSourceInstance}}.{{valueMember.GetFullPath(context.ValueSource).Join(".")}},
                        """;

                } else if (prop is AggregateMember.Ref refProp) {
                    var refTargetKeys = refProp.GetForeignKeys().ToArray();
                    var refPropFullpath = $"{context.ValueSourceInstance}.{refProp.GetFullPath(context.ValueSource).Join(".")}.{AggregateInstanceKeyNamePair.KEY}";
                    for (int i = 0; i < refTargetKeys.Length; i++) {
                        var foreignKey = refTargetKeys[i];
                        var propertyName = foreignKey.GetDbColumn().PropertyName;
                        var memberType = foreignKey.MemberType.GetCSharpTypeName();

                        yield return $$"""
                            {{propertyName}} = ({{memberType}}){{AggregateInstanceKeyNamePair.RenderKeyJsonRestoring(refPropFullpath)}}[{{i}}]!,
                            """;
                    }

                } else if (prop is AggregateMember.Children children) {
                    var item = context.Depth == 0 ? "item" : $"item{context.Depth}";
                    var nav = children.GetNavigationProperty();
                    var childProp = nav.Principal.GetFullPath(context.ValueSource).Join(".");
                    var childInstance = children.MemberAggregate;
                    var childDbEntityClass = $"{_ctx.Config.EntityNamespace}.{nav.Relevant.Owner.Item.EFCoreEntityClassName}";

                    yield return $$"""
                        {{children.PropertyName}} = {{context.ValueSourceInstance}}.{{childProp}}.Select({{item}} => new {{childDbEntityClass}} {
                            {{WithIndent(RenderBody(context.Nest(childInstance, item)), "    ")}}
                        }).ToList(),
                        """;

                } else if (prop is AggregateMember.RelationMember child) {
                    var nav = child.GetNavigationProperty();
                    var childProp = nav.Principal.PropertyName;
                    var childInstance = child.MemberAggregate;
                    var childDbEntityClass = $"{_ctx.Config.EntityNamespace}.{nav.Relevant.Owner.Item.EFCoreEntityClassName}";

                    yield return $$"""
                        {{child.PropertyName}} = new {{childDbEntityClass}} {
                            {{WithIndent(RenderBody(context.Nest(childInstance)), "    ")}}
                        },
                        """;

                } else {
                    throw new NotImplementedException();
                }
            }
        }

        private class BodyRenderingContext {
            public BodyRenderingContext(GraphNode<Aggregate> aggregate, string instanceName) {
                _stack = new Stack<Item>();
                _stack.Push(new Item { Aggregate = aggregate, InstanceName = instanceName });
                ValueSource = aggregate;
            }
            private BodyRenderingContext(Stack<Item> stack, GraphNode<Aggregate> valueSource) {
                _stack = stack;
                ValueSource = valueSource;
            }
            private readonly Stack<Item> _stack;

            public GraphNode<Aggregate> RenderingAggregate => _stack.Peek().Aggregate;
            public GraphNode<Aggregate> ValueSource { get; }
            public string ValueSourceInstance => _stack.Peek().InstanceName;
            public int Depth => _stack.Count - 1;

            public BodyRenderingContext Nest(GraphNode<Aggregate> childAggregate, string? childInstanceName = null) {
                var newStack = new Stack<Item>(_stack);
                newStack.Push(new Item { Aggregate = childAggregate, InstanceName = childInstanceName ?? ValueSourceInstance });
                var valueSource = childInstanceName == null ? ValueSource : childAggregate;
                return new BodyRenderingContext(newStack, valueSource);
            }
            /// <summary>
            /// 集約クラスはは親の主キーを持っていないため、
            /// EFCoreエンティティの親の主キーは集約クラスのインスタンスの親から持ってくる必要があるので
            /// そのインスタンスの名前を探す
            /// </summary>
            public string FindOwnerInstanceName(AggregateMember.ParentPK parentPK) {
                var declareingAggregate = parentPK.GetDeclaringMember().Owner;
                return _stack
                    .Single(item => item.Aggregate == declareingAggregate)
                    .InstanceName;
            }

            private class Item {
                public required GraphNode<Aggregate> Aggregate { get; init; }
                public required string InstanceName { get; init; }
            }
        }
    }
}
