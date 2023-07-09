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
            Graph = directedGraph;
        }

        public object ApplicationName { get; }
        internal DirectedGraph Graph { get; }

        internal IEnumerable<GraphNode<Aggregate>> AllAggregates() {
            return Graph.Only<Aggregate>();
        }
        internal IEnumerable<GraphNode<Aggregate>> RootAggregates() {
            return AllAggregates().Where(aggregate => aggregate.IsRoot());
        }

        internal IEnumerable<GraphNode<EFCoreEntity>> ToEFCoreGraph() {
            return Graph.Only<EFCoreEntity>();
        }

        internal IEnumerable<GraphNode<AggregateInstance>> ToAggregateInstanceGraph() {
            return Graph.Only<AggregateInstance>();
        }
    }
}
