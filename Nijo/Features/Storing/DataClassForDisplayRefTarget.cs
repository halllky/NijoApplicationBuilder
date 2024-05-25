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
        internal string TsTypeName => $"{RefTo.Item.ClassName}RefInfo";

        /// <summary>
        /// インスタンスの名前または名前に準ずるメンバーを列挙する
        /// </summary>
        private static IEnumerable<AggregateMember.AggregateMemberBase> EnumerateNameLikeMembers(GraphNode<Aggregate> aggregate) {
            var visited = new HashSet<(GraphNode<Aggregate>, GraphPath)>();

            IEnumerable<AggregateMember.AggregateMemberBase> Visit(GraphNode<Aggregate> agg) {
                foreach (var member in agg.GetMembers()) {
                    if (member is AggregateMember.ValueMember vm
                        && vm.DeclaringAggregate == vm.Owner
                        && (vm.IsKey || vm.Options.IsNameLike)) {

                        yield return vm;

                    } else if (member is AggregateMember.RelationMember rm) {

                        // 無限ループ回避
                        var identifier = (rm.MemberAggregate, rm.MemberAggregate.PathFromEntry());
                        if (visited.Contains(identifier)) continue;
                        visited.Add(identifier);

                        // Refの場合は明示的にnamelikeに指定されている場合のみ名前扱い
                        if (rm is AggregateMember.Ref
                            && !rm.Relation.IsPrimary()
                            && !rm.Relation.IsInstanceName()
                            && !rm.Relation.IsNameLike()) continue;

                        var nameLikeMembers = Visit(rm.MemberAggregate).ToArray();
                        if (nameLikeMembers.Length > 0) {

                            // その子要素等に表示するメンバーが1個以上ある場合だけその子要素等を列挙する
                            yield return rm;

                            foreach (var m in nameLikeMembers) {
                                yield return m;
                            }
                        }
                    }
                }
            }

            foreach (var nameLikeMember in Visit(aggregate)) {
                yield return nameLikeMember;
            }
        }

        internal IEnumerable<AggregateMember.AggregateMemberBase> GetDisplayMembers() {
            foreach (var vm in EnumerateNameLikeMembers(RefTo)) {
                yield return vm;
            }
        }

        internal string Render() {
            static IEnumerable<string> RenderBody(GraphNode<Aggregate> agg) {
                foreach (var nameLikeMember in EnumerateNameLikeMembers(agg)) {
                    if (nameLikeMember.DeclaringAggregate != agg) {
                        continue;

                    } else if (nameLikeMember is AggregateMember.ValueMember vm) {
                        yield return $$"""
                            {{vm.MemberName}}?: {{vm.TypeScriptTypename}},
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
                export type {{TsTypeName}} = {
                  {{WithIndent(RenderBody(RefTo), "  ")}}
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
