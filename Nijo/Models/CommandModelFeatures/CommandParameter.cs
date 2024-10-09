using Nijo.Core;
using Nijo.Models.RefTo;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.CommandModelFeatures {
    /// <summary>
    /// <see cref="CommandModel"/> のパラメータの型
    /// </summary>
    internal class CommandParameter {
        internal CommandParameter(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string CsClassName => $"{_aggregate.Item.PhysicalName}Parameter{GetUniqueId()}";
        internal string TsTypeName => $"{_aggregate.Item.PhysicalName}Parameter{GetUniqueId()}";
        internal string MessageDataCsClassName => $"{_aggregate.Item.PhysicalName}ParameterMessages{GetUniqueId()}";

        /// <summary>
        /// 異なるコマンドの子孫要素同士で名称衝突するのを防ぐためにフルパスの経路をクラス名に含める
        /// </summary>
        private string GetUniqueId() {
            return _aggregate.IsRoot()
                ? string.Empty
                : $"_{_aggregate.GetRoot().Item.UniqueId.Substring(0, 8)}"; // 8桁も切り取れば重複しないはず
        }

        internal IEnumerable<Member> GetOwnMembers() {
            return _aggregate
                .GetMembers()
                .Where(m => m.DeclaringAggregate == _aggregate)
                .Select(m => new Member(m));
        }
        private IEnumerable<CommandParameter> EnumerateThisAndDescendants() {
            yield return this;

            var child = GetOwnMembers()
                .Select(m => m.GetMemberParameter())
                .OfType<CommandParameter>()
                .SelectMany(c => c.EnumerateThisAndDescendants());
            foreach (var item in child) {
                yield return item;
            }
        }

        internal string RenderCSharpDeclaring(CodeRenderingContext context) {
            return EnumerateThisAndDescendants().SelectTextTemplate(param => $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}処理のパラメータ{{(param._aggregate.IsRoot() ? "" : "の一部")}}
                /// </summary>
                public partial class {{param.CsClassName}} {
                {{param.GetOwnMembers().SelectTextTemplate(m => $$"""
                    public virtual {{m.CsTypeName}}? {{m.MemberName}} { get; set; }
                """)}}
                }
                """);
        }

        /// <summary>
        /// この集約がグリッドで表示される場合はエラーメッセージの表示方法が特殊
        /// （グリッドのヘッダとセル内部の計2か所にエラーメッセージが出る）
        /// </summary>
        private static bool IsInGrid(GraphNode<Aggregate> agg) {
            return agg
            .EnumerateAncestorsAndThis()
            .Any(agg => agg.IsChildrenMember()
                     && agg.CanDisplayAllMembersAs2DGrid());
        }

        internal string RenderCSharpMessageClassDeclaring(CodeRenderingContext context) {
            return EnumerateThisAndDescendants().SelectTextTemplate(param => {
                var members = param.GetOwnMembers().ToArray();

                // この集約がグリッドで表示される場合はエラーメッセージの表示方法が特殊
                var isInGrid = IsInGrid(param._aggregate);

                // このクラスが継承する基底クラスやインターフェース
                var implements = new List<string>();
                if (isInGrid) {
                    implements.Add(DisplayMessageContainer.CONCRETE_CLASS_IN_GRID);
                } else {
                    implements.Add(DisplayMessageContainer.ABSTRACT_CLASS);
                }

                string[] args;
                if (isInGrid) {
                    args = ["IEnumerable<string> path", $"{DisplayMessageContainer.ABSTRACT_CLASS} grid", "int rowIndex"];
                } else if (!param._aggregate.IsRoot()) {
                    args = ["IEnumerable<string> path"];
                } else {
                    args = []; // ルート集約の場合
                }

                string[] @base;
                if (isInGrid) {
                    @base = ["path, grid, rowIndex"];
                } else if (!param._aggregate.IsRoot()) {
                    @base = ["path"];
                } else {
                    @base = ["[]"]; // ルート集約の場合
                }

                return $$"""
                    /// <summary>
                    /// {{_aggregate.Item.DisplayName}}処理のパラメータ{{(param._aggregate.IsRoot() ? "" : "の一部")}}のメッセージ格納用クラス
                    /// </summary>
                    public partial class {{param.MessageDataCsClassName}} : {{implements.Join(", ")}} {
                        public {{param.MessageDataCsClassName}}({{args.Join(", ")}}) : base({{@base.Join(", ")}}) {
                    {{members.SelectTextTemplate(m => $$"""
                            {{WithIndent(RenderConstructor(m.MemberInfo), "        ")}}
                    """)}}
                        }
                    {{members.SelectTextTemplate(m => $$"""
                        public virtual {{m.CsErrorMemberType}} {{m.MemberName}} { get; }
                    """)}}

                        public override IEnumerable<{{DisplayMessageContainer.INTERFACE}}> EnumerateChildren() {
                    {{If(members.Length == 0, () => $$"""
                            yield break;
                    """)}}
                    {{members.SelectTextTemplate(m => $$"""
                            yield return {{m.MemberName}};
                    """)}}
                        }
                    }
                    """;

                string RenderConstructor(AggregateMember.AggregateMemberBase member) {
                    var path = param._aggregate.IsRoot()
                        ? string.Empty
                        : ".. path, ";

                    if (member is AggregateMember.ValueMember || member is AggregateMember.Ref) {
                        return isInGrid ? $$"""
                            {{member.MemberName}} = new {{DisplayMessageContainer.CONCRETE_CLASS_IN_GRID}}([{{path}}"{{member.MemberName}}"], grid, rowIndex);
                            """ : $$"""
                            {{member.MemberName}} = new {{DisplayMessageContainer.CONCRETE_CLASS}}([{{path}}"{{member.MemberName}}"]);
                            """;

                    } else if (member is AggregateMember.Children children) {
                        var desc = new CommandParameter(children.ChildrenAggregate);
                        return $$"""
                            {{member.MemberName}} = new([{{path}}"{{member.MemberName}}"], rowIndex => {
                            {{If(children.ChildrenAggregate.CanDisplayAllMembersAs2DGrid(), () => $$"""
                                return new {{desc.MessageDataCsClassName}}([{{path}}"{{member.MemberName}}", rowIndex.ToString()], {{member.MemberName}}!, rowIndex);
                            """).Else(() => $$"""
                                return new {{desc.MessageDataCsClassName}}([{{path}}"{{member.MemberName}}", rowIndex.ToString()]);
                            """)}}
                            });
                            """;

                    } else if (member is AggregateMember.RelationMember rel) {
                        var desc = new CommandParameter(rel.MemberAggregate);
                            return $$"""
                            {{member.MemberName}} = new {{desc.MessageDataCsClassName}}([{{path}}"{{member.MemberName}}"]);
                            """;

                    } else {
                        throw new NotImplementedException();
                    }
                }
            });
        }

        internal string RenderTsDeclaring(CodeRenderingContext context) {
            return EnumerateThisAndDescendants().SelectTextTemplate(param => $$"""
                /** {{_aggregate.Item.DisplayName}}処理のパラメータ{{(param._aggregate.IsRoot() ? "" : "の一部")}} */
                export type {{param.TsTypeName}} = {
                {{param.GetOwnMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}?: {{m.TsTypeName}}
                """)}}
                }
                """);
        }

        internal string TsNewObjectFunction => $"createEmpty{TsTypeName}";
        internal string RenderTsNewObjectFunction(CodeRenderingContext context) {
            var aggregates = _aggregate
                .EnumerateThisAndDescendants()
                .Where(a => a.IsRoot() || a.IsChildrenMember())
                .Select(a => new CommandParameter(a));

            return aggregates.SelectTextTemplate(p => $$"""
                export const {{p.TsNewObjectFunction}} = (): {{p.TsTypeName}} => ({
                {{p.GetOwnMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}: {{WithIndent(m.RenderInitializer(), "  ")}},
                """)}}
                })
                """);
        }

        /// <summary>
        /// <see cref="CommandParameter"/> のメンバー
        /// </summary>
        internal class Member {
            internal Member(AggregateMember.AggregateMemberBase member) {
                MemberInfo = member;
            }
            internal AggregateMember.AggregateMemberBase MemberInfo { get; }

            internal string MemberName => MemberInfo.MemberName;
            internal string CsTypeName => MemberInfo switch {
                AggregateMember.ValueMember vm => vm.Options.MemberType.GetCSharpTypeName(),
                AggregateMember.Children children => $"List<{new CommandParameter(children.ChildrenAggregate).CsClassName}>",
                AggregateMember.Child child => new CommandParameter(child.ChildAggregate).CsClassName,
                AggregateMember.VariationItem variation => new CommandParameter(variation.VariationAggregate).CsClassName,
                AggregateMember.Ref @ref => new RefTo.RefDisplayData(@ref.RefTo, @ref.RefTo).CsClassName,
                _ => throw new NotImplementedException(),
            };
            internal string TsTypeName => MemberInfo switch {
                AggregateMember.ValueMember vm => vm.Options.MemberType.GetTypeScriptTypeName(),
                AggregateMember.Children children => $"{new CommandParameter(children.ChildrenAggregate).TsTypeName}[]",
                AggregateMember.Child child => new CommandParameter(child.ChildAggregate).TsTypeName,
                AggregateMember.VariationItem variation => new CommandParameter(variation.VariationAggregate).TsTypeName,
                AggregateMember.Ref @ref => new RefTo.RefDisplayData(@ref.RefTo, @ref.RefTo).TsTypeName,
                _ => throw new NotImplementedException(),
            };

            internal string CsErrorMemberType => MemberInfo switch {
                AggregateMember.ValueMember => IsInGrid(MemberInfo.Owner)
                    ? DisplayMessageContainer.CONCRETE_CLASS_IN_GRID
                    : DisplayMessageContainer.CONCRETE_CLASS,
                AggregateMember.Children children => $"{DisplayMessageContainer.CONCRETE_CLASS_LIST}<{new CommandParameter(children.ChildrenAggregate).MessageDataCsClassName}>",
                AggregateMember.Child child => new CommandParameter(child.ChildAggregate).MessageDataCsClassName,
                AggregateMember.VariationItem variation => new CommandParameter(variation.VariationAggregate).MessageDataCsClassName,
                AggregateMember.Ref => IsInGrid(MemberInfo.Owner)
                    ? DisplayMessageContainer.CONCRETE_CLASS_IN_GRID
                    : DisplayMessageContainer.CONCRETE_CLASS,
                _ => throw new NotImplementedException(),
            };

            internal CommandParameter? GetMemberParameter() {
                if (MemberInfo is not AggregateMember.RelationMember rel) return null;
                if (MemberInfo is AggregateMember.Ref) return null;

                return new CommandParameter(rel.MemberAggregate);
            }

            /// <summary>
            /// オブジェクト新規作成関数の中での値初期化
            /// </summary>
            internal string RenderInitializer() {
                if (MemberInfo is AggregateMember.Variation variation) {
                    var first = variation.GetGroupItems().First().TsValue;
                    return $"'{first}'";

                } else if (MemberInfo is AggregateMember.ValueMember) {
                    return $"undefined"; // 初期値なし

                } else if (MemberInfo is AggregateMember.Ref) {
                    return $"undefined"; // 初期値なし

                } else if (MemberInfo is AggregateMember.Children) {
                    return $"[]";

                } else {
                    var childParam = GetMemberParameter()!;
                    return $$"""
                        {
                        {{childParam.GetOwnMembers().SelectTextTemplate(m => $$"""
                          {{m.MemberName}}: {{WithIndent(m.RenderInitializer(), "  ")}},
                        """)}}
                        }
                        """;
                }
            }
        }
    }


    internal static partial class GetFullPathExtensions {
        /// <summary>
        /// エントリーからのパスを
        /// <see cref="CommandParameter"/> と
        /// <see cref="RefTo.RefSearchCondition"/> の
        /// インスタンスの型のルールにあわせて返す。
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsCommandParameter(this AggregateMember.AggregateMemberBase member, GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
            var path = member.Owner.PathFromEntry();
            if (since != null) path = path.Since(since);
            if (until != null) path = path.Until(until);

            foreach (var e in path) {
                var edge = e.As<Aggregate>();
                if (edge.Terminal.IsInEntryTree()) {
                    // コマンドのツリー内部のパス。コマンドパラメータ型の定義に合わせる
                    if (edge.Source == edge.Terminal && edge.IsParentChild()) {
                        // 子から親へ向かう経路の場合
                        yield return $"/* エラー！{nameof(CommandParameter)}では子は親の参照を持っていません */";

                    } else {
                        var asMember = edge.Terminal.AsChildRelationMember();
                        yield return new CommandParameter.Member(asMember).MemberName;
                    }

                } else {
                    // コマンドのツリー外部のパス。Refの画面表示データの定義に合わせる
                    foreach (var p in member.GetFullPathAsDataClassForRefTarget(since: edge.Initial)) {
                        yield return p;
                    }
                    yield break;
                }
            }

            yield return new CommandParameter.Member(member).MemberName;
        }

        /// <summary>
        /// エントリーからのパスを
        /// <see cref="CommandParameter"/> と
        /// <see cref="RefTo.RefSearchCondition"/> の
        /// インスタンスの型のルールにあわせて返す。（React hook form のパス形式）
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsCommandParameterRHFRegisterName(this AggregateMember.AggregateMemberBase member, IEnumerable<string>? arrayIndexes = null) {
            var currentArrayIndex = 0;
            var path = member.Owner.PathFromEntry();

            foreach (var e in path) {
                var edge = e.As<Aggregate>();
                if (edge.Terminal.IsInEntryTree()) {
                    // コマンドのツリー内部のパス。コマンドパラメータ型の定義に合わせる
                    if (edge.Source == edge.Terminal && edge.IsParentChild()) {
                        // 子から親へ向かう経路の場合
                        yield return $"/* エラー！{nameof(CommandParameter)}では子は親の参照を持っていません */";

                    } else {
                        var asMember = edge.Terminal.AsChildRelationMember();
                        yield return new CommandParameter.Member(asMember).MemberName;

                        // 配列インデックス
                        var isChildren = edge.Source == edge.Initial && edge.Terminal.IsChildrenMember();
                        var isLastChildren = member is AggregateMember.Children children && edge == children.Relation; // 配列自身に対するフルパス列挙の場合は末尾の配列インデックスは列挙しない
                        if (isChildren && !isLastChildren) {
                            yield return $"${{{arrayIndexes?.ElementAtOrDefault(currentArrayIndex)}}}";
                            currentArrayIndex++;
                        }
                    }

                } else {
                    // コマンドのツリー外部のパス。Refの画面表示データの定義に合わせる
                    foreach (var p in member.GetFullPathAsDataClassForRefTarget(since: edge.Initial)) {
                        yield return p;
                    }
                    yield break;
                }
            }

            yield return new CommandParameter.Member(member).MemberName;
        }
    }
}
