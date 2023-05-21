using HalApplicationBuilder.Core20230514;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.EFCore {
    partial class Search {
        internal Search(CodeRenderingContext ctx) {
            _ctx = ctx;
            _graph = ctx.Schema.ToEFCoreGraph();
        }
        private readonly CodeRenderingContext _ctx;

        private class SearchMethod {
            public required string ReturnType { get; init; }
            public required string ReturnItemType { get; init; }
            public required string MethodName { get; init; }
            public required string ArgType { get; init; }
            public required string DbSetName { get; init; }
            public required IEnumerable<string> SelectClause { get; init; }
            public required IEnumerable<string> WhereClause { get; init; }
        }

        private readonly DirectedGraph<EFCoreEntity> _graph;
        private IEnumerable<SearchMethod> BuildSearchMethods() {
            IEnumerable<string> BuildSelectClauseRecursively(GraphNode<EFCoreEntity> graphNode) {
                var path = graphNode is NeighborNode<EFCoreEntity> neighborNode
                    ? neighborNode.PathFromEntry().Select(edge => edge.RelationName).ToArray()
                    : Array.Empty<string>();
                var indent = new string(' ', path.Length * 4);

                // 集約のメンバー
                foreach (var member in graphNode.Item.Source.Members) {
                    var memberName = member.ToPropertyDefinition().PropertyName;
                    var searchResultMember = string.Join("_", path.Union(new[] { memberName }));
                    var entityProp = string.Join(".", path.Union(new[] { memberName }));

                    yield return $"{indent}{searchResultMember} = {entityProp},";
                }

                // 子要素（除: Children）、参照先
                foreach (var edge in graphNode.Out) {
                    if ((string)edge.Attributes[AppSchema.REL_ATTR_RELATION_TYPE] == AppSchema.REL_ATTRVALUE_REFERENCE
                        || (string)edge.Attributes[AppSchema.REL_ATTR_RELATION_TYPE] == AppSchema.REL_ATTRVALUE_PARENT_CHILD
                        && !edge.Attributes.ContainsKey(AppSchema.REL_ATTR_MULTIPLE)) {

                        foreach (var line in BuildSelectClauseRecursively(edge.Terminal)) {
                            yield return line;
                        }
                    }
                }
            }
            IEnumerable<string> BuildWhereClauseRecursively(GraphNode<EFCoreEntity> graphNode) {
                var path = graphNode is NeighborNode<EFCoreEntity> neighborNode
                    ? neighborNode.PathFromEntry().Select(edge => edge.RelationName).ToArray()
                    : Array.Empty<string>();
                var indent = new string(' ', path.Length * 4);

            }

            //var roots = _graph.GetNodes().Where(entry => !entry.Node.In.Any(edge => edge.Relation.RelationName == AppSchema.REL_NAME_PARENT_CHILD));
            foreach (var graphNode in _graph) {
                // ルート集約以外なら処理中断
                if (graphNode.In.Any(edge => (string)edge.Attributes[AppSchema.REL_ATTR_RELATION_TYPE] == AppSchema.REL_ATTRVALUE_PARENT_CHILD)) {
                    continue;
                }

                var name = graphNode.Item.Source.DisplayName.ToCSharpSafe();
                var searchResult = $"{_ctx.Config.EntityNamespace}.{name}SearchResult";

                yield return new SearchMethod {
                    MethodName = $"Search{name}",

                    ReturnType = $"IEnumerable<{searchResult}>",
                    ReturnItemType = searchResult,
                    ArgType = $"{_ctx.Config.EntityNamespace}.{name}SearchCondition",

                    DbSetName = name,

                    SelectClause = BuildSelectClauseRecursively(graphNode),
                    WhereClause = BuildWhereClauseRecursively(graphNode),
                };
            }
        }
    }
}
