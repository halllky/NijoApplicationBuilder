using HalApplicationBuilder.CodeRendering.Presentation;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalApplicationBuilder.CodeRendering.Util;

namespace HalApplicationBuilder.CodeRendering.InstanceHandling {
    internal class AggregateDetail {
        internal AggregateDetail(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        protected readonly GraphNode<Aggregate> _aggregate;

        internal virtual string ClassName => _aggregate.Item.ClassName;

        internal const string FROM_DBENTITY = "FromDbEntity";
        internal const string TO_DBENTITY = "ToDbEntity";

        internal virtual IEnumerable<AggregateMember.AggregateMemberBase> GetMembers() {
            return _aggregate
                .GetMembers()
                .Where(m => m is not AggregateMember.KeyOfParent
                         && m is not AggregateMember.KeyOfRefTarget);
        }

        internal virtual string RenderCSharp(CodeRenderingContext ctx) {
            return $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    
                    /// <summary>
                    /// {{_aggregate.Item.DisplayName}}のデータ1件の詳細を表すクラスです。
                    /// </summary>
                    public partial class {{ClassName}} : {{AggregateInstanceBase.CLASS_NAME}} {
                {{GetMembers().SelectTextTemplate(prop => $$"""
                        public {{prop.CSharpTypeName}} {{prop.MemberName}} { get; set; }
                """)}}

                {{If(_aggregate.IsRoot(), () => $$"""
                        {{WithIndent(ToDbEntity(ctx), "        ")}}
                        {{WithIndent(FromDbEntity(ctx), "        ")}}
                """)}}
                    }
                }
                """;
        }

        internal virtual string RenderTypeScript(CodeRenderingContext ctx) {
            return $$"""
                export type {{_aggregate.Item.TypeScriptTypeName}} = {
                {{GetMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}?: {{m.TypeScriptTypename}}
                """)}}
                {{If(_aggregate.IsRoot(), () => $$"""
                  {{AggregateInstanceBase.INSTANCE_KEY}}?: string
                  {{AggregateInstanceBase.INSTANCE_NAME}}?: string
                """)}}
                  {{AggregateInstanceBase.IS_LOADED}}?: boolean
                }
                """;
        }


        #region FromDbEntity
        internal string FromDbEntity(CodeRenderingContext ctx) {
            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のデータベースから取得した内容を画面に表示する形に変換します。
                /// </summary>
                public static {{_aggregate.Item.ClassName}} {{FROM_DBENTITY}}({{ctx.Config.EntityNamespace}}.{{_aggregate.Item.EFCoreEntityClassName}} entity) {
                    var instance = new {{_aggregate.Item.ClassName}} {
                        {{WithIndent(RenderBodyOfFromDbEntity(_aggregate, _aggregate, "entity", 0), "        ")}}
                    };
                    instance.{{AggregateInstanceBase.INSTANCE_KEY}} = {{WithIndent(AggregateInstanceKeyNamePair.RenderKeyJsonConverting(_aggregate.GetKeyMembers().Select(m => $"instance.{m.GetFullPath().Join(".")}")), "    ")}};
                    instance.{{AggregateInstanceBase.INSTANCE_NAME}} = {{_aggregate.GetInstanceNameMembers().Select(m => $"instance.{m.GetFullPath().Join(".")}?.ToString()").Join(" + ")}};
                    return instance;
                }
                """;
        }
        private IEnumerable<string> RenderBodyOfFromDbEntity(GraphNode<Aggregate> instance, GraphNode<Aggregate> rootInstance, string rootInstanceName, int depth) {
            foreach (var prop in instance.GetMembers()) {
                if (prop is AggregateMember.KeyOfParent) {
                    continue; // 不要

                } else if (prop is AggregateMember.KeyOfRefTarget) {
                    continue; // Refの分岐でレンダリングするので

                } else if (prop is AggregateMember.ValueMember valueMember) {
                    yield return $$"""
                        {{valueMember.MemberName}} = {{rootInstanceName}}.{{valueMember.GetFullPath(rootInstance).Join(".")}},
                        """;

                } else if (prop is AggregateMember.Ref refProp) {
                    var names = refProp.MemberAggregate
                        .GetInstanceNameMembers()
                        .Select(member => member.GetDbColumn().GetFullPath(rootInstance.As<IEFCoreEntity>()).Join("."))
                        .Select(fullpath => $"{rootInstanceName}.{fullpath}?.ToString()");
                    var foreignKeys = refProp
                        .GetForeignKeys()
                        .Select(member => member.GetDbColumn().GetFullPath(rootInstance.As<IEFCoreEntity>()).Join("."))
                        .Select(fullpath => $"{rootInstanceName}.{fullpath}");

                    yield return $$"""
                        {{refProp.MemberName}} = new() {
                            {{AggregateInstanceKeyNamePair.KEY}} = {{Utility.CLASSNAME}}.{{Utility.TO_JSON}}(new object?[] { {{foreignKeys.Join(", ")}} }),
                            {{AggregateInstanceKeyNamePair.NAME}} = {{names.Join(" + ")}},
                        },
                        """;

                } else if (prop is AggregateMember.Children children) {
                    var item = depth == 0 ? "item" : $"item{depth}";
                    var childClass = children.MemberAggregate.Item.ClassName;
                    var childInstance = children.MemberAggregate;
                    var childFullPath = children.GetFullPath(rootInstance).Join(".");

                    yield return $$"""
                        {{children.MemberName}} = {{rootInstanceName}}.{{childFullPath}}.Select({{item}} => new {{childClass}} {
                            {{WithIndent(RenderBodyOfFromDbEntity(childInstance, childInstance, item, depth + 1), "    ")}}
                        }).ToList(),
                        """;

                } else if (prop is AggregateMember.RelationMember child) {
                    var childClass = child.MemberAggregate.Item.ClassName;
                    var childInstance = child.MemberAggregate;

                    yield return $$"""
                        {{child.MemberName}} = new {{childClass}} {
                            {{WithIndent(RenderBodyOfFromDbEntity(childInstance, rootInstance, rootInstanceName, depth + 1), "    ")}}
                        },
                        """;

                } else {
                    throw new NotImplementedException();
                }
            }
        }
        #endregion FromDbEntity


        #region ToDbEntity
        internal string ToDbEntity(CodeRenderingContext ctx) {
            var context = new BodyRenderingContext(_aggregate, "this", ctx.Config);

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のオブジェクトをデータベースに保存する形に変換します。
                /// </summary>
                public {{ctx.Config.EntityNamespace}}.{{_aggregate.Item.EFCoreEntityClassName}} {{TO_DBENTITY}}() {
                    return new {{ctx.Config.EntityNamespace}}.{{_aggregate.Item.EFCoreEntityClassName}} {
                        {{WithIndent(RenderBodyOfToDbEntity(context), "        ")}}
                    };
                }
                """;
        }
        private IEnumerable<string> RenderBodyOfToDbEntity(BodyRenderingContext context) {
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
                    var childDbEntityClass = $"{context.Config.EntityNamespace}.{nav.Relevant.Owner.Item.EFCoreEntityClassName}";

                    yield return $$"""
                        {{children.MemberName}} = {{context.ValueSourceInstance}}.{{childProp}}.Select({{item}} => new {{childDbEntityClass}} {
                            {{WithIndent(RenderBodyOfToDbEntity(context.Nest(childInstance, item)), "    ")}}
                        }).ToList(),
                        """;

                } else if (prop is AggregateMember.RelationMember child) {
                    var nav = child.GetNavigationProperty();
                    var childProp = nav.Principal.PropertyName;
                    var childInstance = child.MemberAggregate;
                    var childDbEntityClass = $"{context.Config.EntityNamespace}.{nav.Relevant.Owner.Item.EFCoreEntityClassName}";

                    yield return $$"""
                        {{child.MemberName}} = new {{childDbEntityClass}} {
                            {{WithIndent(RenderBodyOfToDbEntity(context.Nest(childInstance)), "    ")}}
                        },
                        """;

                } else {
                    throw new NotImplementedException();
                }
            }
        }

        private class BodyRenderingContext {
            public BodyRenderingContext(GraphNode<Aggregate> aggregate, string instanceName, Config config) {
                _stack = new Stack<Item>();
                _stack.Push(new Item {
                    Instance = instanceName,
                    InstanceType = aggregate,
                    MostRecent1To1Ancestor = aggregate,
                });
                ValueSource = aggregate;
                Config = config;
            }
            private BodyRenderingContext(Stack<Item> stack, GraphNode<Aggregate> valueSource, Config config) {
                _stack = stack;
                ValueSource = valueSource;
                Config = config;
            }
            private readonly Stack<Item> _stack;

            public Config Config { get; }

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
                return new BodyRenderingContext(newStack, valueSource, Config);
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
        #endregion ToDbEntity
    }
}
