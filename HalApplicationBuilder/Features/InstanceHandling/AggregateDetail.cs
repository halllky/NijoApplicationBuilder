using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using static HalApplicationBuilder.Features.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalApplicationBuilder.Features.Util;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace HalApplicationBuilder.Features.InstanceHandling {
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
                .Where(m => m.DeclaringAggregate == _aggregate);
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

        /// <summary>
        /// 以下の事情を考慮したパスを返す。
        /// - Childrenのメンバーはthis等ではなくSelectのラムダ式の変数のメンバーになる
        /// - 参照先のメンバーは <see cref="RefTargetKeyName"/> に生成されるメンバーになる
        ///   - <see cref="RefTargetKeyName"/> のメンバーのうち祖先のキーはそのKeyNameクラスの中で定義されている
        ///   - <see cref="RefTargetKeyName"/> のメンバーのうちさらに参照先のキーがある場合はKeyNameクラスの入れ子になる
        /// </summary>
        internal static IEnumerable<string> GetPathOf(string instance, GraphNode<Aggregate> entry, AggregateMember.ValueMember vm) {
            // 移送元インスタンスを特定する
            GraphNode<Aggregate> source;
            if (vm.DeclaringAggregate.IsInTreeOf(entry)) {
                source = vm.DeclaringAggregate;
            } else {
                // ツリー外に出る直前の集約がSourceInstance
                source = entry;
                foreach (var edge in vm.DeclaringAggregate.PathFromEntry()) {
                    var a = edge.Initial.As<Aggregate>();
                    if (a.IsInTreeOf(entry)) source = a;
                }
            }
            var (instanceName, valueSource) = GetSourceInstanceOf(instance, source);

            // 移送元プロパティを特定する
            var original = vm;
            while (true) {
                if (original.Original == null) break;

                // RefTargetKeyNameの都合上、
                // 参照先の主キーに祖先のキーが含まれている場合は祖先でなく子孫のプロパティが移送元
                if (!original.Owner.IsInTreeOf(entry)
                    && original.IsKeyOfAncestor) break;

                original = original.Original;
            }

            yield return instanceName;
            foreach (var path in original.GetFullPath(valueSource)) {
                yield return path;
            }
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
                if (prop.Owner != prop.DeclaringAggregate) {
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
                            {{keyNameClass.GetMembers().SelectTextTemplate(m => m is AggregateMember.ValueMember vm ? $$"""
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
            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のオブジェクトをデータベースに保存する形に変換します。
                /// </summary>
                public {{ctx.Config.EntityNamespace}}.{{_aggregate.Item.EFCoreEntityClassName}} {{TO_DBENTITY}}() {
                    return new {{ctx.Config.EntityNamespace}}.{{_aggregate.Item.EFCoreEntityClassName}} {
                        {{WithIndent(RenderBodyOfToDbEntity(_aggregate, ctx.Config), "        ")}}
                    };
                }
                """;
        }
        private static IEnumerable<string> RenderBodyOfToDbEntity(GraphNode<Aggregate> renderingAggregate, Config config) {
            foreach (var prop in renderingAggregate.GetMembers()) {
                if (prop is AggregateMember.ValueMember vm) {
                    var entry = renderingAggregate.GetEntry().As<Aggregate>();
                    var path = GetPathOf("this", entry, vm);

                    yield return $$"""
                        {{vm.GetDbColumn().Options.MemberName}} = {{path.Join(".")}},
                        """;

                } else if (prop is AggregateMember.Ref refProp) {
                    continue;

                } else if (prop is AggregateMember.Children children) {
                    var nav = children.GetNavigationProperty();
                    var childDbEntityClass = $"{config.EntityNamespace}.{nav.Relevant.Owner.Item.EFCoreEntityClassName}";
                    var (instanceName, valueSource) = GetSourceInstanceOf("this", children.DeclaringAggregate);
                    var (item, _) = GetSourceInstanceOf("this", children.MemberAggregate);

                    yield return $$"""
                        {{children.MemberName}} = {{instanceName}}.{{prop.GetFullPath(valueSource).Join(".")}}.Select({{item}} => new {{childDbEntityClass}} {
                            {{WithIndent(RenderBodyOfToDbEntity(children.MemberAggregate, config), "    ")}}
                        }).ToList(),
                        """;

                } else if (prop is AggregateMember.RelationMember child) {
                    var nav = child.GetNavigationProperty();
                    var childProp = nav.Principal.PropertyName;
                    var childDbEntityClass = $"{config.EntityNamespace}.{nav.Relevant.Owner.Item.EFCoreEntityClassName}";

                    yield return $$"""
                        {{child.MemberName}} = new {{childDbEntityClass}} {
                            {{WithIndent(RenderBodyOfToDbEntity(child.MemberAggregate, config), "    ")}}
                        },
                        """;

                } else {
                    throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// 集約ツリーの途中でChildrenが挟まるたびに Select(x => ...) によって値取得元インスタンスが変わる問題を解決する
        /// </summary>
        private static (string instanceName, GraphNode<Aggregate>) GetSourceInstanceOf(string instance, GraphNode<Aggregate> aggregate) {
            var valueSourceAggregate = aggregate
                .EnumerateAncestorsAndThis()
                .Reverse()
                .First(a => a.IsRoot() || a.IsChildrenMember());
            var instanceName = valueSourceAggregate.IsRoot()
                ? instance
                : $"item{valueSourceAggregate.EnumerateAncestors().Count()}";

            return (instanceName, valueSourceAggregate);
        }
        #endregion ToDbEntity
    }
}
