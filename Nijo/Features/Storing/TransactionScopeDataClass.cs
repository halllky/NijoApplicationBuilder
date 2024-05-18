using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Storing {
    /// <summary>
    /// 登録・更新・削除される範囲を1つの塊とするデータクラス。
    /// 具体的には、ルート集約とその Child, Children, Variaton 達から成る一塊。Refの参照先はこの範囲の外。
    /// </summary>
    internal class TransactionScopeDataClass {
        internal TransactionScopeDataClass(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        protected readonly GraphNode<Aggregate> _aggregate;

        internal virtual string ClassName => _aggregate.Item.ClassName;

        internal const string FROM_DBENTITY = "FromDbEntity";
        internal const string TO_DBENTITY = "ToDbEntity";

        internal IEnumerable<AggregateMember.AggregateMemberBase> GetOwnMembers() {
            return _aggregate
                .GetMembers()
                .Where(m => m.DeclaringAggregate == _aggregate);
        }

        internal virtual string RenderCSharp(CodeRenderingContext ctx) {
            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のデータ1件の詳細を表すクラスです。
                /// </summary>
                public partial class {{ClassName}} {
                {{GetOwnMembers().SelectTextTemplate(prop => $$"""
                    public {{prop.CSharpTypeName}}? {{prop.MemberName}} { get; set; }
                """)}}

                {{If(_aggregate.IsRoot(), () => $$"""
                    {{WithIndent(ToDbEntity(ctx), "    ")}}
                    {{WithIndent(FromDbEntity(ctx), "    ")}}
                """)}}
                }
                """;
        }

        internal virtual string RenderTypeScript(CodeRenderingContext ctx) {
            return $$"""
                export type {{_aggregate.Item.TypeScriptTypeName}} = {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}?: {{m.TypeScriptTypename}}
                """)}}
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

            yield return instanceName;
            foreach (var path in vm.Declared.GetFullPath(valueSource)) {
                yield return path;
            }
        }


        #region FromDbEntity
        internal string FromDbEntity(CodeRenderingContext ctx) {

            static IEnumerable<string> RenderBodyOfFromDbEntity(GraphNode<Aggregate> instance, string instanceName) {

                foreach (var prop in instance.GetMembers()) {
                    if (prop.Owner != prop.DeclaringAggregate) {
                        continue; // 不要

                    } else if (prop is AggregateMember.Parent) {
                        continue;

                    } else if (prop is AggregateMember.ValueMember valueMember) {
                        yield return $$"""
                            {{valueMember.MemberName}} = {{instanceName}}.{{valueMember.GetFullPath().Join("?.")}},
                            """;

                    } else if (prop is AggregateMember.Ref refProp) {

                        string RenderKeyNameConvertingRecursively(AggregateMember.RelationMember refOrParent) {
                            var keyNameClass = new RefTargetKeyName(refOrParent.MemberAggregate);

                            return $$"""
                                {{refOrParent.MemberName}} = new {{keyNameClass.CSharpClassName}}() {
                                {{keyNameClass.GetOwnMembers().SelectTextTemplate(m => m is AggregateMember.ValueMember vm ? $$"""
                                    {{m.MemberName}} = {{instanceName}}.{{vm.GetFullPath().Join("?.")}},
                                """ : $$"""
                                    {{WithIndent(RenderKeyNameConvertingRecursively((AggregateMember.RelationMember)m), "    ")}}
                                """)}}
                                },
                                """;
                        }

                        yield return RenderKeyNameConvertingRecursively(refProp);

                    } else if (prop is AggregateMember.Children children) {
                        var depth = children.Owner.EnumerateAncestors().Count();
                        var loopVar = depth == 0 ? "item" : $"item{depth}";

                        yield return $$"""
                            {{children.MemberName}} = {{instanceName}}.{{children.GetFullPath().Join("?.")}}?.Select({{loopVar}} => new {{children.ChildrenAggregate.Item.ClassName}}() {
                                {{WithIndent(RenderBodyOfFromDbEntity(children.ChildrenAggregate.AsEntry(), loopVar), "    ")}}
                            }).ToList(),
                            """;

                    } else if (prop is AggregateMember.RelationMember child) {

                        yield return $$"""
                            {{child.MemberName}} = new {{child.MemberAggregate.Item.ClassName}}() {
                                {{WithIndent(RenderBodyOfFromDbEntity(child.MemberAggregate, instanceName), "    ")}}
                            },
                            """;

                    } else {
                        throw new NotImplementedException();
                    }
                }
            }

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のデータベースから取得した内容を画面に表示する形に変換します。
                /// </summary>
                public static {{_aggregate.Item.ClassName}} {{FROM_DBENTITY}}({{ctx.Config.EntityNamespace}}.{{_aggregate.Item.EFCoreEntityClassName}} entity) {
                    var instance = new {{_aggregate.Item.ClassName}} {
                        {{WithIndent(RenderBodyOfFromDbEntity(_aggregate, "entity"), "        ")}}
                    };
                    return instance;
                }
                """;
        }
        #endregion FromDbEntity


        #region ToDbEntity
        internal string ToDbEntity(CodeRenderingContext ctx) {

            static IEnumerable<string> RenderBodyOfToDbEntity(GraphNode<Aggregate> renderingAggregate, Config config) {
                foreach (var prop in renderingAggregate.GetMembers()) {
                    if (prop is AggregateMember.ValueMember vm) {
                        var entry = renderingAggregate.GetEntry().As<Aggregate>();
                        var path = GetPathOf("this", entry, vm);

                        yield return $$"""
                            {{vm.MemberName}} = {{path.First()}}.{{path.Skip(1).Join("?.")}},
                            """;

                    } else if (prop is AggregateMember.Ref refProp) {
                        continue;

                    } else if (prop is AggregateMember.Parent) {
                        continue;

                    } else if (prop is AggregateMember.Children children) {
                        var nav = children.GetNavigationProperty();
                        var childDbEntityClass = $"{config.EntityNamespace}.{nav.Relevant.Owner.Item.EFCoreEntityClassName}";
                        var (instanceName, valueSource) = GetSourceInstanceOf("this", children.DeclaringAggregate);
                        var (item, _) = GetSourceInstanceOf("this", children.ChildrenAggregate);

                        yield return $$"""
                            {{children.MemberName}} = {{instanceName}}.{{prop.GetFullPath(valueSource).Join("?.")}}?.Select({{item}} => new {{childDbEntityClass}} {
                                {{WithIndent(RenderBodyOfToDbEntity(children.ChildrenAggregate, config), "    ")}}
                            }).ToHashSet() ?? new HashSet<{{childDbEntityClass}}>(),
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


        #region 値の同一比較
        internal string DeepEqualTsFnName => $"deepEquals{_aggregate.Item.ClassName}";
        internal string RenderTsDeppEquals() {

            static string Render(string instanceA, string instanceB, GraphNode<Aggregate> agg) {
                var agg2 = agg.IsChildrenMember() ? agg.AsEntry() : agg;
                var compareMembers = agg2
                    .GetMembers()
                    .OfType<AggregateMember.ValueMember>()
                    .Where(vm => vm.Inherits?.Relation.IsParentChild() != true);
                var childAggregates = agg2
                    .GetMembers()
                    .OfType<AggregateMember.RelationMember>()
                    .Where(rel => rel is not AggregateMember.Ref
                               && rel is not AggregateMember.Parent);

                if (agg2.IsChildrenMember()) {
                    var thisPath = agg.GetFullPath().Select(p => $"?.{p}").Join("");
                    var arrA = $"{instanceA}{thisPath}";
                    var arrB = $"{instanceB}{thisPath}";

                    var depth = agg2.EnumerateAncestors().Count();
                    var i = $"i{depth}";
                    var itemA = $"a{depth}";
                    var itemB = $"b{depth}";

                    // TODO: 要素の順番を考慮していない
                    return $$"""

                        // {{agg2.Item.DisplayName}}
                        if ({{arrA}}?.length !== {{arrB}}?.length) return false
                        if ({{arrA}} !== undefined && {{arrB}} !== undefined) {
                          for (let {{i}} = 0; {{i}} < {{arrA}}.length; {{i}}++) {
                            const {{itemA}} = {{arrA}}[{{i}}]
                            const {{itemB}} = {{arrB}}[{{i}}]
                        {{compareMembers.SelectTextTemplate(vm => $$"""
                            if ({{itemA}}.{{vm.Declared.GetFullPath().Join("?.")}} !== {{itemB}}.{{vm.Declared.GetFullPath().Join("?.")}}) return false
                        """)}}
                        {{childAggregates.SelectTextTemplate(rel => $$"""
                            {{WithIndent(Render(itemA, itemB, rel.MemberAggregate), "    ")}}
                        """)}}
                          }
                        }
                        """;

                } else {
                    return $$"""

                        {{If(!agg2.IsRoot(), () => $$"""
                        // {{agg2.Item.DisplayName}}
                        """)}}
                        {{compareMembers.SelectTextTemplate(vm => $$"""
                        if ({{instanceA}}?.{{vm.Declared.GetFullPath().Join("?.")}} !== {{instanceB}}?.{{vm.Declared.GetFullPath().Join("?.")}}) return false
                        """)}}
                        {{childAggregates.SelectTextTemplate(rel => $$"""
                        {{Render(instanceA, instanceB, rel.MemberAggregate)}}
                        """)}}
                        """;
                }
            }

            return $$"""
                export const {{DeepEqualTsFnName}} = (a: {{_aggregate.Item.TypeScriptTypeName}}, b: {{_aggregate.Item.TypeScriptTypeName}}): boolean => {
                  {{WithIndent(Render("a", "b", _aggregate), "  ")}}

                  return true
                }
                """;
        }
        #endregion 値の同一比較
    }

    internal static partial class StoringExtensions {

        /// <summary>
        /// エントリーからのパスを <see cref="TransactionScopeDataClass"/> のインスタンスの型のルールにあわせて返す。
        /// </summary>
        internal static IEnumerable<string> GetFullPath(this GraphNode<Aggregate> aggregate, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var path = aggregate.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);

            foreach (var edge in path) {
                if (edge.Source == edge.Terminal && edge.IsParentChild()) {
                    yield return AggregateMember.PARENT_PROPNAME; // 子から親に向かって辿る場合
                } else {
                    yield return edge.RelationName;
                }
            }
        }

        /// <summary>
        /// エントリーからのパスを <see cref="TransactionScopeDataClass"/> のインスタンスの型のルールにあわせて返す。
        /// </summary>
        internal static IEnumerable<string> GetFullPath(this AggregateMember.AggregateMemberBase member, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            foreach (var path in member.Owner.GetFullPath(since, until)) {
                yield return path;
            }
            yield return member.MemberName;
        }
    }
}
