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
    internal class DirectedGraph : IEnumerable<GraphNode> {

        internal static DirectedGraph Empty() {
            return new DotnetEx.DirectedGraph(Enumerable.Empty<IGraphNode>(), new HashSet<GraphEdgeInfo>());
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
        internal static DirectedGraph Create(IEnumerable<IGraphNode> nodes, IEnumerable<GraphEdgeInfo> edges) {
            if (!TryCreate(nodes, edges, out var graph, out var errors)) {
                throw new InvalidOperationException($"Error occured when new directed graph is created:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
            }
            return graph;
        }

        private DirectedGraph(IEnumerable<IGraphNode> nodes, IReadOnlySet<GraphEdgeInfo> edges) {
            Nodes = nodes.ToDictionary(n => n.Id, n => n);
            Edges = edges;
        }

        internal IReadOnlyDictionary<NodeId, IGraphNode> Nodes { get; }
        internal IReadOnlySet<GraphEdgeInfo> Edges { get; }

        internal IEnumerable<GraphNode<T>> Only<T>() where T : IGraphNode {
            return this
                .Where(node => node.Item is T)
                .Select(node => node.As<T>());
        }

        public string ToMermaidText() {
            var builder = new StringBuilder();
            builder.AppendLine("graph TD;");

            foreach (var edge in Edges) {
                var id1 = new HashedString(edge.Initial.Value).Guid.ToString();
                var id2 = new HashedString(edge.Terminal.Value).Guid.ToString();
                var label1 = edge.Initial.Value.Replace("\"", "");
                var label2 = edge.Terminal.Value.Replace("\"", "");
                var relation = edge.RelationName.Replace("\"", "");
                builder.AppendLine($"  {id1}(\"{label1}\") --\"{relation}\"--> {id2}(\"{label2}\");");
            }

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
    internal class GraphNode : ValueObject {
        internal GraphNode(IGraphNode item, DirectedGraph graph, GraphEdge? source) {
            _graph = graph;
            Item = item;
            Source = source;
        }
        protected readonly DirectedGraph _graph;

        internal IGraphNode Item { get; }

        private ICollection<GraphEdge>? _out;
        private ICollection<GraphEdge>? _in;

        /// <summary>
        /// この頂点から出て行く辺の一覧
        /// </summary>
        internal IEnumerable<GraphEdge> Out {
            get {
                _out ??= _graph.Edges
                    .Where(edgeInfo => edgeInfo.Initial == Item.Id)
                    .Select(edgeInfo => new GraphEdge(edgeInfo, _graph, this))
                    .ToArray();
                return _out;
            }
        }
        /// <summary>
        /// この頂点に入る辺の一覧
        /// </summary>
        internal IEnumerable<GraphEdge> In {
            get {
                _in ??= _graph.Edges
                    .Where(edgeInfo => edgeInfo.Terminal == Item.Id)
                    .Select(edgeInfo => new GraphEdge(edgeInfo, _graph, this))
                    .ToArray();
                return _in;
            }
        }
        /// <summary>
        /// <see cref="In"/> + <see cref="Out"/>
        /// </summary>
        internal IEnumerable<GraphEdge> InAndOut => In.Union(Out);

        /// <summary>
        /// この頂点がどの経路を辿って生成されたか。Entryの最初の頂点の場合はnull
        /// </summary>
        internal GraphEdge? Source { get; }
        /// <summary>
        /// ここまで辿ってきた経路をリセットした新しいインスタンスを返します。
        /// </summary>
        internal GraphNode AsEntry() {
            return new GraphNode(Item, _graph, null);
        }

        /// <summary>
        /// エントリーからの辺の一覧を返します。
        /// </summary>
        internal IEnumerable<GraphEdge> PathFromEntry() {
            var list = new List<GraphEdge>();
            var node = this;
            while (true) {
                if (node.Source == null) break;
                list.Add(node.Source);
                node = node.Source.Source;
            }
            list.Reverse();
            return list;
        }

        internal GraphNode<T> As<T>() where T : IGraphNode {
            return new GraphNode<T>((T)Item, _graph, Source);
        }

        public override string ToString() => $"GraphNode[{Item.Id}]";

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return _graph;
            yield return Item.Id;
        }
    }
    internal class GraphNode<T> : GraphNode where T : IGraphNode {
        internal GraphNode(T item, DirectedGraph graph, GraphEdge? source)
            : base(item, graph, source) { }

        internal new T Item => (T)base.Item;

        internal new GraphNode<T> AsEntry() => base.AsEntry().As<T>();
    }

    /// <summary>
    /// 有向グラフの辺
    /// </summary>
    internal class GraphEdge : ValueObject {
        internal GraphEdge(GraphEdgeInfo info, DirectedGraph graph, GraphNode source) {
            _graph = graph;
            _info = info;
            Source = source;
        }
        private readonly GraphEdgeInfo _info;
        private readonly DirectedGraph _graph;

        internal string RelationName => _info.RelationName;
        internal IReadOnlyDictionary<string, object> Attributes => _info.Attributes;

        /// <summary>
        /// 辺の始点ではなくこの辺がどこから辿ってきて生成されたか
        /// </summary>
        internal GraphNode Source { get; }

        private GraphNode? _initial;
        private GraphNode? _terminal;
        /// <summary>
        /// 辺の始点
        /// </summary>
        internal GraphNode Initial {
            get {
                _initial ??= new GraphNode(_graph.Nodes[_info.Initial], _graph, this);
                return _initial;
            }
        }
        /// <summary>
        /// 辺の終点
        /// </summary>
        internal GraphNode Terminal {
            get {
                _terminal ??= new GraphNode(_graph.Nodes[_info.Terminal], _graph, this);
                return _terminal;
            }
        }

        public override string ToString() => $"{_info.Initial} == {_info.RelationName} ==> {_info.Terminal}";

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return _info;
            yield return _graph;
        }
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
    internal sealed class GraphEdgeInfo {
        internal required NodeId Initial { get; init; }
        internal required NodeId Terminal { get; init; }
        internal required string RelationName { get; init; }
        internal IReadOnlyDictionary<string, object> Attributes { get; init; } = new Dictionary<string, object>();
    }
    #endregion VALUE
}
