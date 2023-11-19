using HalApplicationBuilder.CodeRendering.InstanceHandling;
using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.DotnetEx;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HalApplicationBuilder.Core {

    internal class AggregateMemberNode : IReadOnlyMemberOptions, IGraphNode {
        public required NodeId Id { get; init; }
        public required string MemberName { get; init; }
        public required IAggregateMemberType MemberType { get; init; }
        public required bool IsKey { get; init; }
        public required bool IsDisplayName { get; init; }
        public required bool IsRequired { get; init; }
        public required bool InvisibleInGui { get; init; }

        public override string ToString() => Id.Value;
    }


    internal static class AggregateMember {

        internal static IOrderedEnumerable<AggregateMemberBase> GetMembers(this GraphNode<Aggregate> aggregate) {
            return aggregate
                .GetNonOrderedMembers()
                .OrderBy(member => member.Order);
        }
        private static IEnumerable<AggregateMemberBase> GetNonOrderedMembers(this GraphNode<Aggregate> aggregate) {
            var parent = aggregate.GetParent();
            if (parent != null) {
                var parentPKs = parent.Initial
                    .GetKeys()
                    .OfType<ValueMember>()
                    .ToArray();
                foreach (var parentPk in parent.Initial.GetKeys()) {
                    if (parentPk is Schalar schalar) {
                        yield return new Schalar(schalar.GraphNode, owner: aggregate);

                    } else if (parentPk is Variation variation) {
                        yield return new Variation(variation.VariationGroup, owner: aggregate);

                    } else if (parentPk is Ref @ref) {
                        yield return new Ref(@ref.Relation, owner: aggregate);
                    }
                }
            }

            var memberEdges = aggregate.Out.Where(edge =>
                (string)edge.Attributes[DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE] == DirectedEdgeExtensions.REL_ATTRVALUE_HAVING);
            foreach (var edge in memberEdges) {
                yield return new Schalar(edge.Terminal.As<AggregateMemberNode>());
            }

            var childrenEdges = aggregate.Out.Where(edge =>
                edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD
                && edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_MULTIPLE, out var isArray)
                && (bool)isArray);
            foreach (var edge in childrenEdges) {
                yield return new Children(edge.As<Aggregate>());
            }

            var childEdges = aggregate.Out.Where(edge =>
                edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD
                && (!edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_MULTIPLE, out var isArray) || (bool)isArray == false)
                && (!edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUPNAME, out var groupName) || (string)groupName == string.Empty));
            foreach (var edge in childEdges) {
                yield return new Child(edge.As<Aggregate>());
            }

            var variationGroups = aggregate.Out
                .Where(edge =>
                    edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, out var type)
                    && (string)type == DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD
                    && (!edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_MULTIPLE, out var isArray) || (bool)isArray == false)
                    && edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUPNAME, out var groupName)
                    && (string)groupName! != string.Empty)
                .GroupBy(edge => (string)edge.Attributes[DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUPNAME])
                .Select(group => new VariationGroup<Aggregate> {
                    GroupName = group.Key,
                    VariationAggregates = group.ToDictionary(
                        edge => (string)edge.Attributes[DirectedEdgeExtensions.REL_ATTR_VARIATIONSWITCH],
                        edge => edge.As<Aggregate>()),
                    MemberOrder = group.First().GetMemberOrder(),
                });
            foreach (var group in variationGroups) {
                var variationGroup = new Variation(group);
                yield return variationGroup;
                foreach (var item in variationGroup.GetGroupItems()) yield return item;
            }

            var refEdges = aggregate.Out.Where(edge =>
                edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == DirectedEdgeExtensions.REL_ATTRVALUE_REFERENCE);
            foreach (var edge in refEdges) {
                var refMember = new Ref(edge.As<Aggregate>());
                yield return refMember;
                foreach (var refPK in refMember.GetForeignKeys()) yield return refPK;
            }
        }

        internal static IEnumerable<AggregateMemberBase> GetKeys(this GraphNode<Aggregate> aggregate) {
            foreach (var member in aggregate.GetMembers()) {
                if (member is ValueMember valueMember && valueMember.IsKey) {
                    yield return valueMember;

                } else if (member is Ref refMember && refMember.Relation.IsPrimary()) {
                    yield return refMember;
                }
            }
        }

        internal static IEnumerable<NavigationProperty> GetNavigationProperties(this GraphNode<Aggregate> aggregate) {
            var parent = aggregate.GetParent();
            if (parent != null) {
                yield return new NavigationProperty(parent);
            }

            foreach (var member in aggregate.GetMembers()) {
                if (member is not RelationMember relationMember) continue;
                yield return relationMember.GetNavigationProperty();
            }

            foreach (var refered in aggregate.GetReferedEdges()) {
                yield return new NavigationProperty(refered);
            }
        }


        #region MEMBER BASE
        internal abstract class AggregateMemberBase {
            internal abstract GraphNode<Aggregate> Owner { get; }
            internal abstract GraphNode<Aggregate> Declaring { get; }
            internal abstract string MemberName { get; }
            internal abstract decimal Order { get; }
            internal abstract string CSharpTypeName { get; }
            internal abstract string TypeScriptTypename { get; }

            internal virtual IEnumerable<string> GetFullPath(GraphNode<Aggregate>? since = null) {
                var skip = since != null;
                var before = Owner;
                foreach (var edge in Declaring.PathFromEntry()) {
                    if (skip && edge.Source?.As<Aggregate>() == since) skip = false;
                    if (skip) continue;

                    // PathFromEntryの列挙中に有向グラフの矢印の先から根本に向かって辿るパターンは
                    // 参照先の集約の子から親に向かうパターンだけであり、
                    // その場合は子の主キーに親の主キーが含まれているためGetFullPathでは列挙してほしくないため、スキップ
                    if (before != edge.Terminal.As<Aggregate>()) {
                        yield return edge.RelationName;
                    }

                    before = before == edge.Terminal.As<Aggregate>()
                        ? edge.Initial.As<Aggregate>()
                        : edge.Terminal.As<Aggregate>();
                }
                yield return MemberName;
            }
            public override string ToString() {
                return GetFullPath().Join(".");
            }
        }
        internal abstract class ValueMember : AggregateMemberBase {
            internal abstract IReadOnlyMemberOptions Options { get; }

            internal sealed override string MemberName => Options.MemberName;
            internal sealed override string CSharpTypeName => Options.MemberType.GetCSharpTypeName();
            internal sealed override string TypeScriptTypename => Options.MemberType.GetTypeScriptTypeName();

            internal bool IsKey => Options.IsKey;
            internal bool IsDisplayName => Options.IsDisplayName
                                        || (!Owner.Item.HasNameMember && Options.IsKey); // 集約中に名前が無い場合はキーを名前のかわりに使う

            internal DbColumn GetDbColumn() {
                return new DbColumn {
                    Owner = Owner.As<IEFCoreEntity>(),
                    Options = Options,
                };
            }
        }
        internal abstract class RelationMember : AggregateMemberBase {
            internal abstract GraphEdge<Aggregate> Relation { get; }

            internal override GraphNode<Aggregate> Owner => Relation.Initial;
            internal override GraphNode<Aggregate> Declaring => Relation.Initial;
            internal override string MemberName => Relation.RelationName;
            internal GraphNode<Aggregate> MemberAggregate => Relation.Terminal;
            internal override decimal Order => Relation.GetMemberOrder();

            internal NavigationProperty GetNavigationProperty() {
                return new NavigationProperty(Relation);
            }
        }
        #endregion MEMBER BASE


        #region MEMBER IMPLEMEMT
        internal class Schalar : ValueMember {
            internal Schalar(GraphNode<AggregateMemberNode> aggregateMemberNode, GraphNode<Aggregate>? owner = null) {
                GraphNode = aggregateMemberNode;

                Owner = owner ?? aggregateMemberNode.Source!.Initial.As<Aggregate>();
                Declaring = aggregateMemberNode.Source!.Initial.As<Aggregate>();
            }
            internal GraphNode<AggregateMemberNode> GraphNode { get; }
            internal override GraphNode<Aggregate> Owner { get; }
            internal override GraphNode<Aggregate> Declaring { get; }

            internal override IReadOnlyMemberOptions Options => GraphNode.Item;
            internal override decimal Order => GraphNode.Source!.GetMemberOrder();
        }

        internal class Children : RelationMember {
            internal Children(GraphEdge<Aggregate> edge) {
                Relation = edge;
            }
            internal override GraphEdge<Aggregate> Relation { get; }
            internal override string CSharpTypeName => $"List<{Relation.Terminal.Item.ClassName}>";
            internal override string TypeScriptTypename => $"{Relation.Terminal.Item.TypeScriptTypeName}[]";
        }

        internal class Child : RelationMember {
            internal Child(GraphEdge<Aggregate> edge) {
                Relation = edge;
            }
            internal override GraphEdge<Aggregate> Relation { get; }
            internal override string CSharpTypeName => Relation.Terminal.Item.ClassName;
            internal override string TypeScriptTypename => Relation.Terminal.Item.TypeScriptTypeName;
        }

        internal class Variation : ValueMember {
            internal Variation(VariationGroup<Aggregate> group, GraphNode<Aggregate>? owner = null) {
                VariationGroup = group;

                Options = new MemberOptions {
                    MemberName = group.GroupName,
                    MemberType = new AggregateMemberTypes.VariationSwitch(),
                    IsKey = group.IsPrimary,
                    IsDisplayName = group.IsInstanceName,
                    IsRequired = group.RequiredAtDB,
                    InvisibleInGui = false,
                };

                Owner = owner ?? group.Owner;
                Declaring = group.Owner;
            }
            internal VariationGroup<Aggregate> VariationGroup { get; }
            internal override IReadOnlyMemberOptions Options { get; }
            internal override GraphNode<Aggregate> Owner { get; }
            internal override GraphNode<Aggregate> Declaring { get; }
            internal override decimal Order => VariationGroup.MemberOrder;

            internal IEnumerable<VariationItem> GetGroupItems() {
                foreach (var kv in VariationGroup.VariationAggregates) {
                    yield return new VariationItem(this, kv.Key, kv.Value);
                }
            }
        }

        internal class VariationItem : RelationMember {
            internal VariationItem(Variation group, string key, GraphEdge<Aggregate> edge) {
                Relation = edge;
                Group = group;
                Key = key;
            }

            internal override GraphEdge<Aggregate> Relation { get; }
            internal Variation Group { get; }
            internal string Key { get; }

            internal override string CSharpTypeName => Relation.Terminal.Item.ClassName;
            internal override string TypeScriptTypename => Relation.Terminal.Item.TypeScriptTypeName;
        }

        internal class Ref : RelationMember {
            internal Ref(GraphEdge<Aggregate> edge, GraphNode<Aggregate>? owner = null) {
                Relation = edge;
                Owner = owner ?? base.Owner;
            }
            internal override GraphNode<Aggregate> Owner { get; }
            internal override GraphEdge<Aggregate> Relation { get; }
            internal override string CSharpTypeName => new RefTargetKeyName(Relation.Terminal).CSharpClassName;
            internal override string TypeScriptTypename => new RefTargetKeyName(Relation.Terminal).TypeScriptTypeName;

            internal IEnumerable<ValueMember> GetForeignKeys() {
                foreach (var fk in Relation.Terminal.GetKeys()) {
                    if (fk is Schalar schalar) {
                        yield return new Schalar(schalar.GraphNode, Relation.Initial);

                    } else if (fk is Variation variation) {
                        yield return new Variation(variation.VariationGroup, Relation.Initial);
                    }
                }
            }
        }
        #endregion MEMBER IMPLEMEMT
    }
}
