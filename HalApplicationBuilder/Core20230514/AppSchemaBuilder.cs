using HalApplicationBuilder.Core.MemberImpl;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HalApplicationBuilder.Core20230514 {
    internal class AppSchemaBuilder {

        #region XML
        internal static bool FromXml(XDocument xDocument, out AppSchemaBuilder builder, out ICollection<string> errors) {
            if (xDocument.Root == null) throw new FormatException($"Xml doesn't have contents.");
            var schemaBuilder = new AppSchemaBuilder();
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

            var aggregates = new List<Aggregate>();
            var relations = new Dictionary<(NodeId, string, NodeId), GraphEdgeInfo>();

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
                aggregates.Add(aggregate);
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
                aggregates.Add(aggregate);
                relations[(relation.Initial, relation.RelationName, relation.Terminal)] = relation;
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
                aggregates.Add(aggregate);
                relations[(relation.Initial, relation.RelationName, relation.Terminal)] = relation;
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
                    aggregates.Add(aggregate);
                    relations[(relation.Initial, relation.RelationName, relation.Terminal)] = relation;
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
                relations[(relation.Initial, relation.RelationName, relation.Terminal)] = relation;
            }

            var aggregateDict = new Dictionary<AggregatePath, Aggregate>();
            foreach (var aggregate in aggregates) {
                aggregateDict[aggregate.Path] = aggregate; 
            }
            var duplicates = aggregates
                .GroupBy(a => a.Path)
                .Where(group => group.Count() >= 2);
            foreach (var dup in duplicates) {
                errors.Add($"Aggregate path duplicates: {dup.Key}");
            }

            if (!DirectedGraph<Aggregate>.TryCreate(aggregates, relations.Values, out var graph, out var errors1)) {
                foreach (var err in errors1) errors.Add(err);
            }

            if (errors.Any()) {
                appSchema = AppSchema.Empty();
                return false;
            }

            appSchema = new AppSchema(graph);
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

        internal const string REL_ATTR_MULTIPLE = "multiple";
        internal const string REL_ATTR_IS_PRIMARY = "is-primary";
        internal const string REL_ATTR_IS_INSTANCE_NAME = "is-instance-name";

        // ----------------------------- DirectedGraph extensions -----------------------------

        internal static IEnumerable<GraphNode<EFCoreEntity>> RootEntities(this DirectedGraph<EFCoreEntity> graph) {
            return graph.Where(entity => entity.IsRoot());
        }

        // ----------------------------- GraphNode extensions -----------------------------

        internal static GraphNode<T>? GetParent<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            var edge = graphNode.In.SingleOrDefault(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                                                && (string)type == REL_ATTRVALUE_PARENT_CHILD);
            return edge?.Initial;
        }
        internal static bool IsRoot<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.GetParent() == null;
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
            return graphNode.Out.Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                                                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                                                && edge.Attributes.ContainsKey(REL_ATTR_MULTIPLE));
        }
        internal static IEnumerable<GraphEdge<T>> GetChildMembers<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.Out.Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                                                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                                                && !edge.Attributes.ContainsKey(REL_ATTR_MULTIPLE));
        }
        internal static IEnumerable<GraphEdge<T>> GetVariationMembers<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            // TODO
            yield break;
        }
        internal static IEnumerable<GraphEdge<T>> GetRefMembers<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.Out.Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                                                && (string)type == REL_ATTRVALUE_REFERENCE);
        }
        internal static IEnumerable<GraphEdge<T>> GetReferrings<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.In.Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                                                && (string)type == REL_ATTRVALUE_REFERENCE);
        }

        // ----------------------------- GraphEdge extensions -----------------------------

        internal static bool IsPrimary<T>(this GraphEdge<T> graphEdge) where T : IGraphNode {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_PRIMARY, out var bln) && (bool)bln;
        }
        internal static bool IsInstanceName<T>(this GraphEdge<T> graphEdge) where T : IGraphNode {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_INSTANCE_NAME, out var bln) && (bool)bln;
        }
        internal static bool IsChildren<T>(this GraphEdge<T> graphEdge) where T : IGraphNode {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                && graphEdge.Attributes.ContainsKey(REL_ATTR_MULTIPLE);
        }
        internal static bool IsChild<T>(this GraphEdge<T> graphEdge) where T : IGraphNode {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                && !graphEdge.Attributes.ContainsKey(REL_ATTR_MULTIPLE);
        }
        internal static bool IsVariation<T>(this GraphEdge<T> graphEdge) where T : IGraphNode {
            // TODO
            return false;
        }
        internal static bool IsRef<T>(this GraphEdge<T> graphEdge) where T : IGraphNode {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_REFERENCE;
        }
    }
}
