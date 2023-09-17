using HalApplicationBuilder.Core.AggregateMemberTypes;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static HalApplicationBuilder.Core.AggregateMember;

namespace HalApplicationBuilder.Core {

    public sealed class AppSchemaBuilder {

        private string? _applicationName;
        private readonly Dictionary<TreePath, AggregateBuildOption> _aggregates = new();
        private readonly Dictionary<TreePath, AggregateMemberBuildOption> _aggregateMembers = new();
        private readonly Dictionary<string, IEnumerable<EnumValueOption>> _enums = new();

        public AppSchemaBuilder SetApplicationName(string value) {
            _applicationName = value;
            return this;
        }
        public AppSchemaBuilder AddAggregate(IEnumerable<string> path, AggregateBuildOption? options = null) {
            _aggregates[new TreePath(path.ToArray())] = options ?? new();
            return this;
        }
        public AppSchemaBuilder AddAggregateMember(IEnumerable<string> path, AggregateMemberBuildOption? options = null) {
            _aggregateMembers[new TreePath(path)] = options ?? new();
            return this;
        }
        public AppSchemaBuilder AddEnum(string name, IEnumerable<EnumValueOption> values) {
            _enums[name] = values;
            return this;
        }

        internal bool TryBuild(out AppSchema appSchema, out ICollection<string> errors, MemberTypeResolver? memberTypeResolver = null) {

            var aggregateDefs = _aggregates
                .Select(aggregate => new {
                    TreePath = aggregate.Key,
                    Members = _aggregateMembers
                        .Where(member => member.Key.Parent == aggregate.Key)
                        .Select(member => new {
                            TreePath = member.Key,
                            Name = member.Key.BaseName,
                            Type = member.Value.MemberType,
                            IsPrimary = member.Value.IsPrimary == true,
                            IsInstanceName = member.Value.IsDisplayName == true,
                            IsRequired = member.Value.IsRequired == true,
                            RefTarget = member.Value.IsReferenceTo == null ? null : TreePath.FromString(member.Value.IsReferenceTo),
                        })
                        .ToArray(),
                    Options = aggregate.Value,
                })
                .ToArray();

            var parentAndChild = aggregateDefs
                .Where(aggregate => !aggregate.TreePath.IsRoot)
                .Select(aggregate => new {
                    Initial = aggregate.TreePath.Parent,
                    Terminal = aggregate.TreePath,
                    RelationName = aggregate.TreePath.BaseName,
                    Attributes = new Dictionary<string, object> {
                        { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD },
                        { DirectedEdgeExtensions.REL_ATTR_MULTIPLE, aggregate.Options.IsArray == true },
                        { DirectedEdgeExtensions.REL_ATTR_VARIATIONSWITCH, aggregate.Options.IsVariationGroupMember?.Key ?? string.Empty },
                        { DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUPNAME, aggregate.Options.IsVariationGroupMember?.GroupName ?? string.Empty },
                        { DirectedEdgeExtensions.REL_ATTR_IS_PRIMARY, aggregate.Options.IsPrimary == true },
                    },
                });
            var refs = aggregateDefs
                .SelectMany(aggregate => aggregate.Members, (aggregate, member) => new { aggregate, member })
                .Where(x => x.member.RefTarget != null)
                .Select(x => new {
                    Initial = x.aggregate.TreePath,
                    Terminal = x.member.RefTarget!,
                    RelationName = x.member.Name,
                    Attributes = new Dictionary<string, object> {
                        { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_REFERENCE },
                        { DirectedEdgeExtensions.REL_ATTR_IS_PRIMARY, x.member.IsPrimary },
                        { DirectedEdgeExtensions.REL_ATTR_IS_INSTANCE_NAME, x.member.IsInstanceName },
                        { DirectedEdgeExtensions.REL_ATTR_IS_REQUIRED, x.member.IsRequired },
                    },
                });
            var relationDefs = parentAndChild.Concat(refs);

            // ---------------------------------------------------------
            // バリデーションおよびドメインクラスへの変換

            errors = new HashSet<string>();
            memberTypeResolver ??= MemberTypeResolver.Default();

            if (string.IsNullOrWhiteSpace(_applicationName)) {
                errors.Add($"アプリケーション名が指定されていません。");
            }

            // enumの組み立て
            var builtEnums = new List<EnumDefinition>();
            foreach (var @enum in _enums) {
                var items = new List<EnumDefinition.Item>();
                var unusedInt = 0;
                var usedInt = @enum.Value
                    .Where(v => v.Value.HasValue)
                    .Select(v => v.Value)
                    .Cast<int>()
                    .ToHashSet();
                foreach (var item in @enum.Value) {
                    if (item.Value.HasValue) {
                        items.Add(new EnumDefinition.Item {
                            PhysicalName = item.Name,
                            Value = item.Value.Value,
                        });
                    } else {
                        // 値が未指定の場合、自動的に使われていない整数値を使う
                        while (usedInt.Contains(unusedInt)) unusedInt++;
                        usedInt.Add(unusedInt);
                        items.Add(new EnumDefinition.Item {
                            PhysicalName = item.Name,
                            Value = unusedInt,
                        });
                    }
                }

                if (EnumDefinition.TryCreate(@enum.Key, items, out var created, out var enumCreateErrors)) {
                    builtEnums.Add(created);
                    memberTypeResolver.Register(created.Name, new EnumList(created));
                } else {
                    foreach (var err in enumCreateErrors) errors.Add(err);
                }
            }

            // GraphNodeの組み立て
            var aggregates = new Dictionary<NodeId, Aggregate>();
            var aggregateMembers = new HashSet<AggregateMemberNode>();
            var edgesFromAggToAgg = new List<GraphEdgeInfo>();
            var edgesFromAggToMember = new List<GraphEdgeInfo>();
            foreach (var aggregate in aggregateDefs) {
                var successToParse = true;

                // バリデーションおよびグラフ構成要素の作成: 集約ID
                var aggregateId = aggregate.TreePath.ToGraphNodeId();
                if (aggregates.ContainsKey(aggregateId)) {
                    errors.Add($"ID '{aggregate.TreePath}' が重複しています。");
                    successToParse = false;
                }

                // バリデーションおよびグラフ構成要素の作成: 集約メンバー
                foreach (var member in aggregate.Members) {

                    // refはリレーションの方で作成する
                    if (member.RefTarget != null) continue;

                    if (member.Type == null) {
                        errors.Add($"'{member.Name}' のタイプが指定されていません。");
                        successToParse = false;
                        continue;
                    }
                    if (!memberTypeResolver.TryResolve(member.Type, out var memberType)) {
                        errors.Add($"'{member.Name}' のタイプ '{member.Type}' が不正です。");
                        successToParse = false;
                        continue;
                    }
                    var memberId = member.TreePath.ToGraphNodeId();
                    aggregateMembers.Add(new AggregateMemberNode {
                        Id = memberId,
                        Name = member.Name,
                        Type = memberType,
                        IsPrimary = member.IsPrimary,
                        IsInstanceName = member.IsInstanceName,
                        Optional = !member.IsRequired,
                    });
                    edgesFromAggToMember.Add(new GraphEdgeInfo {
                        Initial = aggregateId,
                        Terminal = memberId,
                        RelationName = member.Name,
                        Attributes = new Dictionary<string, object> {
                            { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_HAVING },
                        },
                    });
                }

                if (successToParse) {
                    aggregates.Add(aggregateId, new Aggregate(aggregate.TreePath));
                }
            }

            // GraphEdgeの組み立て
            foreach (var relation in relationDefs) {
                var successToParse = true;
                var initial = relation.Initial.ToGraphNodeId();
                var terminal = relation.Terminal.ToGraphNodeId();

                // バリデーションおよびグラフ構成要素の作成: リレーションの集約ID
                if (!aggregates.ContainsKey(initial)) {
                    errors.Add($"ID '{relation.Initial}' と対応する定義がありません。");
                    successToParse = false;
                }
                if (!aggregates.ContainsKey(terminal)) {
                    errors.Add($"ID '{relation.Terminal}' と対応する定義がありません。");
                    successToParse = false;
                }

                if (successToParse) {
                    edgesFromAggToAgg.Add(new GraphEdgeInfo {
                        Initial = initial,
                        Terminal = terminal,
                        RelationName = relation.RelationName,
                        Attributes = relation.Attributes,
                    });
                }
            }

            // ---------------------------------------------------------
            // 基盤機能
            var halappEntities = new List<IGraphNode>();
            var halappEnums = new List<EnumDefinition>();

            // 基盤機能: バッチ処理
            halappEntities.Add(CodeRendering.BackgroundService.BackgroundTaskEntity.CreateEntity());
            halappEnums.Add(CodeRendering.BackgroundService.BackgroundTaskEntity.CreateBackgroundTaskStateEnum());

            // ---------------------------------------------------------
            // グラフを作成して返す
            var nodes = aggregates.Values
                .Cast<IGraphNode>()
                .Concat(aggregateMembers)
                .Concat(halappEntities);
            var edges = edgesFromAggToAgg
                .Concat(edgesFromAggToMember);
            if (!DirectedGraph.TryCreate(nodes, edges, out var graph, out var errors1)) {
                foreach (var err in errors1) errors.Add(err);
            }
            var enums = builtEnums
                .Concat(halappEnums)
                .ToArray();

            appSchema = errors.Any()
                ? AppSchema.Empty()
                : new AppSchema(_applicationName!, graph, enums);
            return !errors.Any();
        }
    }

    public sealed class AggregateBuildOption {
        public bool? IsPrimary { get; set; }
        public bool? IsArray { get; set; }
        public GroupOption? IsVariationGroupMember { get; set; }

        public sealed class GroupOption {
            public required string GroupName { get; init; }
            public required string Key { get; init; }
        }
    }
    public sealed class AggregateMemberBuildOption {
        public string? MemberType { get; set; }
        public bool? IsPrimary { get; set; }
        public bool? IsDisplayName { get; set; }
        public bool? IsRequired { get; set; }
        public string? IsReferenceTo { get; set; }
    }
    public sealed class EnumValueOption {
        public string Name { get; set; } = string.Empty;
        public int? Value { get; set; }
    }


    internal static class DirectedEdgeExtensions {
        internal const string REL_ATTR_RELATION_TYPE = "relationType";
        internal const string REL_ATTRVALUE_HAVING = "having";
        internal const string REL_ATTRVALUE_PARENT_CHILD = "child";
        internal const string REL_ATTRVALUE_REFERENCE = "reference";
        internal const string REL_ATTRVALUE_AGG_2_ETT = "aggregate-dbentity";
        internal const string REL_ATTRVALUE_AGG_2_INS = "aggregate-instance";

        internal const string REL_ATTR_MULTIPLE = "multiple";
        internal const string REL_ATTR_VARIATIONGROUPNAME = "variation-group-name";
        internal const string REL_ATTR_VARIATIONSWITCH = "switch";
        internal const string REL_ATTR_IS_PRIMARY = "is-primary";
        internal const string REL_ATTR_IS_INSTANCE_NAME = "is-instance-name";
        internal const string REL_ATTR_IS_REQUIRED = "is-required";

        // ----------------------------- GraphNode extensions -----------------------------

        internal static bool IsRoot(this GraphNode graphNode) {
            return !graphNode.In.Any(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                                             && (string)type == REL_ATTRVALUE_PARENT_CHILD);
        }
        internal static GraphNode<T> GetRoot<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.EnumerateAncestorsAndThis().First();
        }

        internal static GraphEdge? GetParent(this GraphNode graphNode) {
            return graphNode.In.SingleOrDefault(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                                                     && (string)type == REL_ATTRVALUE_PARENT_CHILD);
        }
        internal static GraphEdge<T>? GetParent<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return ((GraphNode)graphNode).GetParent()?.As<T>();
        }

        internal static IEnumerable<GraphNode<T>> SelectUntil<T>(this GraphNode<T> graphNode, Func<GraphNode<T>, IEnumerable<GraphNode<T>>> predicate) where T : IGraphNode {
            foreach (var item in predicate(graphNode)) {
                yield return item;

                foreach (var item2 in SelectUntil(item, predicate)) {
                    yield return item2;
                }
            }
        }
        internal static IEnumerable<GraphNode<T>> SelectThisAndUntil<T>(this GraphNode<T> graphNode, Func<GraphNode<T>, IEnumerable<GraphNode<T>>> predicate) where T : IGraphNode {
            yield return graphNode;
            foreach (var item in graphNode.SelectUntil(predicate)) {
                yield return item;
            }
        }

        /// <summary>
        /// 祖先を列挙する。ルート要素が先。
        /// </summary>
        internal static IEnumerable<GraphNode<T>> EnumerateAncestorsAndThis<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            foreach (var ancestor in graphNode.EnumerateAncestors()) {
                yield return ancestor.Initial;
            }
            yield return graphNode;
        }
        /// <summary>
        /// 祖先を列挙する。ルート要素が先。
        /// </summary>
        internal static IEnumerable<GraphEdge<T>> EnumerateAncestors<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            var stack = new Stack<GraphEdge<T>>();
            GraphEdge<T>? edge = graphNode.GetParent();
            while (edge != null) {
                stack.Push(edge);
                edge = edge.Initial.GetParent();
            }
            while (stack.Count > 0) {
                yield return stack.Pop();
            }
        }

        internal static IEnumerable<GraphNode<T>> EnumerateDescendants<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            static IEnumerable<GraphNode<T>> GetDescencantsRecursively(GraphNode<T> node) {
                var children = node.GetChildEdges()
                    .Concat(node.GetVariationGroups().SelectMany(group => group.VariationAggregates.Values))
                    .Concat(node.GetChildrenEdges());
                foreach (var edge in children) {
                    yield return edge.Terminal;
                    foreach (var descendant in GetDescencantsRecursively(edge.Terminal)) {
                        yield return descendant;
                    }
                }
            }

            foreach (var desc in GetDescencantsRecursively(graphNode)) {
                yield return desc;
            }
        }
        internal static IEnumerable<GraphNode<T>> EnumerateThisAndDescendants<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            yield return graphNode;
            foreach (var desc in graphNode.EnumerateDescendants()) {
                yield return desc;
            }
        }

        internal static bool IsChildrenMember(this GraphNode graphNode) {
            var parent = graphNode.GetParent();
            return parent != null
                && parent.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                && parent.Attributes.TryGetValue(REL_ATTR_MULTIPLE, out var isArray)
                && (bool)isArray;
        }
        internal static bool IsChildMember(this GraphNode graphNode) {
            var parent = graphNode.GetParent();
            return parent != null
                && parent.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                && (!parent.Attributes.TryGetValue(REL_ATTR_MULTIPLE, out var isArray) || (bool)isArray == false)
                && (!parent.Attributes.TryGetValue(REL_ATTR_VARIATIONGROUPNAME, out var groupName) || (string)groupName == string.Empty);
        }
        internal static bool IsVariationMember(this GraphNode graphNode) {
            var parent = graphNode.GetParent();
            return parent != null
                && parent.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                && (!parent.Attributes.TryGetValue(REL_ATTR_MULTIPLE, out var isArray) || (bool)isArray == false)
                && parent.Attributes.TryGetValue(REL_ATTR_VARIATIONGROUPNAME, out var groupName)
                && (string)groupName != string.Empty;
        }

        internal static IEnumerable<GraphNode<AggregateMemberNode>> GetMemberNodes(this GraphNode<Aggregate> aggregate) {
            return aggregate.Out
                .Where(edge => (string)edge.Attributes[REL_ATTR_RELATION_TYPE] == REL_ATTRVALUE_HAVING)
                .Select(edge => edge.Terminal.As<AggregateMemberNode>());
        }
        internal static IEnumerable<GraphEdge<T>> GetChildrenEdges<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.Out
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_PARENT_CHILD
                            && edge.Attributes.TryGetValue(REL_ATTR_MULTIPLE, out var isArray)
                            && (bool)isArray)
                .Select(edge => edge.As<T>());
        }
        internal static IEnumerable<GraphEdge<T>> GetChildEdges<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.Out
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_PARENT_CHILD
                            && (!edge.Attributes.TryGetValue(REL_ATTR_MULTIPLE, out var isArray) || (bool)isArray == false)
                            && (!edge.Attributes.TryGetValue(REL_ATTR_VARIATIONGROUPNAME, out var groupName) || (string)groupName == string.Empty))
                .Select(edge => edge.As<T>());
        }
        internal static IEnumerable<GraphEdge<T>> GetRefEdge<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.Out
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_REFERENCE)
                .Select(edge => edge.As<T>());
        }
        internal static IEnumerable<GraphEdge<T>> GetReferedEdges<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.In
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_REFERENCE)
                .Select(edge => edge.As<T>());
        }

        internal static IEnumerable<VariationGroup<T>> GetVariationGroups<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.Out
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_PARENT_CHILD
                            && (!edge.Attributes.TryGetValue(REL_ATTR_MULTIPLE, out var isArray) || (bool)isArray == false)
                            && (!edge.Attributes.TryGetValue(REL_ATTR_VARIATIONGROUPNAME, out var groupName)
                            && (string)groupName! != string.Empty))
                .GroupBy(edge => (string)edge.Attributes[REL_ATTR_VARIATIONGROUPNAME])
                .Select(group => new VariationGroup<T> {
                    GroupName = group.Key,
                    VariationAggregates = group.ToDictionary(
                        edge => (string)edge.Attributes[REL_ATTR_VARIATIONSWITCH],
                        edge => edge.As<T>()),
                });
        }

        // ----------------------------- GraphEdge extensions -----------------------------

        internal static bool IsPrimary(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_PRIMARY, out var bln) && (bool)bln;
        }
        internal static bool IsInstanceName(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_INSTANCE_NAME, out var bln) && (bool)bln;
        }
        internal static bool IsRequired(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_REQUIRED, out var bln) && (bool)bln;
        }
        internal static bool IsRef(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_REFERENCE;
        }
    }

    internal class VariationGroup<T> where T : IGraphNode {
        internal GraphNode<T> Owner => VariationAggregates.First().Value.Initial.As<T>();
        internal required string GroupName { get; init; }
        internal required IReadOnlyDictionary<string, GraphEdge<T>> VariationAggregates { get; init; }
        internal bool IsPrimary => VariationAggregates.First().Value.IsPrimary();
        internal bool IsInstanceName => VariationAggregates.First().Value.IsInstanceName();
        internal bool RequiredAtDB => VariationAggregates.First().Value.IsRequired();
    }
}
