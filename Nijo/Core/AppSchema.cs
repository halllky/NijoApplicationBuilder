using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core {
    public class AppSchema {
        internal static AppSchema Empty() => new(string.Empty, DirectedGraph.Empty(), new HashSet<EnumDefinition>());

        internal AppSchema(string appName, DirectedGraph directedGraph, IReadOnlyCollection<EnumDefinition> enumDefinitions) {
            ApplicationName = appName;
            Graph = directedGraph;
            EnumDefinitions = enumDefinitions;
        }

        public string ApplicationName { get; }

        internal DirectedGraph Graph { get; }
        internal IEnumerable<GraphNode<Aggregate>> AllAggregates() {
            return RootAggregates().SelectMany(a => a.EnumerateThisAndDescendants());
        }
        internal IEnumerable<GraphNode<Aggregate>> RootAggregates() {
            return Graph.Only<Aggregate>().Where(aggregate => aggregate.IsRoot());
        }
        internal GraphNode<Aggregate> GetAggregate(NodeId id) {
            return Graph.Single(x => x.Item.Id == id).As<Aggregate>();
        }

        internal IReadOnlyCollection<EnumDefinition> EnumDefinitions { get; }

        /// <summary>
        /// データの流れの順（=Refされている順）に列挙
        /// </summary>
        internal IEnumerable<GraphNode<Aggregate>> RootAggregatesOrderByDataFlow() {
            var rest = RootAggregates()
                .Select(root => new {
                    root,
                    refTargets = root
                        .EnumerateThisAndDescendants()
                        .SelectMany(agg => agg.GetMembers())
                        .OfType<AggregateMember.Ref>()
                        .Select(@ref => @ref.MemberAggregate.GetRoot())
                        .ToHashSet(),
                })
                .ToList();
            var index = 0;
            while (true) {
                if (rest.Count == 0) yield break;

                var next = rest[index];

                // 参照先集約が未処理ならば後回し
                var notEnumerated = rest.Where(agg => next.refTargets.Contains(agg.root));
                if (notEnumerated.Any()) {
                    // 集約間の循環参照が存在するなどの場合は無限ループが発生するので例外。
                    // なお循環参照はスキーマ作成時にエラーとする想定
                    if (index + 1 >= rest.Count) throw new InvalidOperationException("集約間のデータの流れを決定できません。");

                    index++;
                    continue;
                }

                // 参照先集約が無い == nextは現在のrestの中で再上流の集約
                yield return next.root;

                rest.Remove(next);
                index = 0;
            }
        }

        /// <summary>
        /// デバッグ用TSV。Excelやスプレッドシートに貼り付けて構造の妥当性を確認するのに使う
        /// </summary>
        internal string DumpTsv() {
            var builder = new StringBuilder();

            var allAggregates = RootAggregates()
                .SelectMany(a => a.EnumerateThisAndDescendants())
                .ToArray();
            var maxIndent = allAggregates.Max(a => a.EnumerateAncestors().Count());

            var columns = new List<(string, Func<AggregateMember.AggregateMemberBase, string>)> {
                (
                    "メンバー型",
                    member => member.GetType().Name
                ), (
                    nameof(AggregateMember.ValueMember.Original),
                    member => member is AggregateMember.ValueMember vm
                        ? (vm.Original?.MemberName ?? "null")
                        : "-"
                ), (
                    nameof(AggregateMember.AggregateMemberBase.DeclaringAggregate),
                    member => member.DeclaringAggregate.ToString()
                ), (
                    nameof(AggregateMember.ValueMember.Declared),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.Declared.MemberName
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.ForeignKeyOf),
                    member => member is AggregateMember.ValueMember vm
                        ? (vm.ForeignKeyOf?.MemberName ?? "null")
                        : "-"
                ), (
                    nameof(AggregateMember.RelationMember.Relation),
                    member => member is AggregateMember.RelationMember rel
                        ? rel.Relation.ToString()
                        : "-"
                ), (
                    nameof(AggregateMember.AggregateMemberBase.Order),
                    member => member.Order.ToString()
                ), (
                    nameof(AggregateMember.AggregateMemberBase.CSharpTypeName),
                    member => member.CSharpTypeName
                ), (
                    nameof(AggregateMember.AggregateMemberBase.TypeScriptTypename),
                    member => member.TypeScriptTypename
                ), (
                    nameof(AggregateMember.ValueMember.Options.MemberName),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.Options.MemberName
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.Options.MemberType),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.Options.MemberType.GetType().Name
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.IsKey),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.IsKey.ToString()
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.IsDisplayName),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.IsDisplayName.ToString()
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.IsKeyOfAncestor),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.IsKeyOfAncestor.ToString()
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.IsKeyOfRefTarget),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.IsKeyOfRefTarget.ToString()
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.Options.IsRequired),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.Options.IsRequired.ToString()
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.Options.InvisibleInGui),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.Options.InvisibleInGui.ToString()
                        : "-"
                ),
            };

            builder.AppendLine($"# 集約");
            builder.AppendLine(string.Concat(Enumerable.Repeat("\t", maxIndent + 2)) + columns.Select(c => c.Item1).Join("\t"));

            foreach (var aggregate in allAggregates) {
                var depth = aggregate.EnumerateAncestors().Count();
                var indent1L = string.Concat(Enumerable.Repeat("\t", depth));
                var indent2L = "\t" + indent1L;
                var indent2R = string.Concat(Enumerable.Repeat("\t", maxIndent - depth + 1));
                builder.AppendLine($"{indent1L}{aggregate}");

                foreach (var member in aggregate.GetMembers()) {
                    builder.Append($"{indent2L}{member.MemberName}{indent2R}");
                    builder.AppendLine(columns.Select(c => c.Item2(member)).Join("\t"));
                }
            }

            return builder.ToString();
        }
    }
}
