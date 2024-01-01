using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalCodeAnalyzer {
    partial class Program {
        private static void RenderNeo4jCreateScript(DirectedGraph graph) {
            const string REL_HASCHILD = "HAS_CHILD";
            const string REL_CALLS = "CALLS";
            const string PROP_NAME = "name";

            using var sw = new StreamWriter(@"cypher-script.txt", append: false, encoding: Encoding.UTF8);

            string GetHashedNodeId(NodeId nodeId) => nodeId.Value.ToHashedString();
            string? GetHashedGroupId(NodeGroup group) => group == NodeGroup.Root
                ? null
                : group.FullName.ToHashedString();

            // node
            var namespaces = graph.SubGraphs
                .SelectMany(group => group.Ancestors())
                .ToHashSet();
            var nodes = graph.SubGraphs
                .Select(container => new {
                    id = GetHashedGroupId(container)!,
                    type = namespaces.Contains(container) ? "Namespace" : "Class",
                    name = container.Name,
                })
                .Concat(graph.Nodes.Keys.Select(node => new {
                    id = GetHashedNodeId(node),
                    type = "Method",
                    name = node.BaseName,
                }));
            foreach (var node in nodes) {
                sw.WriteLine($$"""
                    CREATE ({{node.id}}:{{node.type}} {{{PROP_NAME}}:'{{node.name}}'})
                    """);
            }
            // edge(parent-child)
            foreach (var container in graph.SubGraphs) {
                if (container == NodeGroup.Root) continue;
                var parent = GetHashedGroupId(container.Parent);
                var child = GetHashedGroupId(container);
                sw.WriteLine($$"""
                    CREATE ({{parent}})-[:{{REL_HASCHILD}}]->({{child}})
                    """);
            }
            foreach (var node in graph.Nodes.Keys) {
                var parent = GetHashedGroupId(node.Group);
                var child = GetHashedNodeId(node);
                sw.WriteLine($$"""
                    CREATE ({{parent}})-[:{{REL_HASCHILD}}]->({{child}})
                    """);
            }
            // edge(method calling)
            foreach (var edge in graph.Edges) {
                var initial = GetHashedNodeId(edge.Initial);
                var terminal = GetHashedNodeId(edge.Terminal);
                sw.WriteLine($$"""
                    CREATE ({{initial}})-[:{{REL_CALLS}}]->({{terminal}})
                    """);
            }

            Console.WriteLine($$"""
                // 依存関係の収集を完了しました。以下のクエリを実行してください。
                match (m1:Method)-[calling:CALLS]->(m2:Method)
                optional match path1 = (m1)<-[r1:HAS_CHILD*]-()
                optional match path2 = (m2)<-[r2:HAS_CHILD*]-()
                return calling, r1, r2, nodes(path1), nodes(path2)
                """);
        }
    }
}
