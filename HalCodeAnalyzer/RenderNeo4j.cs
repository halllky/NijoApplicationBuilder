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
            const string HAS_CHILD = "HAS_CHILD";
            const string CALLS = "CALLS";

            using var sw = new StreamWriter(@"cypher-script.txt", append: false, encoding: Encoding.UTF8);

            string GetHashedGroupId(NodeGroup group) => group.FullName.ToHashedString();
            string GetHashedNodeId(NodeId nodeId) => nodeId.Value.ToHashedString();

            // node
            foreach (var container in graph.SubGraphs) {
                sw.WriteLine($$"""
                    CREATE ({{GetHashedGroupId(container)}}:ClassOrNamespace {name:'{{container.Name}}'})
                    """);
            }
            foreach (var node in graph.Nodes.Keys) {
                sw.WriteLine($$"""
                    CREATE ({{GetHashedNodeId(node)}}:Method {name:'{{node.BaseName}}'})
                    """);
            }
            // edge(parent-child)
            foreach (var container in graph.SubGraphs) {
                if (container == NodeGroup.Root) continue;
                var parent = GetHashedGroupId(container.Parent);
                var child = GetHashedGroupId(container);
                sw.WriteLine($$"""
                    CREATE ({{parent}})-[:{{HAS_CHILD}}]->({{child}})
                    """);
            }
            foreach (var node in graph.Nodes.Keys) {
                var parent = GetHashedGroupId(node.Group);
                var child = GetHashedNodeId(node);
                sw.WriteLine($$"""
                    CREATE ({{parent}})-[:{{HAS_CHILD}}]->({{child}})
                    """);
            }
            // edge(method calling)
            foreach (var edge in graph.Edges) {
                var initial = GetHashedNodeId(edge.Initial);
                var terminal = GetHashedNodeId(edge.Terminal);
                sw.WriteLine($$"""
                    CREATE ({{initial}})-[:{{CALLS}}]->({{terminal}})
                    """);
            }
        }
    }
}
