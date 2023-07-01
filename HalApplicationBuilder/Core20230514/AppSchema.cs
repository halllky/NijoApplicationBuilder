using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514 {
    internal class AppSchema {
        internal static AppSchema Empty() => new(string.Empty, DirectedGraph.Empty());

        internal AppSchema(string appName, DirectedGraph directedGraph) {
            ApplicationName = appName;
            _graph = directedGraph;
        }

        private readonly DirectedGraph _graph;

        public object ApplicationName { get; }

        internal IEnumerable<GraphNode<Aggregate>> AllAggregates() {
            return _graph.Only<Aggregate>();
        }
        internal IEnumerable<GraphNode<Aggregate>> RootAggregates() {
            return AllAggregates().Where(aggregate => aggregate.IsRoot());
        }

        internal IEnumerable<GraphNode<EFCoreEntity>> ToEFCoreGraph() {
            return _graph.Only<EFCoreEntity>();
        }

        internal IEnumerable<GraphNode<AggregateInstance>> ToAggregateInstanceGraph() {
            return _graph.Only<AggregateInstance>();
        }
    }
}
