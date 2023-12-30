using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.DotnetEx {
    /// <summary>
    /// 有向グラフ
    /// </summary>
    public class DirectedGraph : IEnumerable<GraphNode> {

        public static DirectedGraph Empty() {
            return new Util.DotnetEx.DirectedGraph(Enumerable.Empty<IGraphNode>(), new HashSet<GraphEdgeInfo>());
        }

        /// <summary>
        /// 新しい有向グラフを作成します。
        /// </summary>
        /// <param name="nodes">頂点</param>
        /// <param name="edges">辺</param>
        /// <param name="graph">作成されたグラフ</param>
        /// <param name="errors">グラフが作成できなかった場合、その理由の一覧</param>
        /// <returns>グラフを作成できたか否か</returns>
        public static bool TryCreate(
            IEnumerable<IGraphNode> nodes,
            IEnumerable<GraphEdgeInfo> edges,
            out DirectedGraph graph,
            out ICollection<string> errors) {

            errors = new HashSet<string>();

            var duplicates = nodes
                .GroupBy(node => node.Id)
                .Where(group => group.Count() >= 2)
                .ToArray();
            foreach (var dup in duplicates) {
                errors.Add($"Nodes duplicates: {dup.Key}");
            }

            var nodeIds = nodes.Select(node => node.Id).ToHashSet();
            var initialNodeNotExists = edges
                .Where(edge => !nodeIds.Contains(edge.Initial))
                .Select(edge => edge.Initial);
            var terminalNodeNotExists = edges
                .Where(edge => !nodeIds.Contains(edge.Terminal))
                .Select(edge => edge.Terminal);
            var nodeNotExists = initialNodeNotExists.Union(terminalNodeNotExists);
            foreach (var nodeId in nodeNotExists) {
                errors.Add($"Node '{nodeId}' is not exists.");
            }

            if (errors.Any()) {
                graph = Empty();
                return false;
            }

            graph = new DirectedGraph(nodes.ToHashSet(), edges.ToHashSet());
            return true;
        }

        /// <summary>
        /// 新しい有向グラフを作成します。エラーが出ないとわかっている場合はこちらを使用
        /// </summary>
        /// <param name="nodes">頂点</param>
        /// <param name="edges">辺</param>
        /// <returns>作成されたグラフ</returns>
        public static DirectedGraph Create(IEnumerable<IGraphNode> nodes, IEnumerable<GraphEdgeInfo> edges) {
            if (!TryCreate(nodes, edges, out var graph, out var errors)) {
                throw new InvalidOperationException($"Error occured when new directed graph is created:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
            }
            return graph;
        }

        private DirectedGraph(IEnumerable<IGraphNode> nodes, IReadOnlySet<GraphEdgeInfo> edges) {
            Nodes = nodes.ToDictionary(n => n.Id, n => n);
            Edges = edges;
        }

        public IReadOnlyDictionary<NodeId, IGraphNode> Nodes { get; }
        public IReadOnlySet<GraphEdgeInfo> Edges { get; }

        public IEnumerable<GraphNode<T>> Only<T>() where T : IGraphNode {
            return this
                .Where(node => node.Item is T)
                .Select(node => node.As<T>());
        }

        public string ToMermaidText() {
            var builder = new StringBuilder();
            builder.AppendLine("graph TD;");

            foreach (var edge in Edges) {
                var id1 = edge.Initial.Value.ToHashedString();
                var id2 = edge.Terminal.Value.ToHashedString();
                var label1 = edge.Initial.Value.Replace("\"", "");
                var label2 = edge.Terminal.Value.Replace("\"", "");
                var relation = edge.RelationName.Replace("\"", "");
                builder.AppendLine($"  {id1}(\"{label1}\") --\"{relation}\"--> {id2}(\"{label2}\");");
            }

            return builder.ToString();
        }
        public string ToDotText() {
            var builder = new StringBuilder();
            builder.AppendLine("digraph G {");
            foreach (var node in Nodes) {
                builder.AppendLine($"  {node.Key.Value.ToHashedString()} [label=\"{node.Key.Value.Replace("\"", "”")}\"];");
            }
            foreach (var edge in Edges) {
                var initial = edge.Initial.Value.ToHashedString();
                var terminal = edge.Terminal.Value.ToHashedString();
                builder.AppendLine($"  {initial} -> {terminal} [label=\"{edge.RelationName.Replace("\"", "”")}\"]");
            }

            builder.AppendLine("}");
            return builder.ToString();
        }

        public IEnumerator<GraphNode> GetEnumerator() {
            foreach (var node in Nodes.Values) {
                yield return new GraphNode(node, this, null);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }


    #region COMPUTED
    /// <summary>
    /// 有向グラフの頂点
    /// </summary>
    public class GraphNode : ValueObject {
        public GraphNode(IGraphNode item, DirectedGraph graph, GraphEdge? source) {
            _graph = graph;
            Item = item;
            Source = source;
        }
        protected readonly DirectedGraph _graph;

        public IGraphNode Item { get; }

        private ICollection<GraphEdge>? _out;
        private ICollection<GraphEdge>? _in;

        /// <summary>
        /// この頂点から出て行く辺の一覧
        /// </summary>
        public IEnumerable<GraphEdge> Out {
            get {
                _out ??= _graph.Edges
                    .Where(edgeInfo => edgeInfo.Initial == Item.Id)
                    .Select(GoToNeighborEdge)
                    .ToArray();
                return _out;
            }
        }
        /// <summary>
        /// この頂点に入る辺の一覧
        /// </summary>
        public IEnumerable<GraphEdge> In {
            get {
                _in ??= _graph.Edges
                    .Where(edgeInfo => edgeInfo.Terminal == Item.Id)
                    .Select(GoToNeighborEdge)
                    .ToArray();
                return _in;
            }
        }

        private GraphEdge GoToNeighborEdge(GraphEdgeInfo edgeInfo) {
            var newEdge = new GraphEdge(edgeInfo, _graph, this);
            if (newEdge == Source) return Source;
            return newEdge;
        }

        /// <summary>
        /// この頂点がどの経路を辿って生成されたか。Entryの最初の頂点の場合はnull
        /// </summary>
        public GraphEdge? Source { get; }

        /// <summary>
        /// エントリーからの辺の一覧を返します。よりエントリーに近いほうから順番に返します。
        /// </summary>
        public GraphPath PathFromEntry() {
            var list = new List<GraphEdge>();
            var node = this;
            while (true) {
                if (node.Source == null) break;
                list.Add(node.Source);
                node = node.Source.Source;
            }
            list.Reverse();
            return new GraphPath(list);
        }

        public GraphNode GetEntry() {
            return PathFromEntry().FirstOrDefault()?.Source ?? this;
        }

        public GraphNode AsEntry() {
            return new GraphNode(Item, _graph, null);
        }

        public GraphNode<T> As<T>() where T : IGraphNode {
            return this is GraphNode<T> t
                ? t
                : new GraphNode<T>((T)Item, _graph, Source);
        }

        public override string ToString() => $"GraphNode[{Item.Id}]";

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return _graph;
            yield return Item.Id;
        }
    }
    /// <summary>
    /// 有向グラフの辺
    /// </summary>
    public class GraphEdge : ValueObject {
        public GraphEdge(GraphEdgeInfo info, DirectedGraph graph, GraphNode source) {
            _graph = graph;
            _info = info;
            Source = source;
        }
        private readonly GraphEdgeInfo _info;
        private readonly DirectedGraph _graph;

        public string RelationName => _info.RelationName;
        public IReadOnlyDictionary<string, object> Attributes => _info.Attributes;

        /// <summary>
        /// 辺の始点ではなくこの辺がどこから辿ってきて生成されたか
        /// </summary>
        public GraphNode Source { get; }

        private GraphNode? _initial;
        private GraphNode? _terminal;
        /// <summary>
        /// 辺の始点
        /// </summary>
        public GraphNode Initial => _initial ??= GoToNeighborNode(_info.Initial);
        /// <summary>
        /// 辺の終点
        /// </summary>
        public GraphNode Terminal => _terminal ??= GoToNeighborNode(_info.Terminal);

        private GraphNode GoToNeighborNode(NodeId nodeId) {
            var newNode = new GraphNode(_graph.Nodes[nodeId], _graph, this);
            if (newNode == Source) return Source;
            return newNode;
        }

        public GraphEdge<T> As<T>() where T : IGraphNode {
            return new GraphEdge<T>(_info, _graph, Source);
        }

        public override string ToString() => $"{_info.Initial} == {_info.RelationName} ==> {_info.Terminal}";

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return _info;
            yield return _graph;
        }
    }

    public class GraphNode<T> : GraphNode where T : IGraphNode {
        public GraphNode(T item, DirectedGraph graph, GraphEdge? source)
            : base(item, graph, source) { }

        public new GraphNode<T> AsEntry() {
            return new GraphNode<T>(Item, _graph, null);
        }

        public new T Item => (T)base.Item;
    }
    public class GraphEdge<T> : GraphEdge where T : IGraphNode {
        public GraphEdge(GraphEdgeInfo info, DirectedGraph graph, GraphNode source) : base(info, graph, source) {
        }

        public new GraphNode<T> Initial => base.Initial.As<T>();
        public new GraphNode<T> Terminal => base.Terminal.As<T>();
    }

    /// <summary>
    /// 同じ経路を同じオブジェクトと判定してDistinctやHashSetに使いたいためのクラス
    /// </summary>
    public class GraphPath : ValueObject, IEnumerable<GraphEdge> {
        public GraphPath(IReadOnlyList<GraphEdge> edges) {
            _edges = edges;
        }
        private readonly IReadOnlyList<GraphEdge> _edges;

        /// <summary>
        /// 指定のノード以降の区間のみを切り出す
        /// </summary>
        public GraphPath Since(GraphNode node) {
            var skip = true;
            var list = new List<GraphEdge>();
            foreach (var edge in _edges) {
                if (skip && edge.Source == node) skip = false;
                if (!skip) list.Add(edge);
            }
            return new GraphPath(list);
        }
        /// <summary>
        /// 指定のノード以前の区間のみを切り出す
        /// </summary>
        public GraphPath Until(GraphNode node) {
            var list = new List<GraphEdge>();
            foreach (var edge in _edges) {
                if (edge.Source == node) break;
                list.Add(edge);
            }
            return new GraphPath(list);
        }

        protected override IEnumerable<object?> ValueObjectIdentifiers() => _edges;
        public IEnumerator<GraphEdge> GetEnumerator() => _edges.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    #endregion COMPUTED


    #region VALUE
    public class NodeId : ValueObject {
        public NodeId(string value) {
            Value = value;
        }
        public string Value { get; }

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return Value;
        }
        public override string ToString() {
            return Value;
        }

        public static NodeId Empty => new NodeId(string.Empty);
    }
    public interface IGraphNode {
        NodeId Id { get; }
    }
    public sealed class GraphEdgeInfo {
        public required NodeId Initial { get; init; }
        public required NodeId Terminal { get; init; }
        public required string RelationName { get; init; }
        public IReadOnlyDictionary<string, object> Attributes { get; init; } = new Dictionary<string, object>();
    }
    #endregion VALUE


    public static class DirectedGraphExtensions {
        public static IEnumerable<GraphNode<T>> SelectNeighbors<T>(this GraphNode<T> graphNode, Func<GraphNode<T>, IEnumerable<GraphNode<T>>> predicate) where T : IGraphNode {
            foreach (var item in predicate(graphNode)) {
                yield return item;

                foreach (var item2 in SelectNeighbors(item, predicate)) {
                    yield return item2;
                }
            }
        }
        public static IEnumerable<GraphNode<T>> SelectThisAndNeighbors<T>(this GraphNode<T> graphNode, Func<GraphNode<T>, IEnumerable<GraphNode<T>>> predicate) where T : IGraphNode {
            yield return graphNode;
            foreach (var item in graphNode.SelectNeighbors(predicate)) {
                yield return item;
            }
        }

    }
}
