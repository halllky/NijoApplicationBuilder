using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HalApplicationBuilder.Core {
    internal class AppSchemaBuilder {

        #region XML
        internal static bool FromXml(XDocument xDocument, out AppSchemaBuilder builder, out ICollection<string> errors) {
            if (xDocument.Root == null) throw new FormatException($"Xml doesn't have contents.");
            var schemaBuilder = new AppSchemaBuilder { ApplicationName = xDocument.Root.Name.LocalName };
            var errorList = new List<string>();

            IEnumerable<XElement> GetSelfAndAncestors(XElement xElement) {
                yield return xElement;
                var parent = xElement.Parent;
                while (parent != null && parent != xDocument.Root) {
                    yield return parent;
                    parent = parent.Parent;
                }
            }
            void ParseAsAggregate(XElement el, AggregatePath? parent) {
                var localError = new HashSet<string>();
                if (el.Attribute(XML_ATTR_KEY) != null)
                    localError.Add($"Aggregate define '{el.Name}' cann't have '{XML_ATTR_KEY}' attribute.");
                if (el.Attribute(XML_ATTR_NAME) != null)
                    localError.Add($"Aggregate define '{el.Name}' cann't have '{XML_ATTR_NAME}' attribute.");
                if (el.Attribute(XML_ATTR_TYPE) != null)
                    localError.Add($"Aggregate define '{el.Name}' cann't have '{XML_ATTR_TYPE}' attribute.");
                if (el.Attribute(XML_ATTR_REFTO) != null)
                    localError.Add($"Aggregate define '{el.Name}' cann't have '{XML_ATTR_REFTO}' attribute.");

                var multiple = el.Attribute(XML_ATTR_MULTIPLE) != null;
                if (parent == null && multiple)
                    localError.Add($"Root aggregate define '{el.Name}' cann't have '{XML_ATTR_MULTIPLE}' attribute.");

                var ancestorNames = GetSelfAndAncestors(el)
                    .Reverse()
                    .Select(xElement => xElement.Name.LocalName);
                if (!AggregatePath.TryCreate(ancestorNames, out var aggregatePath, out var err)) {
                    localError.Add(err);
                }

                var schalarMembers = new List<SchalarMemberDef>();
                foreach (var innerElement in el.Elements()) {
                    var type = innerElement.Attribute(XML_ATTR_TYPE);
                    var key = innerElement.Attribute(XML_ATTR_KEY);
                    var name = innerElement.Attribute(XML_ATTR_NAME);
                    var refTo = innerElement.Attribute(XML_ATTR_REFTO);

                    if (type != null) {
                        schalarMembers.Add(new SchalarMemberDef {
                            Name = innerElement.Name.LocalName,
                            Type = type.Value,
                            IsPrimary = key != null,
                            IsInstanceName = name != null,
                        });
                    } else if (refTo != null) {
                        schemaBuilder.AddReference(new ReferenceDef {
                            Name = innerElement.Name.LocalName,
                            OwnerFullPath = aggregatePath.Value,
                            TargetFullPath = refTo.Value,
                            IsPrimary = key != null,
                            IsInstanceName = name != null,
                        });
                    } else {
                        ParseAsAggregate(innerElement, parent: aggregatePath);
                    }
                }

                if (localError.Any()) {
                    errorList.AddRange(localError);
                    return;
                }

                if (parent == null) {
                    schemaBuilder.AddAggregate(new AggregateDef {
                        FullPath = aggregatePath,
                        Members = schalarMembers,
                    });
                } else if (multiple) {
                    schemaBuilder.AddChildrenAggregate(new ChildrenDef {
                        Name = el.Name.LocalName,
                        Members = schalarMembers,
                        OwnerFullPath = parent.Value,
                    });
                } else {
                    schemaBuilder.AddChildAggregate(new ChildDef {
                        Name = el.Name.LocalName,
                        Members = schalarMembers,
                        OwnerFullPath = parent.Value,
                    });
                }
                // TODO: variationを解釈していない
            }

            foreach (var xElement in xDocument.Root.Elements()) {
                if (xElement.Name.LocalName == Config.XML_CONFIG_SECTION_NAME) continue;
                ParseAsAggregate(xElement, parent: null);
            }
            builder = schemaBuilder;
            errors = errorList;
            return errors.Count == 0;
        }
        private const string XML_ATTR_KEY = "key";
        private const string XML_ATTR_NAME = "name";
        private const string XML_ATTR_TYPE = "type";
        private const string XML_ATTR_REFTO = "refTo";
        private const string XML_ATTR_MULTIPLE = "multiple";
        #endregion XML

        internal required string ApplicationName { get; init; }

        private readonly List<AggregateDef> _aggregateDefs = new List<AggregateDef>();
        private readonly List<ChildDef> _childDefs = new List<ChildDef>();
        private readonly List<VariationDef> _variationDefs = new List<VariationDef>();
        private readonly List<ChildrenDef> _childrenDefs = new List<ChildrenDef>();
        private readonly List<ReferenceDef> _referencesDefs = new List<ReferenceDef>();

        internal AppSchemaBuilder AddAggregate(AggregateDef aggregateDef) {
            _aggregateDefs.Add(aggregateDef);
            return this;
        }
        internal AppSchemaBuilder AddChildAggregate(ChildDef childDef) {
            _childDefs.Add(childDef);
            return this;
        }
        internal AppSchemaBuilder AddChildrenAggregate(ChildrenDef childrenDef) {
            _childrenDefs.Add(childrenDef);
            return this;
        }
        internal AppSchemaBuilder AddVariationAggregate(VariationDef variationDef) {
            _variationDefs.Add(variationDef);
            return this;
        }
        internal AppSchemaBuilder AddReference(ReferenceDef referenceDef) {
            _referencesDefs.Add(referenceDef);
            return this;
        }

        internal bool TryBuild(out AppSchema appSchema, out ICollection<string> errors, MemberTypeResolver? memberTypeResolver = null) {

            errors = new HashSet<string>();
            if (memberTypeResolver == null) memberTypeResolver = MemberTypeResolver.Default();

            var aggregates = new Dictionary<NodeId, Aggregate>();
            var aggregateRelations = new Dictionary<(NodeId, string, NodeId), GraphEdgeInfo>();

            // ----------------------------------------------------------------------------------
            // 集約ノードの作成
            foreach (var def in _aggregateDefs) {
                var members = new List<Aggregate.Member>();
                foreach (var member in def.Members) {
                    if (memberTypeResolver.TryResolve(member.Type, out var memberType)) {
                        members.Add(new Aggregate.Member {
                            Name = member.Name,
                            Type = memberType,
                            IsPrimary = member.IsPrimary,
                            IsInstanceName = member.IsInstanceName,
                        });
                    } else {
                        errors.Add($"Type name '{member.Type}' of '{member.Name}' is invalid.");
                    }
                }
                var aggregate = new Aggregate(def.FullPath, members);
                aggregates.Add(aggregate.Id, aggregate);
            }

            foreach (var def in _childrenDefs) {
                if (!AggregatePath.TryParse(def.OwnerFullPath, out var parentPath, out var err)) {
                    errors.Add(err);
                    continue;
                }
                var path = parentPath.GetChildAggregatePath(def.Name);
                var members = new List<Aggregate.Member>();
                foreach (var member in def.Members) {
                    if (memberTypeResolver.TryResolve(member.Type, out var memberType)) {
                        members.Add(new Aggregate.Member {
                            Name = member.Name,
                            Type = memberType,
                            IsPrimary = member.IsPrimary,
                            IsInstanceName = member.IsInstanceName,
                        });
                    } else {
                        errors.Add($"Type name '{member.Type}' of '{member.Name}' is invalid.");
                    }
                }
                var aggregate = new Aggregate(path, members);
                var relation = new GraphEdgeInfo {
                    Initial = new NodeId(parentPath.Value),
                    Terminal = new NodeId(path.Value),
                    RelationName = def.Name,
                    Attributes = new Dictionary<string, object> {
                        { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD },
                        { DirectedEdgeExtensions.REL_ATTR_MULTIPLE, true },
                    },
                };
                aggregates.Add(aggregate.Id, aggregate);
                aggregateRelations[(relation.Initial, relation.RelationName, relation.Terminal)] = relation;
            }

            foreach (var def in _childDefs) {
                if (!AggregatePath.TryParse(def.OwnerFullPath, out var parentPath, out var err)) {
                    errors.Add(err);
                    continue;
                }
                var path = parentPath.GetChildAggregatePath(def.Name);
                var members = new List<Aggregate.Member>();
                foreach (var member in def.Members) {
                    if (memberTypeResolver.TryResolve(member.Type, out var memberType)) {
                        members.Add(new Aggregate.Member {
                            Name = member.Name,
                            Type = memberType,
                            IsPrimary = member.IsPrimary,
                            IsInstanceName = member.IsInstanceName,
                        });
                    } else {
                        errors.Add($"Type name of '{member.Name}' is invalid: '{member.Type}'");
                    }
                }
                var aggregate = new Aggregate(path, members);
                var relation = new GraphEdgeInfo {
                    Initial = new NodeId(parentPath.Value),
                    Terminal = new NodeId(path.Value),
                    RelationName = def.Name,
                    Attributes = new Dictionary<string, object> {
                        { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD },
                    },
                };
                aggregates.Add(aggregate.Id, aggregate);
                aggregateRelations[(relation.Initial, relation.RelationName, relation.Terminal)] = relation;
            }

            foreach (var def in _variationDefs) {
                if (!AggregatePath.TryParse(def.OwnerFullPath, out var parentPath, out var err)) {
                    errors.Add(err);
                    continue;
                }

                foreach (var variation in def.Variations) {
                    var path = parentPath.GetChildAggregatePath(variation.Name);
                    var members = new List<Aggregate.Member>();
                    foreach (var member in variation.Members) {
                        if (memberTypeResolver.TryResolve(member.Type, out var memberType)) {
                            members.Add(new Aggregate.Member {
                                Name = member.Name,
                                Type = memberType,
                                IsPrimary = member.IsPrimary,
                                IsInstanceName = member.IsInstanceName,
                            });
                        } else {
                            errors.Add($"Type name '{member.Type}' of '{member.Name}' is invalid.");
                        }
                    }
                    var aggregate = new Aggregate(path, members);
                    var relation = new GraphEdgeInfo {
                        Initial = new NodeId(parentPath.Value),
                        Terminal = new NodeId(path.Value),
                        RelationName = def.Name,
                        Attributes = new Dictionary<string, object> {
                            { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD },
                        },
                    };
                    aggregates.Add(aggregate.Id, aggregate);
                    aggregateRelations[(relation.Initial, relation.RelationName, relation.Terminal)] = relation;
                }
            }

            foreach (var def in _referencesDefs) {
                if (!AggregatePath.TryParse(def.OwnerFullPath, out var ownerPath, out var err)) {
                    errors.Add(err);
                    continue;
                }
                if (!AggregatePath.TryParse(def.TargetFullPath, out var targetPath, out err)) {
                    errors.Add(err);
                    continue;
                }
                var relation = new GraphEdgeInfo {
                    Initial = new NodeId(ownerPath.Value),
                    Terminal = new NodeId(targetPath.Value),
                    RelationName = def.Name,
                    Attributes = new Dictionary<string, object> {
                        { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_REFERENCE },
                        { DirectedEdgeExtensions.REL_ATTR_IS_PRIMARY, def.IsPrimary },
                        { DirectedEdgeExtensions.REL_ATTR_IS_INSTANCE_NAME, def.IsInstanceName },
                    },
                };
                aggregateRelations[(relation.Initial, relation.RelationName, relation.Terminal)] = relation;
            }

            // ----------------------------------------------------------------------------------
            // 集約ノードのバリデーション
            var aggregateDict = new Dictionary<AggregatePath, Aggregate>();
            foreach (var aggregate in aggregates.Values) {
                aggregateDict[aggregate.Path] = aggregate; 
            }
            var duplicates = aggregates.Values
                .GroupBy(a => a.Path)
                .Where(group => group.Count() >= 2);
            foreach (var dup in duplicates) {
                errors.Add($"Aggregate path duplicates: {dup.Key}");
            }

            // ----------------------------------------------------------------------------------
            // DBEntityノード、Instanceノードの作成
            var dbEntities = new List<IGraphNode>();
            var dbEntityRelations = new Dictionary<(NodeId, string, NodeId), GraphEdgeInfo>();
            foreach (var aggregate in aggregates.Values) {
                var dbEntity = new EFCoreEntity(aggregate);
                dbEntities.Add(dbEntity);

                dbEntityRelations.Add((dbEntity.Id, "origin", aggregate.Id), new GraphEdgeInfo {
                    Initial = dbEntity.Id,
                    Terminal = aggregate.Id,
                    RelationName = "origin",
                    Attributes = new Dictionary<string, object> {
                        { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_AGG_2_ETT },
                    },
                });

                foreach (var edge in aggregateRelations.Values.Where(e => e.Initial == aggregate.Id)) {
                    var terminal = new EFCoreEntity(aggregates[edge.Terminal]).Id;
                    dbEntityRelations.Add((dbEntity.Id, edge.RelationName, terminal), new GraphEdgeInfo {
                         Initial = dbEntity.Id,
                         RelationName = edge.RelationName,
                         Terminal = terminal,
                         Attributes = new Dictionary<string, object>(edge.Attributes),
                    });
                }
            }

            var aggregateInstances = new List<IGraphNode>();
            var aggregateInstanceRelations = new Dictionary<(NodeId, string, NodeId), GraphEdgeInfo>();
            foreach (var aggregate in aggregates.Values) {
                var instance = new AggregateInstance(aggregate);
                aggregateInstances.Add(instance);

                aggregateInstanceRelations.Add((instance.Id, "origin", aggregate.Id), new GraphEdgeInfo {
                    Initial = instance.Id,
                    Terminal = aggregate.Id,
                    RelationName = "origin",
                    Attributes = new Dictionary<string, object> {
                        { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_AGG_2_INS },
                    },
                });

                foreach (var edge in aggregateRelations.Values.Where(e => e.Initial == aggregate.Id)) {
                    var terminal = new AggregateInstance(aggregates[edge.Terminal]).Id;
                    aggregateInstanceRelations.Add((instance.Id, edge.RelationName, terminal), new GraphEdgeInfo {
                        Initial = instance.Id,
                        RelationName = edge.RelationName,
                        Terminal = terminal,
                        Attributes = new Dictionary<string, object>(edge.Attributes),
                    });
                }
            }

            // ----------------------------------------------------------------------------------
            // グラフを作成して返す
            var allNodes = aggregates.Values
                .Concat(dbEntities)
                .Concat(aggregateInstances);
            var allEdges = aggregateRelations.Values
                .Concat(dbEntityRelations.Values)
                .Concat(aggregateInstanceRelations.Values);
            if (!DirectedGraph.TryCreate(allNodes, allEdges, out var graph, out var errors1)) {
                foreach (var err in errors1) errors.Add(err);
            }
            if (errors.Any()) {
                appSchema = AppSchema.Empty();
                return false;
            }
            appSchema = new AppSchema(ApplicationName, graph);
            return true;
        }

        internal class AggregateDef {
            internal AggregatePath FullPath { get; set; } = AggregatePath.Empty;
            internal IList<SchalarMemberDef> Members { get; set; } = new List<SchalarMemberDef>();
        }
        internal class SchalarMemberDef {
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
            public bool IsPrimary { get; set; }
            public bool IsInstanceName { get; set; }
        }
        internal class ChildDef {
            public string Name { get; set; } = "";
            public string OwnerFullPath { get; set; } = "";
            internal IList<SchalarMemberDef> Members { get; set; } = new List<SchalarMemberDef>();
        }
        internal class VariationDef {
            public string Name { get; set; } = "";
            public string OwnerFullPath { get; set; } = "";
            public IList<Item> Variations { get; set; } = new List<Item>();

            internal class Item {
                public string Name { get; set; } = "";
                internal IList<SchalarMemberDef> Members { get; set; } = new List<SchalarMemberDef>();
            }
        }
        internal class ChildrenDef {
            public string Name { get; set; } = "";
            public string OwnerFullPath { get; set; } = "";
            internal IList<SchalarMemberDef> Members { get; set; } = new List<SchalarMemberDef>();
        }
        internal class ReferenceDef {
            public string Name { get; set; } = "";
            public string OwnerFullPath { get; set; } = "";
            public string TargetFullPath { get; set; } = "";
            public bool IsPrimary { get; set; }
            public bool IsInstanceName { get; set; }
        }
    }

    internal static class DirectedEdgeExtensions {
        internal const string REL_ATTR_RELATION_TYPE = "relationType";
        internal const string REL_ATTRVALUE_PARENT_CHILD = "child";
        internal const string REL_ATTRVALUE_REFERENCE = "reference";
        internal const string REL_ATTRVALUE_AGG_2_ETT = "aggregate-dbentity";
        internal const string REL_ATTRVALUE_AGG_2_INS = "aggregate-instance";

        internal const string REL_ATTR_MULTIPLE = "multiple";
        internal const string REL_ATTR_IS_PRIMARY = "is-primary";
        internal const string REL_ATTR_IS_INSTANCE_NAME = "is-instance-name";

        // ----------------------------- DirectedGraph extensions -----------------------------

        internal static IEnumerable<GraphNode<EFCoreEntity>> RootEntities(this IEnumerable<GraphNode<EFCoreEntity>> graph) {
            return graph.Where(entity => entity.IsRoot());
        }

        // ----------------------------- GraphNode extensions -----------------------------

        internal static IEnumerable<GraphNode<T>> EnumerateAncestors<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            GraphNode<T>? node = graphNode.GetParent()?.Initial;
            while (node != null) {
                yield return node;
                node = node.GetParent()?.Initial;
            }
        }
        internal static IEnumerable<GraphNode<T>> EnumerateDescendants<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            static IEnumerable<GraphNode<T>> GetDescencantsRecursively(GraphNode<T> node) {
                var children = node.GetChildMembers()
                    .Concat(node.GetVariationMembers())
                    .Concat(node.GetChildrenMembers());
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
        internal static IEnumerable<GraphNode<T>> EnumerateThisAndAncestors<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            yield return graphNode;
            foreach (var ancestor in graphNode.EnumerateAncestors()) {
                yield return ancestor;
            }
        }
        internal static IEnumerable<GraphNode<T>> EnumerateThisAndDescendants<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            yield return graphNode;
            foreach (var desc in graphNode.EnumerateDescendants()) {
                yield return desc;
            }
        }
        internal static GraphEdge<T>? GetParent<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            var edge = graphNode.In.SingleOrDefault(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                                                    && (string)type == REL_ATTRVALUE_PARENT_CHILD);
            if (edge == null) return null;
            if (edge.Initial.Item is not T) throw new InvalidOperationException($"Parent of '{graphNode.Item.Id}' is not same type to it's child.");
            return edge.As<T>();
        }
        internal static bool IsRoot(this GraphNode graphNode) {
            return !graphNode.In.Any(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                                             && (string)type == REL_ATTRVALUE_PARENT_CHILD);
        }

        internal static GraphNode<EFCoreEntity> GetDbEntity(this GraphNode<Aggregate> aggregate) {
            return aggregate.In
                .Single(edge => (string)edge.Attributes[REL_ATTR_RELATION_TYPE] == REL_ATTRVALUE_AGG_2_ETT)
                .Initial
                .As<EFCoreEntity>();
        }
        internal static GraphNode<Aggregate> GetCorrespondingAggregate(this GraphNode<EFCoreEntity> dbEntity) {
            return dbEntity.Out
                .Single(edge => (string)edge.Attributes[REL_ATTR_RELATION_TYPE] == REL_ATTRVALUE_AGG_2_ETT)
                .Terminal
                .As<Aggregate>();
        }
        internal static GraphNode<AggregateInstance> GetInstanceClass(this GraphNode<Aggregate> aggregate) {
            return aggregate.In
            .Single(edge => (string)edge.Attributes[REL_ATTR_RELATION_TYPE] == REL_ATTRVALUE_AGG_2_INS)
            .Initial
            .As<AggregateInstance>();
        }
        internal static GraphNode<Aggregate> GetCorrespondingAggregate(this GraphNode<AggregateInstance> instance) {
            return instance.Out
                .Single(edge => (string)edge.Attributes[REL_ATTR_RELATION_TYPE] == REL_ATTRVALUE_AGG_2_INS)
                .Terminal
                .As<Aggregate>();
        }
        internal static GraphNode<EFCoreEntity> GetDbEntity(this GraphNode<AggregateInstance> instance) {
            return instance.GetCorrespondingAggregate().GetDbEntity();
        }
        internal static GraphNode<AggregateInstance> GetUiInstance(this GraphNode<EFCoreEntity> dbEntity) {
            return dbEntity.GetCorrespondingAggregate().GetInstanceClass();
        }

        internal static bool IsChildrenMemberOf<T>(this GraphNode<T> graphNode, GraphNode<T> parent) where T : IGraphNode {
            return graphNode.Source != null
                && graphNode.Source.IsChildren()
                && graphNode.Source.Initial == parent;
        }
        internal static bool IsChildMemberOf<T>(this GraphNode<T> graphNode, GraphNode<T> parent) where T : IGraphNode {
            return graphNode.Source != null
                && graphNode.Source.IsChild()
                && graphNode.Source.Initial == parent;
        }
        internal static bool IsVariationMemberOf<T>(this GraphNode<T> graphNode, GraphNode<T> parent) where T : IGraphNode {
            throw new NotImplementedException();
        }
        internal static bool IsRefMemberOf<T>(this GraphNode<T> graphNode, GraphNode<T> parent) where T : IGraphNode {
            return graphNode.Source != null
                && graphNode.Source.IsRef()
                && graphNode.Source.Initial == parent;
        }

        internal static IEnumerable<GraphEdge<T>> GetChildrenMembers<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.Out
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_PARENT_CHILD
                            && edge.Attributes.ContainsKey(REL_ATTR_MULTIPLE))
                .Select(edge => edge.As<T>());
        }
        internal static IEnumerable<GraphEdge<T>> GetChildMembers<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.Out
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_PARENT_CHILD
                            && !edge.Attributes.ContainsKey(REL_ATTR_MULTIPLE))
                .Select(edge => edge.As<T>());
        }
        internal static IEnumerable<GraphEdge<T>> GetVariationMembers<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            // TODO
            yield break;
        }
        internal static IEnumerable<GraphEdge<T>> GetRefMembers<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.Out
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_REFERENCE)
                .Select(edge => edge.As<T>());
        }
        internal static IEnumerable<GraphEdge<T>> GetReferrings<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.In
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_REFERENCE)
                .Select(edge => edge.As<T>());
        }

        // ----------------------------- GraphEdge extensions -----------------------------

        internal static bool IsPrimary(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_PRIMARY, out var bln) && (bool)bln;
        }
        internal static bool IsInstanceName(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_INSTANCE_NAME, out var bln) && (bool)bln;
        }
        internal static bool IsChildren(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                && graphEdge.Attributes.ContainsKey(REL_ATTR_MULTIPLE);
        }
        internal static bool IsChild(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                && !graphEdge.Attributes.ContainsKey(REL_ATTR_MULTIPLE);
        }
        internal static bool IsVariation(this GraphEdge graphEdge) {
            // TODO
            return false;
        }
        internal static bool IsRef(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_REFERENCE;
        }
    }
}
