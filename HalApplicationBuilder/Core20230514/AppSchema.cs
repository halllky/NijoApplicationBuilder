using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514 {
    internal class AppSchema {
        internal static AppSchema Empty() => new(string.Empty, DirectedGraph<Aggregate>.Empty());

        internal AppSchema(string appName, DirectedGraph<Aggregate> directedGraph) {
            ApplicationName = appName;
            _graph = directedGraph;
        }

        private readonly DirectedGraph<Aggregate> _graph;

        public object ApplicationName { get; }

        internal IEnumerable<GraphNode<Aggregate>> AllAggregates() {
            return _graph;
        }
        internal IEnumerable<GraphNode<Aggregate>> RootAggregates() {
            return AllAggregates().Where(aggregate => aggregate.IsRoot());
        }

        internal DirectedGraph<EFCoreEntity> ToEFCoreGraph() {
            var nodes = AllAggregates().Select(aggregate => new EFCoreEntity(aggregate));

            return DirectedGraph<EFCoreEntity>.Create(nodes, _graph.Edges);
        }

        internal DirectedGraph<AggregateInstance> ToAggregateInstanceGraph() {
            var nodes = ToEFCoreGraph().Select(dbEntity => new  AggregateInstance(dbEntity));

            return DirectedGraph<AggregateInstance>.Create(nodes, _graph.Edges);
        }
    }
}
