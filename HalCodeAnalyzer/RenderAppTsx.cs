using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalCodeAnalyzer {
    partial class Program {
        static void RenderAppTsx(DirectedGraph graph) {
            if (!File.Exists(Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(),
                @"../../../../HalCodeAnalyzer.Viewer/src/App.tsx")))) throw new InvalidOperationException("Invalid path.");

            var filepath = Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(),
                @"../../../../HalCodeAnalyzer.Viewer/src/data.ts"));
            using var sw = new StreamWriter(filepath, append: false, encoding: new UTF8Encoding(false, false));

            var containers = graph.SubGraphs
                .Where(group => group.Depth >= 2) // ルート名前空間は描画しない。
                                                  // 全ノードがルート名前空間のノードの中に描画されてしまい
                                                  // スワイプしたときに画面スクロールでなくルート名前空間のドラッグになってしまうため
                .Select(group => new { id = ToIdStringOrUndefined(group), group })
                .OrderBy(group => group.id);
            var nodes = graph.Nodes
                .Select(node => new { id = node.Key.Value.ToHashedString(), node = node.Key })
                .OrderBy(x => x.id);
            var edges = graph.Edges
                .Select(edge => new { id = $"{edge.Initial}::{edge.RelationName}::{edge.Terminal}".ToHashedString(), edge })
                .OrderBy(x => x.id);

            sw.WriteLine($$$"""
                import cytoscape from 'cytoscape'

                export default (): cytoscape.ElementDefinition[] => [
                  // containers
                {{{containers.Select(x => $$"""
                  { data: { id: {{x.id}}, label: '{{x.group.Name}}', parent: {{ToIdStringOrUndefined(x.group.Parent)}} } },
                """).Join(Environment.NewLine)}}}

                  // nodes
                {{{nodes.Select(x => $$"""
                  { data: { id: '{{x.id}}', label: '{{x.node.BaseName}}', parent: {{ToIdStringOrUndefined(x.node.Group)}} } },
                """).Join(Environment.NewLine)}}}

                  // edges
                {{{edges.Select(x => $$"""
                  { data: { id: '{{x.id}}', label: '{{x.edge.RelationName}}', source: '{{x.edge.Initial.Value.ToHashedString()}}', target: '{{x.edge.Terminal.Value.ToHashedString()}}' } },
                """).Join(Environment.NewLine)}}}
                ]
                """.Replace(Environment.NewLine, "\n"));
        }

        private static string ToIdStringOrUndefined(NodeGroup group) {
            return group == NodeGroup.Root
                ? $"undefined"
                : $"'{group.FullName.ToHashedString()}'";
        }
    }
}
