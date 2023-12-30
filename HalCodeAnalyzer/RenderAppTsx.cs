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

            sw.WriteLine($$$"""
                import cytoscape from 'cytoscape'

                export default (): cytoscape.ElementDefinition[] => [
                  // containers
                {{{graph.SubGraphs.Select(group => $$"""
                  { data: { id: '{{group.FullName.ToHashedString()}}', label: '{{group.Name}}', parent: {{(group.Parent == NodeGroup.Root ? "undefined" : $"'{group.Parent.FullName.ToHashedString()}'")}} } },
                """).Join(Environment.NewLine)}}}

                  // nodes
                {{{graph.Nodes.Select(node => $$"""
                  { data: { id: '{{node.Key.Value.ToHashedString()}}', label: '{{node.Key.BaseName}}', parent: {{(node.Key.Group == NodeGroup.Root ? "undefined" : $"'{node.Key.Group.FullName.ToHashedString()}'")}} } },
                """).Join(Environment.NewLine)}}}

                  // edges
                {{{graph.Edges.Select(edge => $$"""
                  { data: { id: '{{$"{edge.Initial}::{edge.RelationName}::{edge.Terminal}".ToHashedString()}}', label: '{{edge.RelationName}}', source: '{{edge.Initial.Value.ToHashedString()}}', target: '{{edge.Terminal.Value.ToHashedString()}}' } },
                """).Join(Environment.NewLine)}}}
                ]
                """.Replace(Environment.NewLine, "\n"));
        }
    }
}
