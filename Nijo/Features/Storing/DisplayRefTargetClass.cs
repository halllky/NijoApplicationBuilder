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
    internal class DisplayRefTargetClass {

        internal DisplayRefTargetClass(GraphNode<Aggregate> refTo) {
            RefTo = refTo;
        }
        internal GraphNode<Aggregate> RefTo { get; }
        internal string TsTypeName => $"{RefTo.Item.ClassName}RefInfo";

        internal IEnumerable<AggregateMember.AggregateMemberBase> GetDisplayMembers() {
            static IEnumerable<AggregateMember.AggregateMemberBase> Enumerate(GraphNode<Aggregate> agg) {
                foreach (var member in agg.GetMembers()) {
                    if (member is AggregateMember.ValueMember vm
                        && vm.DeclaringAggregate == vm.Owner
                        && (vm.IsKey || vm.Options.IsNameLike)) {

                        yield return vm;

                    } else if (member is AggregateMember.RelationMember rm) {
                        var recursively = Enumerate(rm.MemberAggregate).ToArray();
                        if (recursively.Length > 0) {

                            // その子要素等に表示するメンバーが1個以上ある場合だけその子要素等を列挙する
                            yield return rm;

                            foreach (var m in recursively) {
                                yield return m;
                            }
                        }
                    }
                }
            }
            foreach (var vm in Enumerate(RefTo)) {
                yield return vm;
            }
        }

        internal string Render() {

            static IEnumerable<string> RenderBody(GraphNode<Aggregate> agg) {
                foreach (var member in agg.GetMembers()) {
                    if (member is AggregateMember.ValueMember vm
                        && vm.DeclaringAggregate == vm.Owner
                        && (vm.IsKey || vm.Options.IsNameLike)) {

                        yield return $$"""
                            {{vm.MemberName}}?: {{vm.TypeScriptTypename}},
                            """;

                    } else if (member is AggregateMember.RelationMember rm) {
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
