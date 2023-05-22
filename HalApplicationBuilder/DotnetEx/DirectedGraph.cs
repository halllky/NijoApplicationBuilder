using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.DotnetEx {
    /// <summary>
    /// 有向グラフ
    /// </summary>
    internal class DirectedGraph<T> : IEnumerable<GraphNode<T>> where T : IGraphNode {

        internal static DirectedGraph<T> Empty() {
            return new DotnetEx.DirectedGraph<T>(Enumerable.Empty<T>(), new HashSet<GraphEdgeInfo>());
        }

        /// <summary>
        /// 新しい有向グラフを作成します。
        /// </summary>
        /// <param name="nodes">頂点</param>
        /// <param name="edges">辺</param>
        /// <param name="graph">作成されたグラフ</param>
        /// <param name="errors">グラフが作成できなかった場合、その理由の一覧</param>
        /// <returns>グラフを作成できたか否か</returns>
        internal static bool TryCreate(
            IEnumerable<T> nodes,
            IEnumerable<GraphEdgeInfo> edges,
            out DirectedGraph<T> graph,
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
                graph = new DirectedGraph<T>(new HashSet<T>(), new HashSet<GraphEdgeInfo>());
                return false;
            }

            graph = new DirectedGraph<T>(nodes.ToHashSet(), edges.ToHashSet());
            return true;
        }

        /// <summary>
        /// 新しい有向グラフを作成します。エラーが出ないとわかっている場合はこちらを使用
        /// </summary>
        /// <param name="nodes">頂点</param>
        /// <param name="edges">辺</param>
        /// <returns>作成されたグラフ</returns>
        internal static DirectedGraph<T> Create(IEnumerable<T> nodes, IEnumerable<GraphEdgeInfo> edges) {
            if (!TryCreate(nodes,edges, out var graph, out var errors)) {
                throw new InvalidOperationException($"Error occured when new directed graph is created:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
            }
            return graph;
        }

        private DirectedGraph(IEnumerable<T> nodes, IReadOnlySet<GraphEdgeInfo> edges) {
            Items = nodes.ToDictionary(n => n.Id, n => n);
            Edges = edges;
        }

        internal IReadOnlyDictionary<NodeId, T> Items { get; }
        internal IReadOnlySet<GraphEdgeInfo> Edges { get; }

        public IEnumerator<GraphNode<T>> GetEnumerator() {
            foreach (var node in Items.Values) {
                yield return new GraphNode<T>(node, this);
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
    internal class GraphNode<T> where T : IGraphNode {
        internal GraphNode(T item, DirectedGraph<T> graph) {
            _graph = graph;
            Item = item;
        }
        protected readonly DirectedGraph<T> _graph;

        internal T Item { get; }

        private ICollection<GraphEdge<T>>? _out;
        private ICollection<GraphEdge<T>>? _in;

        /// <summary>
        /// この頂点から出て行く辺の一覧
        /// </summary>
        internal IEnumerable<GraphEdge<T>> Out {
            get {
                _out ??= _graph.Edges
                    .Where(edgeInfo => edgeInfo.Initial == Item.Id)
                    .Select(edgeInfo => new GraphEdge<T>(edgeInfo, _graph, this))
                    .ToArray();
                return _out;
            }
        }
        /// <summary>
        /// この頂点に入る辺の一覧
        /// </summary>
        internal IEnumerable<GraphEdge<T>> In {
            get {
                _in ??= _graph.Edges
                    .Where(edgeInfo => edgeInfo.Terminal == Item.Id)
                    .Select(edgeInfo => new GraphEdge<T>(edgeInfo, _graph, this))
                    .ToArray();
                return _in;
            }
        }
        /// <summary>
        /// <see cref="In"/> + <see cref="Out"/>
        /// </summary>
        internal IEnumerable<GraphEdge<T>> InAndOut => In.Union(Out);

        public override string ToString() => $"GraphNode[{Item.Id}]";
    }
    /// <summary>
    /// 隣接ノード
    /// </summary>
    internal class NeighborNode<T> : GraphNode<T> where T : IGraphNode {
        internal NeighborNode(T item, DirectedGraph<T> graph, GraphEdge<T> source) : base(item, graph) {
            Source = source;
        }
        internal GraphEdge<T> Source { get; }

        /// <summary>
        /// エントリーからの辺の一覧を返します。
        /// </summary>
        internal IEnumerable<GraphEdge<T>> PathFromEntry() {
            var list = new List<GraphEdge<T>>();
            var node = (GraphNode<T>)this;
            while (true) {
                if (node is not NeighborNode<T> neighborNode) break;
                list.Add(neighborNode.Source);
                node = neighborNode.Source.Source;
            }
            list.Reverse();
            return list;
        }
    }

    /// <summary>
    /// 有向グラフの辺
    /// </summary>
    internal class GraphEdge<T> where T : IGraphNode {
        internal GraphEdge(GraphEdgeInfo info, DirectedGraph<T> graph, GraphNode<T> source) {
            _graph = graph;
            _info = info;
            Source = source;
        }
        private readonly GraphEdgeInfo _info;
        private readonly DirectedGraph<T> _graph;

        internal string RelationName => _info.RelationName;
        internal IReadOnlyDictionary<string, object> Attributes => _info.Attributes;

        /// <summary>
        /// 辺の始点ではなくこの辺がどこから辿ってきて生成されたか
        /// </summary>
        internal GraphNode<T> Source { get; }

        private GraphNode<T>? _initial;
        private GraphNode<T>? _terminal;
        /// <summary>
        /// 辺の始点
        /// </summary>
        internal GraphNode<T> Initial {
            get {
                _initial ??= new NeighborNode<T>(_graph.Items[_info.Initial], _graph, this);
                return _initial;
            }
        }
        /// <summary>
        /// 辺の終点
        /// </summary>
        internal GraphNode<T> Terminal {
            get {
                _terminal ??= new NeighborNode<T>(_graph.Items[_info.Terminal], _graph, this);
                return _terminal;
            }
        }

        public override string ToString() => $"{_info.Initial} == {_info.RelationName} ==> {_info.Terminal}";
    }
    #endregion COMPUTED


    #region VALUE
    internal class NodeId : ValueObject {
        internal NodeId(string value) {
            Value = value;
        }
        internal string Value { get; }

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return Value;
        }
        public override string ToString() {
            return Value;
        }
    }
    internal interface IGraphNode {
        NodeId Id { get; }
    }
    internal sealed class GraphEdgeInfo : ValueObject {
        internal required NodeId Initial { get; init; }
        internal required NodeId Terminal { get; init; }
        internal required string RelationName { get; init; }
        internal IReadOnlyDictionary<string, object> Attributes { get; init; } = new Dictionary<string, object>();

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return Initial;
            yield return Terminal;
            yield return RelationName;
        }
    }
    #endregion VALUE
}
