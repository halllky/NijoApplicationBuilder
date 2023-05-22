using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514 {
    internal class AppSchema {
        internal const string REL_ATTR_RELATION_TYPE = "relationType";
        internal const string REL_ATTRVALUE_PARENT_CHILD = "child";
        internal const string REL_ATTRVALUE_REFERENCE = "reference";

        internal const string REL_ATTR_MULTIPLE = "multiple";
        internal const string REL_ATTR_IS_PRIMARY = "is-primary";
        internal const string REL_ATTR_IS_INSTANCE_NAME = "is-instance-name";

        internal static AppSchema Empty() => new(DirectedGraph<Aggregate>.Empty());

        internal AppSchema(DirectedGraph<Aggregate> directedGraph) {
            _graph = directedGraph;
        }

        private readonly DirectedGraph<Aggregate> _graph;

        internal IEnumerable<GraphNode<Aggregate>> AllAggregates() {
            return _graph;
        }
        internal IEnumerable<GraphNode<Aggregate>> RootAggregates() {
            return AllAggregates().Where(aggregate => !aggregate.In.Any(edge => (string)edge.Attributes[REL_ATTR_RELATION_TYPE] == REL_ATTRVALUE_PARENT_CHILD));
        }

        internal DirectedGraph<EFCoreEntity> ToEFCoreGraph() {
            var nodes = AllAggregates().Select(aggregate => new EFCoreEntity(this, aggregate.Item));

            return DirectedGraph<EFCoreEntity>.Create(nodes, _graph.Edges);
        }
    }
}
