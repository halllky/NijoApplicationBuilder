using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core {
    public class AppSchema {
        internal static AppSchema Empty() => new(string.Empty, DirectedGraph.Empty(), new HashSet<EnumDefinition>(), new HashSet<DynamicEnumTypeInfo>());

        internal AppSchema(string appName, DirectedGraph directedGraph, IReadOnlyCollection<EnumDefinition> enumDefinitions, IReadOnlyCollection<DynamicEnumTypeInfo> dynamicEnumTypeInfo) {
            ApplicationName = appName;
            Graph = directedGraph;
            EnumDefinitions = enumDefinitions;
            DynamicEnumTypeInfo = dynamicEnumTypeInfo;
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

        internal IReadOnlyCollection<DynamicEnumTypeInfo> DynamicEnumTypeInfo { get; }

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
                    nameof(AggregateMember.ValueMember.Inherits),
                    member => member is AggregateMember.ValueMember vm
                        ? (vm.Inherits?.Member.MemberName ?? "null")
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
                    nameof(AggregateMember.RelationMember.Relation),
                    member => member is AggregateMember.RelationMember rel
                        ? rel.Relation.ToString()
                        : "-"
                ), (
                    nameof(AggregateMember.AggregateMemberBase.Order),
                    member => member.Order.ToString()
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
                    nameof(AggregateMember.ValueMember.IsRequired),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.IsRequired.ToString()
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
                builder.AppendLine($"{indent1L}{aggregate.Item.DisplayName}({aggregate.Item.Options.Handler})");

                foreach (var member in aggregate.GetMembers()) {
                    builder.Append($"{indent2L}{member.MemberName}{indent2R}");
                    builder.AppendLine(columns.Select(c => c.Item2(member)).Join("\t"));
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// mermaid.js 記法でスキーマ定義を出力します。
        /// </summary>
        public string ToMermaidText() {
            var builder = new StringBuilder();
            builder.AppendLine("graph LR;");

            foreach (var edge in Graph.Edges) {
                var id1 = edge.Initial.Value.ToHashedString();
                var id2 = edge.Terminal.Value.ToHashedString();
                var label1 = edge.Initial.Value.Replace("\"", "");
                var label2 = edge.Terminal.Value.Replace("\"", "");

                // 分かりやすさのため、親子は関係性を、Refはリレーション名を表示
                string relation;
                if (edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, out var type)
                    && (string)type! == DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD) {

                    if (edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_MULTIPLE, out var isArray) && (bool)isArray!) {
                        relation = "Children";

                    } else if (edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUPNAME, out var groupName)
                        && (string)groupName! != string.Empty) {
                        relation = "Variation";

                    } else {
                        relation = "Child";
                    }

                } else {
                    relation = edge.RelationName.Replace("\"", "");
                }

                builder.AppendLine($"  {id1}(\"{label1}\") --\"{relation}\"--> {id2}(\"{label2}\");");
            }

            // ルート集約のくくりごとにsubgraphにまとめる
            foreach (var root in RootAggregates()) {
                var aggregates = root
                    .EnumerateThisAndDescendants();
                var schalarMembers = root
                    .EnumerateThisAndDescendants()
                    .SelectMany(agg => agg.GetMembers())
                    .OfType<AggregateMember.Schalar>();

                string type;
                if (root.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel2.Key) {
                    type = root.Item.Options.GenerateDefaultReadModel
                        ? "(Write/Read Model)"
                        : "(Write Model)";
                } else if (root.Item.Options.Handler == NijoCodeGenerator.Models.ReadModel2.Key) {
                    type = "(Read Model)";
                } else {
                    type = string.Empty;
                }

                builder.AppendLine($$"""
                      subgraph "{{root.Item.PhysicalName}}{{type}}"
                    {{aggregates.SelectTextTemplate(agg => $$"""
                        {{agg.Item.Id.ToString().ToHashedString()}}
                    """)}}
                    {{schalarMembers.SelectTextTemplate(schalar => $$"""
                        {{schalar.GraphNode.Item.Id.ToString().ToHashedString()}}
                    """)}}
                      end
                    """);
            }

            return builder.ToString();
        }
    }
}
