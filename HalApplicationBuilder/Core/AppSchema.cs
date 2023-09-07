using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal class AppSchema {
        internal static AppSchema Empty() => new(string.Empty, DirectedGraph.Empty(), new HashSet<EnumDefinition>());

        internal AppSchema(string appName, DirectedGraph directedGraph, IReadOnlyCollection<EnumDefinition> enumDefinitions) {
            ApplicationName = appName;
            Graph = directedGraph;
            EnumDefinitions = enumDefinitions;
        }

        public object ApplicationName { get; }

        internal DirectedGraph Graph { get; }
        internal IEnumerable<GraphNode<Aggregate>> AllAggregates() {
            return Graph.Only<Aggregate>();
        }
        internal IEnumerable<GraphNode<Aggregate>> RootAggregates() {
            return AllAggregates().Where(aggregate => aggregate.IsRoot());
        }
        internal IEnumerable<GraphNode<IEFCoreEntity>> ToEFCoreGraph() {
            return Graph.Only<IEFCoreEntity>();
        }
        internal IEnumerable<GraphNode<AggregateInstance>> ToAggregateInstanceGraph() {
            return Graph.Only<AggregateInstance>();
        }

        internal IReadOnlyCollection<EnumDefinition> EnumDefinitions { get; }
    }
}
