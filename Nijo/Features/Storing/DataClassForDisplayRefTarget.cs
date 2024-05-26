using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Storing {
    /// <summary>
    /// 画面に表示される、参照先の集約の情報。
    /// 主キー、<see cref="MemberOptions.IsDisplayName"/>, <see cref="MemberOptions.IsNameLike"/>, が表示される。
    /// </summary>
    internal class DataClassForDisplayRefTarget {

        internal DataClassForDisplayRefTarget(GraphNode<Aggregate> refTo) {
            RefTo = refTo;
        }
        internal GraphNode<Aggregate> RefTo { get; }
        internal string CsTypeName => $"{RefTo.Item.PhysicalName}RefInfo";
        internal string TsTypeName => $"{RefTo.Item.PhysicalName}RefInfo";

        /// <summary>
        /// 登録更新リクエスト時に参照先のインスタンスがDB未登録で主キーが定まっていない可能性があるため、
        /// 参照先インスタンスのUUID等の文字列で参照できるようにするためのプロパティ。
        /// </summary>
        internal const string INSTANCE_KEY = "__instanceKey";

        /// <summary>
        /// インスタンスの名前または名前に準ずるメンバーを列挙する
        /// </summary>
        private static IEnumerable<AggregateMember.AggregateMemberBase> EnumerateNameLikeMembers(GraphNode<Aggregate> aggregate) {
            foreach (var member in aggregate.GetMembers()) {

                if (member is AggregateMember.ValueMember vm) {

                    if (member.DeclaringAggregate == aggregate
                        && (vm.IsKey
                        || vm.IsDisplayName
                        || vm.Options.IsNameLike)) {

                        yield return vm;
                    }

                } else if (member is AggregateMember.Parent parent) {

                    // 無限ループ回避
                    if (parent.MemberAggregate == aggregate.Source?.Source.As<Aggregate>()) continue;

                    yield return parent;

                } else if (member is AggregateMember.Ref @ref) {

                    // Refの場合は明示的にnamelikeに指定されている場合のみ名前扱い
                    if (@ref.Relation.IsPrimary()
                        || @ref.Relation.IsInstanceName()
                        || @ref.Relation.IsNameLike()) yield return @ref;

                } else if (member is AggregateMember.RelationMember child) {

                    // 判断に迷うが以下の理由からChildrenは列挙対象外
                    // - 参照先のChildrenを見たい状況がほぼ無いのではと考えられること
                    // - 参照先検索SQLのパフォーマンスの懸念
                    // - Childrenは必ずキー項目を持っているため、この後の「その子要素等に表示するメンバーが1個以上ある場合だけその子要素等を列挙する」で意図せず引っかかってしまう
                    if (child is AggregateMember.Children) continue;

                    // 無限ループ回避
                    if (child.MemberAggregate == aggregate.Source?.Source.As<Aggregate>()) continue;

                    // その子要素等に表示するメンバーが1個以上ある場合だけその子要素等を列挙する
                    var hasNameLikeMembers = EnumerateNameLikeMembers(child.MemberAggregate).Any();
                    if (hasNameLikeMembers) {
                        yield return child;
                    }
                }
            }
        }

        /// <summary>
        /// 再帰列挙
        /// </summary>
        internal IEnumerable<AggregateMember.ValueMember> GetDisplayMembers() {
            static IEnumerable<AggregateMember.ValueMember> EnumerateRecursively(GraphNode<Aggregate> agg) {
                foreach (var member in EnumerateNameLikeMembers(agg)) {
                    if (member is AggregateMember.ValueMember vm) {
                        yield return vm;

                    } else if (member is AggregateMember.RelationMember rm) {
                        foreach (var vm2 in EnumerateRecursively(rm.MemberAggregate)) {
                            yield return vm2;
                        }
                    }
                }
            }
            foreach (var vm in EnumerateRecursively(RefTo)) {
                yield return vm;
            }
        }

        internal string RenderTsDeclaring() {

            static IEnumerable<string> RenderBody(GraphNode<Aggregate> agg) {
                foreach (var nameLikeMember in EnumerateNameLikeMembers(agg)) {
                    if (nameLikeMember is AggregateMember.ValueMember vm) {
                        yield return $$"""
                            {{vm.MemberName}}?: {{vm.TypeScriptTypename}},
                            """;

                    } else if (nameLikeMember is AggregateMember.Children children) {
                        yield return $$"""
                            {{children.MemberName}}?: {
                              {{WithIndent(RenderBody(children.ChildrenAggregate), "  ")}}
                            }[],
                            """;

                    } else if (nameLikeMember is AggregateMember.RelationMember rm) {
                        yield return $$"""
                            {{rm.MemberName}}?: {
                              {{WithIndent(RenderBody(rm.MemberAggregate), "  ")}}
                            },
                            """;
                    }
                }
            }
            return $$"""
                /** {{RefTo.Item.DisplayName}}を参照する他のデータの画面上に表示される{{RefTo.Item.DisplayName}}のデータ型。 */
                export type {{TsTypeName}} = {
                  /** {{RefTo.Item.DisplayName}}のキー。保存するときはこの値が使用される。
                      新規作成されてからDBに登録されるまでの間の{{RefTo.Item.DisplayName}}をUUID等の不変の値で参照できるようにするために文字列になっている。 */
                  {{INSTANCE_KEY}}: Util.ItemKey

                  {{WithIndent(RenderBody(RefTo.AsEntry()), "  ")}}
                }
                """;
        }

        internal string RenderCsDeclaring() {

            static IEnumerable<string> RenderBody(GraphNode<Aggregate> agg) {
                foreach (var nameLikeMember in EnumerateNameLikeMembers(agg)) {
                    if (nameLikeMember is AggregateMember.ValueMember vm) {
                        yield return $$"""
                            public {{vm.CSharpTypeName}}? {{vm.MemberName}} { get; set; }
                            """;

                    } else if (nameLikeMember is AggregateMember.Children children) {
                        var refTarget = new DataClassForDisplayRefTarget(children.ChildrenAggregate);
                        yield return $$"""
                            public List<{{refTarget.CsTypeName}}> {{children.MemberName}} { get; set; }
                            """;

                    } else if (nameLikeMember is AggregateMember.RelationMember rm) {
                        var refTarget = new DataClassForDisplayRefTarget(rm.MemberAggregate);
                        yield return $$"""
                            public {{refTarget.CsTypeName}}? {{rm.MemberName}} { get; set; }
                            """;
                    }
                }
            }

            return $$"""
                /// <summary>
                /// {{RefTo.Item.DisplayName}}を参照する他のデータの画面上に表示される{{RefTo.Item.DisplayName}}のデータ型。
                /// </summary>
                public partial class {{CsTypeName}} {
                    /// <summary>
                    /// {{RefTo.Item.DisplayName}}のキー。保存するときはこの値が使用される。
                    /// 新規作成されてからDBに登録されるまでの間の{{RefTo.Item.DisplayName}}をUUID等の不変の値で参照できるようにするために文字列になっている。
                    /// </summary>
                    public required string {{INSTANCE_KEY}} { get; set; }

                    {{WithIndent(RenderBody(RefTo.AsEntry()), "    ")}}
                }
                """;
        }
    }

    partial class StoringExtensions {
        internal static IEnumerable<string> GetFullPathAsDisplayRefTargetClass(this AggregateMember.AggregateMemberBase member, GraphNode<Aggregate>? since = null) {
            // おそらくTransactionScopeDataClassと同じデータ構造だと思うので流用する
            return member.GetFullPath(since);
        }
        internal static IEnumerable<string> GetFullPathAsDisplayRefTargetClass(this GraphNode<Aggregate> aggregate, GraphNode<Aggregate>? since = null) {
            // おそらくTransactionScopeDataClassと同じデータ構造だと思うので流用する
            return aggregate.GetFullPath(since);
        }
    }
}
