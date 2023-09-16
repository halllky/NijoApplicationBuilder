using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static HalApplicationBuilder.Core.AggregateMember;

namespace HalApplicationBuilder.Core {

    public class AppSchemaBuilder : IAggregateBuildOption, IAggregateMemberBuildOption {

        private string? _applicationName;
        public AppSchemaBuilder SetApplicationName(string value) {
            _applicationName = value;
            return this;
        }
        public AppSchemaBuilder AddAggregate(IEnumerable<string> path, Action<IAggregateBuildOption>? options = null) {
            Scope(new TreePath(path.ToArray()), () => {
                SetOption(new() { { E_Option.ObjectType, OBJECT_TYPE_AGGREGATE } });
                options?.Invoke(this);
            });
            return this;
        }
        public AppSchemaBuilder AddAggregateMember(IEnumerable<string> path, Action<IAggregateMemberBuildOption>? options = null) {
            Scope(new TreePath(path), () => {
                SetOption(new() { { E_Option.ObjectType, OBJECT_TYPE_AGGREGATE_MEMBER } });
                options?.Invoke(this);
            });
            return this;
        }

        IAggregateBuildOption IAggregateBuildOption.IsPrimary(bool value) => SetOption(new() {
            { E_Option.IsPrimary, value },
        });
        IAggregateBuildOption IAggregateBuildOption.IsDisplayName(bool value) => SetOption(new() {
            { E_Option.IsInstanceName, value },
        });
        IAggregateBuildOption IAggregateBuildOption.IsArray(bool value) => SetOption(new() {
            { E_Option.IsArray, value },
        });
        IAggregateBuildOption IAggregateBuildOption.IsVariationGroupMember(string groupName, string key) => SetOption(new() {
            { E_Option.VariationGroupName, groupName },
            { E_Option.VariationGroupKey, key },
        });

        IAggregateMemberBuildOption IAggregateMemberBuildOption.MemberType(string typeName) => SetOption(new() {
            { E_Option.MemberTypeName, typeName },
        });
        IAggregateMemberBuildOption IAggregateMemberBuildOption.IsPrimary(bool value) => SetOption(new() {
            { E_Option.IsPrimary, value },
        });
        IAggregateMemberBuildOption IAggregateMemberBuildOption.IsDisplayName(bool value) => SetOption(new() {
            { E_Option.IsInstanceName, value },
        });
        IAggregateMemberBuildOption IAggregateMemberBuildOption.IsRequired(bool value) => SetOption(new() {
            { E_Option.IsRequired, value },
        });
        IAggregateMemberBuildOption IAggregateMemberBuildOption.IsReferenceTo(string refTarget) => SetOption(new() {
            { E_Option.RefTo, TreePath.FromString(refTarget) },
        });

        internal bool TryBuild(out AppSchema appSchema, out ICollection<string> errors, MemberTypeResolver? memberTypeResolver = null) {

            var aggregateDefs = _unvalidatedOptions
                .Where(x => x.Key.Item2 == E_Option.ObjectType
                         && (string)x.Value! == OBJECT_TYPE_AGGREGATE)
                .Select(aggregate => new {
                    TreePath = aggregate.Key.Item1,
                    Members = _unvalidatedOptions
                        .Where(y => y.Key.Item1.Parent == aggregate.Key.Item1
                                 && y.Key.Item2 == E_Option.ObjectType
                                 && (string)y.Value! == OBJECT_TYPE_AGGREGATE_MEMBER)
                        .Select(member => new {
                            Name = member.Key.Item1.BaseName,
                            Type = GetOption<string?>(member.Key.Item1, E_Option.MemberTypeName),
                            IsPrimary = GetOption<bool?>(member.Key.Item1, E_Option.IsPrimary) == true,
                            IsInstanceName = GetOption<bool?>(member.Key.Item1, E_Option.IsInstanceName) == true,
                            IsRequired = GetOption<bool?>(member.Key.Item1, E_Option.IsRequired) == true,
                            RefTarget = GetOption<TreePath?>(member.Key.Item1, E_Option.RefTo),
                        })
                        .ToArray(),
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
                        { DirectedEdgeExtensions.REL_ATTR_MULTIPLE, GetOption<bool?>(aggregate.TreePath, E_Option.IsArray) == true },
                        { DirectedEdgeExtensions.REL_ATTR_VARIATIONSWITCH, GetOption<string?>(aggregate.TreePath, E_Option.VariationGroupKey) ?? string.Empty },
                        { DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUPNAME, GetOption<string?>(aggregate.TreePath, E_Option.VariationGroupName) ?? string.Empty },
                        { DirectedEdgeExtensions.REL_ATTR_IS_PRIMARY, GetOption<bool?>(aggregate.TreePath, E_Option.IsPrimary) == true },
                        { DirectedEdgeExtensions.REL_ATTR_IS_INSTANCE_NAME, GetOption<bool?>(aggregate.TreePath, E_Option.IsInstanceName) == true },
                        { DirectedEdgeExtensions.REL_ATTR_IS_REQUIRED, GetOption<bool?>(aggregate.TreePath, E_Option.IsRequired) == true },
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
            // バリデーション

            errors = new HashSet<string>();
            memberTypeResolver ??= MemberTypeResolver.Default();

            if (string.IsNullOrWhiteSpace(_applicationName)) {
                errors.Add($"アプリケーション名が指定されていません。");
            }

            var aggregates = new Dictionary<AggregatePath, Aggregate>();
            var aggregateMembers = new HashSet<AggregateMemberNode>();
            var edgesFromAggToAgg = new List<GraphEdgeInfo>();
            var edgesFromAggToMember = new List<GraphEdgeInfo>();
            foreach (var aggregate in aggregateDefs) {
                var successToParse = true;

                // バリデーションおよびグラフ構成要素の作成: 集約ID
                if (!AggregatePath.TryParse(aggregate.TreePath.ToString(), out var id, out var error)) {
                    errors.Add(error);
                    successToParse = false;
                } else if (aggregates.ContainsKey(id)) {
                    errors.Add($"ID '{id}' が重複しています。");
                    successToParse = false;
                }

                // バリデーションおよびグラフ構成要素の作成: 集約メンバー
                var aggregateId = new NodeId(id.Value);
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
                    var memberId = new NodeId($"{id.Value}/{member.Name}");
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
                    aggregates.Add(id, new Aggregate(id));
                }
            }

            foreach (var relation in relationDefs) {
                var successToParse = true;

                // バリデーションおよびグラフ構成要素の作成: リレーションの集約ID
                if (!AggregatePath.TryParse(relation.Initial.ToString(), out var initial, out var error1)) {
                    errors.Add(error1);
                    successToParse = false;
                } else if (!aggregates.ContainsKey(initial)) {
                    errors.Add($"ID '{relation.Initial}' と対応する定義がありません。");
                    successToParse = false;
                }
                if (!AggregatePath.TryParse(relation.Terminal.ToString(), out var terminal, out var error2)) {
                    errors.Add(error2);
                    successToParse = false;
                } else if (!aggregates.ContainsKey(terminal)) {
                    errors.Add($"ID '{relation.Terminal}' と対応する定義がありません。");
                    successToParse = false;
                }

                if (successToParse) {
                    edgesFromAggToAgg.Add(new GraphEdgeInfo {
                        Initial = new NodeId(initial.Value),
                        Terminal = new NodeId(terminal.Value),
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

            appSchema = errors.Any()
                ? AppSchema.Empty()
                : new AppSchema(_applicationName!, graph, halappEnums);
            return !errors.Any();
        }


        #region オプションを好きな順番で定義できるようTryBuild実行時まで全てのオプションをobject型で保持しておくための仕組み
        private void Scope(TreePath objectPath, Action action) {
            _currentSettingObject.Push(objectPath);
            action();
            _currentSettingObject.Pop();
        }
        private T? GetOption<T>(TreePath owner, E_Option option) {
            var key = (owner, option);
            if (_unvalidatedOptions.TryGetValue(key, out var value)) {
                return (T)value!;
            } else {
                return default;
            }
        }
        private AppSchemaBuilder SetOption(Dictionary<E_Option, object?> options) {
            var obj = _currentSettingObject.Peek();
            foreach (var item in options) {
                _unvalidatedOptions[(obj, item.Key)] = item.Value;
            }
            return this;
        }
        private readonly Stack<TreePath> _currentSettingObject = new();
        private readonly Dictionary<(TreePath, E_Option), object?> _unvalidatedOptions = new();

        private enum E_Option {
            ObjectType,
            PhysicalName,
            Owner,
            IsPrimary,
            IsInstanceName,
            IsRequired,
            RefTo,
            IsArray,
            VariationGroupName,
            VariationGroupKey,
            MemberTypeName,
        }
        private const string OBJECT_TYPE_AGGREGATE = "aggregate";
        private const string OBJECT_TYPE_AGGREGATE_MEMBER = "aggregate-member";
        #endregion オプションを好きな順番で定義できるようTryBuild実行時まで全てのオプションをobject型で保持しておくための仕組み


        private class TreePath : ValueObject {
            public static TreePath FromString(string str) {
                var path = str
                    .Split(AggregatePath.SEPARATOR)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim());
                return new TreePath(path);
            }

            public TreePath(IEnumerable<string> value) {
                _value = value.ToArray();
            }
            private readonly string[] _value;
            private TreePath? _parentCache;

            public string BaseName => _value.LastOrDefault() ?? string.Empty;
            public bool IsRoot => _value.Length <= 1;
            public TreePath Parent => _parentCache ??= _value.Length == 0
                ? new TreePath(Enumerable.Empty<string>())
                : new TreePath(_value.SkipLast(1));

            public NodeId ToGraphNodeId() {
                return new NodeId(ToString());
            }
            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                foreach (var item in _value) yield return item;
            }
            public override string ToString() {
                return AggregatePath.SEPARATOR + _value.Join(AggregatePath.SEPARATOR.ToString());
            }
        }
    }

    public interface IAggregateBuildOption {
        IAggregateBuildOption IsPrimary(bool value = true);
        IAggregateBuildOption IsDisplayName(bool value = true);
        IAggregateBuildOption IsArray(bool value = true);
        IAggregateBuildOption IsVariationGroupMember(string groupName, string key);
    }
    public interface IAggregateMemberBuildOption {
        IAggregateMemberBuildOption MemberType(string typeName);
        IAggregateMemberBuildOption IsPrimary(bool value = true);
        IAggregateMemberBuildOption IsDisplayName(bool value = true);
        IAggregateMemberBuildOption IsRequired(bool value = true);
        IAggregateMemberBuildOption IsReferenceTo(string refTarget);
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
