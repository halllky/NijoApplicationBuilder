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
                if (prop is AggregateMember.KeyOfParent parentPK) {
                    var fullpath = context.GetValueSourceFullPath(parentPK);
                    yield return $$"""
                        {{parentPK.GetDbColumn().Options.MemberName}} = {{fullpath}},
                        """;

                } else if (prop is AggregateMember.KeyOfRefTarget) {
                    continue; // Refの分岐でレンダリングするので

                } else if (prop is AggregateMember.ValueMember valueMember) {
                    yield return $$"""
                        {{valueMember.GetDbColumn().Options.MemberName}} = {{context.ValueSourceInstance}}.{{valueMember.GetFullPath(context.ValueSource).Join(".")}},
                        """;

                } else if (prop is AggregateMember.Ref refProp) {
                    var refTargetKeys = refProp.GetForeignKeys().ToArray();
                    var refPropFullpath = $"{context.ValueSourceInstance}.{refProp.GetFullPath(context.ValueSource).Join(".")}.{AggregateInstanceKeyNamePair.KEY}";
                    for (int i = 0; i < refTargetKeys.Length; i++) {
                        var foreignKey = refTargetKeys[i];
                        var propertyName = foreignKey.GetDbColumn().Options.MemberName;
                        var memberType = foreignKey.Options.MemberType.GetCSharpTypeName();

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
                        {{children.MemberName}} = {{context.ValueSourceInstance}}.{{childProp}}.Select({{item}} => new {{childDbEntityClass}} {
                            {{WithIndent(RenderBody(context.Nest(childInstance, item)), "    ")}}
                        }).ToList(),
                        """;

                } else if (prop is AggregateMember.RelationMember child) {
                    var nav = child.GetNavigationProperty();
                    var childProp = nav.Principal.PropertyName;
                    var childInstance = child.MemberAggregate;
                    var childDbEntityClass = $"{_ctx.Config.EntityNamespace}.{nav.Relevant.Owner.Item.EFCoreEntityClassName}";

                    yield return $$"""
                        {{child.MemberName}} = new {{childDbEntityClass}} {
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
                _stack.Push(new Item {
                    Instance = instanceName,
                    InstanceType = aggregate,
                    MostRecent1To1Ancestor = aggregate,
                });
                ValueSource = aggregate;
            }
            private BodyRenderingContext(Stack<Item> stack, GraphNode<Aggregate> valueSource) {
                _stack = stack;
                ValueSource = valueSource;
            }
            private readonly Stack<Item> _stack;

            public GraphNode<Aggregate> RenderingAggregate => _stack.Peek().InstanceType;
            public GraphNode<Aggregate> ValueSource { get; }
            public string ValueSourceInstance => _stack.Peek().Instance;
            public int Depth => _stack.Count - 1;

            public BodyRenderingContext Nest(GraphNode<Aggregate> childAggregate, string? childInstanceName = null) {
                var newStack = new Stack<Item>(_stack);
                newStack.Push(new Item {
                    Instance = childInstanceName ?? ValueSourceInstance,
                    InstanceType = childAggregate,
                    MostRecent1To1Ancestor = childInstanceName == null
                        ? ValueSource
                        : childAggregate,
                });
                var valueSource = childInstanceName == null ? ValueSource : childAggregate;
                return new BodyRenderingContext(newStack, valueSource);
            }
            /// <summary>
            /// 集約クラスはは親の主キーを持っていないため、EFCoreエンティティの親の主キーは集約クラスのインスタンスの親から持ってくる必要がある。
            /// またラムダ式の中だと単純にthisからのGetFullPathで適切な名前がとれないのでその辺の問題にも対応している
            /// </summary>
            public string GetValueSourceFullPath(AggregateMember.KeyOfParent parentPK) {
                var declaringMember = parentPK.GetDeclaringMember();
                var x = _stack.Single(x => x.InstanceType == declaringMember.Owner);
                var path = declaringMember.GetFullPath(x.MostRecent1To1Ancestor);

                return $"{x.Instance}.{path.Join(".")}";
            }

            private class Item {
                public required string Instance { get; init; }
                public required GraphNode<Aggregate> InstanceType { get; init; }
                /// <summary>
                /// 1対1で辿れるうちの直近の祖先（集約ルートまたはラムダ変数の型）
                /// </summary>
                public required GraphNode<Aggregate> MostRecent1To1Ancestor { get; init; }
            }
        }
    }
}
