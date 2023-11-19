using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalApplicationBuilder.CodeRendering.Util;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace HalApplicationBuilder.CodeRendering.InstanceHandling {
    internal class AggregateDetail {
        internal AggregateDetail(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        protected readonly GraphNode<Aggregate> _aggregate;

        internal virtual string ClassName => _aggregate.Item.ClassName;

        /// <summary>
        /// 編集画面でDBから読み込んだデータとその画面中で新たに作成されたデータで
        /// 挙動を分けるためのフラグ
        /// </summary>
        internal const string IS_LOADED = "__loaded";
        /// <summary>
        /// - useFieldArrayの中で配列インデックスをキーに使うと新規追加されたコンボボックスが
        ///   その1個上の要素の更新と紐づいてしまうのでクライアント側で要素1個ずつにIDを振る
        /// - TabGroupでどのタブがアクティブになっているかの判定にも使う
        /// </summary>
        internal const string OBJECT_ID = "__object_id";

        internal const string FROM_DBENTITY = "FromDbEntity";
        internal const string TO_DBENTITY = "ToDbEntity";

        internal IEnumerable<AggregateMember.AggregateMemberBase> GetOwnMembers() {
            return _aggregate
                .GetMembers()
                .Where(m => m.Declaring == _aggregate);
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
                    public partial class {{ClassName}} {
                {{GetOwnMembers().SelectTextTemplate(prop => $$"""
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
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}?: {{m.TypeScriptTypename}}
                """)}}
                  {{IS_LOADED}}?: boolean
                  {{OBJECT_ID}}?: string
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
                    return instance;
                }
                """;
        }
        private IEnumerable<string> RenderBodyOfFromDbEntity(GraphNode<Aggregate> instance, GraphNode<Aggregate> rootInstance, string rootInstanceName, int depth) {
            foreach (var prop in instance.GetMembers()) {
                if (prop.Owner != prop.Declaring) {
                    continue; // 不要

                } else if (prop is AggregateMember.ValueMember valueMember) {
                    yield return $$"""
                        {{valueMember.MemberName}} = {{rootInstanceName}}.{{valueMember.GetFullPath(rootInstance).Join(".")}},
                        """;

                } else if (prop is AggregateMember.Ref refProp) {

                    string RenderKeyNameConvertingRecursively(AggregateMember.Ref refMember) {
                        var keyNameClass = new RefTargetKeyName(refMember.MemberAggregate);

                        return $$"""
                            {{refMember.MemberName}} = new {{keyNameClass.CSharpClassName}}() {
                            {{keyNameClass.GetKeysAndNames().SelectTextTemplate(m => m is AggregateMember.ValueMember vm ? $$"""
                                {{m.MemberName}} = {{rootInstanceName}}.{{vm.GetDbColumn().GetFullPath(rootInstance.As<IEFCoreEntity>()).Join(".")}},
                            """ : $$"""
                                {{WithIndent(RenderKeyNameConvertingRecursively((AggregateMember.Ref)m), "    ")}}
                            """)}}
                            },
                            """;
                    }

                    yield return RenderKeyNameConvertingRecursively(refProp);

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
                if (prop is AggregateMember.ValueMember valueMember) {
                    yield return $$"""
                        {{valueMember.GetDbColumn().Options.MemberName}} = {{context.ValueSourceInstance}}.{{valueMember.GetFullPath(context.ValueSource).Join(".")}},
                        """;

                } else if (prop is AggregateMember.Ref refProp) {
                    continue; // 参照先のキーはValueMemberの分岐で処理済み

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
