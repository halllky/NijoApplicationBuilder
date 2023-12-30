using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Nijo.Util.DotnetEx;

namespace HalCodeAnalyzer {
    partial class Program {

        static async Task Main(string[] args) {
            // コマンドライン引数から csproj ファイルのパスを取得する
            var projectPath = args[0];
            if (!File.Exists(projectPath) || !projectPath.EndsWith(".csproj")) {
                Console.WriteLine("Invalid project path.");
                return;
            }

            using var workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId();
            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                "MyProject",
                "MyProject.dll",
                LanguageNames.CSharp);

            // Add all C# files in the specified folder to the project
            var folderPath = Path.GetDirectoryName(projectPath) ?? throw new InvalidOperationException($"Failed to get directory name.");
            var filePaths = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
            var docList = new List<DocumentInfo>();
            foreach (var filePath in filePaths) {
                var documentInfo = DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId),
                    Path.GetFileName(filePath),
                    loader: TextLoader.From(TextAndVersion.Create(
                        SourceText.From(File.ReadAllText(filePath)),
                        VersionStamp.Create())),
                    sourceCodeKind: SourceCodeKind.Regular,
                    filePath: filePath);
                docList.Add(documentInfo);
            }

            var solution = workspace.CurrentSolution
                .AddProject(projectInfo)
                .AddDocuments(docList.ToImmutableArray());
            var project = solution.GetProject(projectId) ?? throw new Exception("Failed to get project.");
            var compilation = await project.GetCompilationAsync() ?? throw new InvalidOperationException("Failed to compile.");

            using var sw = new StreamWriter(@"aaaaa.txt", append: false, encoding: Encoding.UTF8);
            var walker = new ClassDependencyCollector { _compilation = compilation };
            walker.OnLogout += (_, str) => {
                sw.WriteLine(str);
            };

            var nodes = new HashSet<NodeId>();
            var edges = new List<GraphEdgeInfo>();
            walker.AddGraphNode += (_, nodeId) => nodes.Add(nodeId);
            walker.AddGraphEdge += (_, edge) => edges.Add(edge);

            foreach (var syntaxTree in compilation.SyntaxTrees) {
                walker.Visit(syntaxTree.GetRoot());
            }

            var graph = DirectedGraph.Create(
                nodes.Select(id => new SimpleGraphNode { Id = id }),
                edges.DistinctBy(edge => (edge.Initial, edge.RelationName, edge.Terminal))
                     .Where(edge => edge.Initial != edge.Terminal));

            //// mermaid.js に出力
            //sw.WriteLine(graph.ToMermaidText());
            //Console.WriteLine("mermaid.js CLI をインストールしたうえで下記コマンドを実行のこと。");
            //Console.WriteLine("mmdc -i aaaaa.txt -o aaaaa.svg");
            //// 出力は可能だが要素数が多すぎて見づらい

            // Graphviz(Dot言語)で出力
            sw.WriteLine(graph.ToDotText());

            //// QuickGraphで出力（Sandwych.QuickGraph.Core）
            //var graph = new BidirectionalGraph<object, IEdge<object>>();
            //graph.AddVertexRange(nodes.Select(nodeId => nodeId.Value));
            //graph.AddEdgeRange(edges
            //    .DistinctBy(edge => (edge.Initial, edge.Terminal))
            //    .Select(edge => new Edge<object>(edge.Initial.Value, edge.Terminal.Value)));
            //// ここから先svgまで持っていく方法がない

            // cytoscapeで表示
            RenderAppTsx(graph);
        }

        private class SimpleGraphNode : IGraphNode {
            public required NodeId Id { get; init; }
        }
    }
}
