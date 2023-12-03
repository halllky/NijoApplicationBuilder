using HalApplicationBuilder.Features.InstanceHandling;
using HalApplicationBuilder.Features.Util;
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
                        yield return new Schalar(aggregate, (Schalar?)schalar.Original ?? schalar, schalar.Declared) {
                            ForeignKeyOf = schalar.ForeignKeyOf,
                        };
                    } else if (parentPk is Variation variation) {
                        yield return new Variation(aggregate, (Variation?)variation.Original ?? variation, variation.Declared) {
                            ForeignKeyOf = variation.ForeignKeyOf,
                        };
                    } else if (parentPk is Ref @ref) {
                        yield return new Ref(@ref.Relation, aggregate);
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
                if (member is ValueMember valueMember
                    && valueMember.IsKey
                    && (valueMember.ForeignKeyOf == null || valueMember.ForeignKeyOf.Relation.IsPrimary())) {

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
        internal abstract class AggregateMemberBase : ValueObject {
            internal abstract GraphNode<Aggregate> Owner { get; }
            internal abstract GraphNode<Aggregate> DeclaringAggregate { get; }
            internal abstract string MemberName { get; }
            internal abstract decimal Order { get; }
            internal abstract string CSharpTypeName { get; }
            internal abstract string TypeScriptTypename { get; }

            internal IEnumerable<GraphEdge> GetFullPathEdge(GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
                var skip = since != null;
                foreach (var edge in Owner.PathFromEntry()) {
                    if (until != null && edge.Source?.As<Aggregate>() == until) yield break;
                    if (skip && edge.Source?.As<Aggregate>() == since) skip = false;
                    if (skip) continue;
                    yield return edge;
                }
            }
            internal virtual IEnumerable<string> GetFullPath(GraphNode<Aggregate>? since = null, GraphNode<Aggregate>? until = null) {
                foreach (var edge in GetFullPathEdge(since, until)) {
                    if (edge.Source == edge.Terminal
                        && edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, out var type)
                        && (string)type == DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD) {
                        // 子から親に向かって辿る場合
                        // ※自動生成されたソース中にこれが出現することはありえないはず
                        yield return "__親__";
                    } else {
                        yield return edge.RelationName;
                    }
                }
                yield return MemberName;
            }
            public override string ToString() {
                return GetFullPath().Join(".");
            }
            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return Owner;
                yield return DeclaringAggregate;
                yield return MemberName;
            }
        }
        internal abstract class ValueMember : AggregateMemberBase {
            protected ValueMember(ValueMember? original, ValueMember? declared) {
                Original = original;
                Declared = declared ?? this;
            }

            internal abstract IReadOnlyMemberOptions Options { get; }

            private string? _memberName;
            internal sealed override string MemberName {
                get {
                    if (_memberName == null) {
                        // 参照先のリレーション
                        var relationPrefix = IsKeyOfRefTarget
                            ? $"{ForeignKeyOf!.Relation.RelationName}_"
                            : string.Empty;
                        // 親の主キーの継承
                        var parentPrefix = IsKeyOfAncestor
                            ? $"{Original!.Owner.Item.ClassName}_"
                            : string.Empty;
                        // 自身の名前
                        var name = Original == null
                            ? Options.MemberName
                            : Original.MemberName;

                        _memberName
                            = relationPrefix
                            + parentPrefix
                            + name;
                    }
                    return _memberName;
                }
            }

            internal sealed override GraphNode<Aggregate> DeclaringAggregate => Original?.DeclaringAggregate ?? Owner;
            internal sealed override string CSharpTypeName => Options.MemberType.GetCSharpTypeName();
            internal sealed override string TypeScriptTypename => Options.MemberType.GetTypeScriptTypeName();

            internal bool IsKey => Options.IsKey;
            internal bool IsDisplayName => Options.IsDisplayName
                                        || (Owner.Item.UseKeyInsteadOfName && Options.IsKey); // 集約中に名前が無い場合はキーを名前のかわりに使う
            internal bool IsKeyOfAncestor => Original != null && Owner.EnumerateAncestors().Select(x=>x.Initial).Contains(Original.Owner);
            internal bool IsKeyOfRefTarget => ForeignKeyOf != null;

            internal ValueMember? Original { get; }
            internal ValueMember Declared { get; }
            internal Ref? ForeignKeyOf { get; init; }

            internal virtual DbColumn GetDbColumn() {
                return new DbColumn {
                    Owner = Owner.As<IEFCoreEntity>(),
                    Options = Options.Clone(opt => {
                        opt.MemberName = MemberName;
                        if (IsKeyOfRefTarget) {
                            opt.IsKey = ForeignKeyOf!.Relation.IsPrimary();
                        }
                    }),
                };
            }
        }
        internal abstract class RelationMember : AggregateMemberBase {
            internal abstract GraphEdge<Aggregate> Relation { get; }

            internal override GraphNode<Aggregate> Owner => Relation.Initial;
            internal override GraphNode<Aggregate> DeclaringAggregate => Relation.Initial;
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
            internal Schalar(GraphNode<AggregateMemberNode> aggregateMemberNode) : base(null, null) {
                GraphNode = aggregateMemberNode;
                Owner = aggregateMemberNode.Source!.Initial.As<Aggregate>();
            }
            internal Schalar(GraphNode<Aggregate> owner, Schalar original, ValueMember declared) : base(original, declared) {
                GraphNode = original.GraphNode;
                Owner = owner;
            }
            internal GraphNode<AggregateMemberNode> GraphNode { get; }
            internal override GraphNode<Aggregate> Owner { get; }

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
            internal Variation(VariationGroup<Aggregate> group) : base(null, null) {
                VariationGroup = group;
                Options = new MemberOptions {
                    MemberName = group.GroupName,
                    MemberType = new AggregateMemberTypes.VariationSwitch(),
                    IsKey = group.IsPrimary,
                    IsDisplayName = group.IsInstanceName,
                    IsRequired = group.RequiredAtDB,
                    InvisibleInGui = false,
                };
                Owner = group.Owner;
            }
            internal Variation(GraphNode<Aggregate> owner, Variation original, ValueMember declared) : base(original, declared) {
                VariationGroup = original.VariationGroup;
                Options = original.Options;
                Owner = owner;
            }

            internal VariationGroup<Aggregate> VariationGroup { get; }
            internal override IReadOnlyMemberOptions Options { get; }
            internal override GraphNode<Aggregate> Owner { get; }
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
                        yield return new Schalar(Relation.Initial, schalar, schalar.Declared) {
                            ForeignKeyOf = this,
                        };

                    } else if (fk is Variation variation) {
                        yield return new Variation(Relation.Initial, variation, variation.Declared) {
                            ForeignKeyOf = this,
                        };
                    }
                }
            }
        }
        #endregion MEMBER IMPLEMEMT
    }
}
