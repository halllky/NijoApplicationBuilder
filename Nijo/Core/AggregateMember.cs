using Nijo.Features.Storing;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core {

    internal class AggregateMemberNode : IReadOnlyMemberOptions, IGraphNode {
        public required NodeId Id { get; init; }
        public required string MemberName { get; init; }
        public required IAggregateMemberType MemberType { get; init; }
        public required bool IsKey { get; init; }
        public required bool IsDisplayName { get; init; }
        public required bool IsNameLike { get; init; }
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
            var parentEdge = aggregate.GetParent();
            if (parentEdge != null) {
                var parent = new Parent(parentEdge, aggregate);
                yield return parent;
                foreach (var parentPK in parent.GetForeignKeys()) yield return parentPK;
            }

            var memberEdges = aggregate.Out.Where(edge =>
                (string)edge.Attributes[DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE] == DirectedEdgeExtensions.REL_ATTRVALUE_HAVING);
            foreach (var edge in memberEdges) {
                yield return new Schalar(edge.Terminal.As<AggregateMemberNode>());
            }

            var childrenEdges = aggregate.Out.Where(edge =>
                edge.IsParentChild()
                && edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_MULTIPLE, out var isArray)
                && (bool)isArray);
            foreach (var edge in childrenEdges) {
                yield return new Children(edge.As<Aggregate>());
            }

            var childEdges = aggregate.Out.Where(edge =>
                edge.IsParentChild()
                && (!edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_MULTIPLE, out var isArray) || (bool)isArray == false)
                && (!edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUPNAME, out var groupName) || (string)groupName == string.Empty));
            foreach (var edge in childEdges) {
                yield return new Child(edge.As<Aggregate>());
            }

            var variationGroups = aggregate.Out
                .Where(edge =>
                    edge.IsParentChild()
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

                } else if (member is Parent parent) {
                    yield return parent;
                }
            }
        }
        internal static IEnumerable<AggregateMemberBase> GetNames(this GraphNode<Aggregate> aggregate) {
            var useKeyAsName = true;

            foreach (var member in aggregate.GetMembers()) {
                if (member is ValueMember valueMember && valueMember.IsDisplayName) {
                    yield return valueMember;
                    useKeyAsName = false;

                } else if (member is Ref refMember && refMember.Relation.IsInstanceName()) {
                    yield return refMember;
                    useKeyAsName = false;

                } else if (member is Parent parent) {
                    yield return parent;
                }
            }

            // 名前が定義されていない集約の場合はキーを表示名に使う
            if (useKeyAsName) {
                foreach (var key in aggregate.GetKeys()) {
                    if (key is ValueMember) {
                        yield return key;

                    } else if (key is Ref) {
                        yield return key;

                    } else if (key is Parent) {
                        // 親は最初のループで返しているので無視
                    }
                }
            }
        }

        internal static IEnumerable<NavigationProperty> GetNavigationProperties(this GraphNode<Aggregate> aggregate) {
            foreach (var member in aggregate.GetMembers()) {
                if (member is not RelationMember relationMember) continue;
                yield return relationMember.GetNavigationProperty();
            }

            foreach (var refered in aggregate.GetReferedEdges()) {
                if (!refered.Initial.IsStored()) continue;
                yield return new NavigationProperty(refered);
            }
        }


        #region MEMBER BASE
        internal const string PARENT_PROPNAME = "Parent";

        internal abstract class AggregateMemberBase : ValueObject {
            internal abstract GraphNode<Aggregate> Owner { get; }
            /// <summary>
            /// TODO: ParentだとDeclaringAggregateは子ではなく親になるのが違和感。
            /// そもそもDeclaringAggregateはValueMemberにのみ存在する概念と考えるのが自然な気がする。
            /// </summary>
            internal abstract GraphNode<Aggregate> DeclaringAggregate { get; }
            internal abstract string MemberName { get; }
            internal abstract decimal Order { get; }
            internal abstract string CSharpTypeName { get; }
            internal abstract string TypeScriptTypename { get; }

            public override string ToString() {
                return this.GetFullPath().Join(".");
            }
        }
        internal abstract class ValueMember : AggregateMemberBase {
            protected ValueMember(InheritInfo? inherits) {
                Inherits = inherits;
            }

            internal abstract IReadOnlyMemberOptions Options { get; }

            private string? _membername;
            internal sealed override string MemberName {
                get {
                    return _membername ??= Inherits == null
                        ? Options.MemberName
                        : $"{Inherits.Relation.RelationName}_{Inherits.Member.MemberName}";
                }
            }
            internal sealed override GraphNode<Aggregate> DeclaringAggregate => Inherits?.Member.DeclaringAggregate ?? Owner;
            internal sealed override string CSharpTypeName => Options.MemberType.GetCSharpTypeName();
            internal sealed override string TypeScriptTypename => Options.MemberType.GetTypeScriptTypeName();

            internal bool IsKey {
                get {
                    if (Inherits?.Relation.IsParentChild() == true) {
                        return true;

                    } else if (Inherits?.Relation.IsRef() == true) {
                        return Inherits.Relation.IsPrimary();

                    } else {
                        return Options.IsKey;
                    }
                }
            }
            internal bool IsDisplayName => Options.IsDisplayName;

            internal bool IsRequired {
                get {
                    if (Inherits?.Relation.IsParentChild() == true) {
                        return true;

                    } else if (Inherits?.Relation.IsRef() == true) {
                        return Inherits.Relation.IsPrimary()
                            || Inherits.Relation.IsRequired();

                    } else {
                        return Options.IsRequired;
                    }
                }
            }

            /// <summary>
            /// このメンバーが親や参照先のメンバーを継承したものである場合はこのプロパティに値が入る。
            /// </summary>
            internal InheritInfo? Inherits { get; }
            /// <summary>
            /// このメンバーが親や参照先のメンバーを継承したものである場合、その一番大元のメンバー。
            /// </summary>
            internal ValueMember Declared => Inherits?.Member.Declared ?? this;

            internal class InheritInfo {
                internal required GraphEdge<Aggregate> Relation { get; init; }
                internal required ValueMember Member { get; init; }
            }
        }

        internal abstract class RelationMember : AggregateMemberBase {
            internal abstract GraphEdge<Aggregate> Relation { get; }
            internal abstract GraphNode<Aggregate> MemberAggregate { get; }

            internal override GraphNode<Aggregate> Owner => Relation.Initial;
            internal override GraphNode<Aggregate> DeclaringAggregate => Relation.Initial;
            internal override string MemberName => Relation.RelationName;
            internal override decimal Order => Relation.GetMemberOrder();

            internal NavigationProperty GetNavigationProperty() {
                return new NavigationProperty(Relation);
            }

            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return Owner;
                yield return Relation;
            }
        }
        #endregion MEMBER BASE


        #region MEMBER IMPLEMEMT
        internal class Schalar : ValueMember {
            internal Schalar(GraphNode<AggregateMemberNode> aggregateMemberNode) : base(null) {
                GraphNode = aggregateMemberNode;
                Owner = aggregateMemberNode.Source!.Initial.As<Aggregate>();
                Options = GraphNode.Item;
            }
            internal Schalar(GraphNode<Aggregate> owner, InheritInfo inherits, IReadOnlyMemberOptions options) : base(inherits) {
                GraphNode = ((Schalar)inherits.Member).GraphNode;
                Owner = owner;
                Options = options;
            }
            internal GraphNode<AggregateMemberNode> GraphNode { get; }
            internal override GraphNode<Aggregate> Owner { get; }

            internal override IReadOnlyMemberOptions Options { get; }
            internal override decimal Order => GraphNode.Source!.GetMemberOrder();

            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return Owner;
                yield return GraphNode;
                yield return Inherits?.Relation;
                yield return Inherits?.Member;
            }
        }

        internal class Children : RelationMember {
            internal Children(GraphEdge<Aggregate> edge) {
                Relation = edge;
            }
            internal override GraphEdge<Aggregate> Relation { get; }
            internal GraphNode<Aggregate> ChildrenAggregate => MemberAggregate;
            /// <summary><see cref="ChildrenAggregate"/>と全く同じもの。より名前がわかりやすい左記の利用を推奨。</summary>
            internal override GraphNode<Aggregate> MemberAggregate => Relation.Terminal;
            internal override string CSharpTypeName => $"List<{new DataClassForSave(Relation.Terminal).CsClassName}>";
            internal override string TypeScriptTypename => $"{new DataClassForSave(Relation.Terminal).TsTypeName}[]";
        }

        internal class Child : RelationMember {
            internal Child(GraphEdge<Aggregate> edge) {
                Relation = edge;
            }
            internal override GraphEdge<Aggregate> Relation { get; }
            internal GraphNode<Aggregate> ChildAggregate => MemberAggregate;
            /// <summary><see cref="ChildAggregate"/>と全く同じもの。より名前がわかりやすい左記の利用を推奨。</summary>
            internal override GraphNode<Aggregate> MemberAggregate => Relation.Terminal;
            internal override string CSharpTypeName => new DataClassForSave(Relation.Terminal).CsClassName;
            internal override string TypeScriptTypename => new  DataClassForSave(Relation.Terminal).TsTypeName;
        }

        internal class Variation : ValueMember {
            internal Variation(VariationGroup<Aggregate> group) : base(null) {
                VariationGroup = group;
                Options = new MemberOptions {
                    MemberName = group.GroupName,
                    MemberType = new AggregateMemberTypes.VariationSwitch(group),
                    IsKey = group.IsPrimary,
                    IsDisplayName = group.IsInstanceName,
                    IsNameLike = group.IsNameLike,
                    IsRequired = group.RequiredAtDB,
                    InvisibleInGui = false,
                };
                Owner = group.Owner;
            }
            internal Variation(GraphNode<Aggregate> owner, InheritInfo inherits) : base(inherits) {
                VariationGroup = ((Variation)inherits.Member).VariationGroup;
                Options = inherits.Member.Options;
                Owner = owner;
            }

            internal VariationGroup<Aggregate> VariationGroup { get; }
            internal override IReadOnlyMemberOptions Options { get; }
            internal override GraphNode<Aggregate> Owner { get; }
            internal override decimal Order => VariationGroup.MemberOrder;

            internal string CsEnumType => $"E_{VariationGroup.GroupName}";

            internal IEnumerable<VariationItem> GetGroupItems() {
                foreach (var kv in VariationGroup.VariationAggregates) {
                    yield return new VariationItem(this, kv.Key, kv.Value);
                }
            }

            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return Owner;
                yield return VariationGroup.GroupName;
                yield return Inherits?.Relation;
                yield return Inherits?.Member;
            }
        }

        internal class VariationItem : RelationMember {
            internal VariationItem(Variation group, string key, GraphEdge<Aggregate> edge) {
                Relation = edge;
                Group = group;
                Key = key;
            }

            internal override GraphEdge<Aggregate> Relation { get; }
            internal GraphNode<Aggregate> VariationAggregate => MemberAggregate;
            /// <summary><see cref="VariationAggregate"/>と全く同じもの。より名前がわかりやすい左記の利用を推奨。</summary>
            internal override GraphNode<Aggregate> MemberAggregate => Relation.Terminal;
            internal Variation Group { get; }
            internal string Key { get; }

            internal override string CSharpTypeName => new DataClassForSave(Relation.Terminal).CsClassName;
            internal override string TypeScriptTypename => new DataClassForSave(Relation.Terminal).TsTypeName;

            /// <summary>この集約のタイプを表すTypeScriptの区分値</summary>
            internal string TsValue => Relation.RelationName;
        }

        internal class Ref : RelationMember {
            internal Ref(GraphEdge<Aggregate> edge, GraphNode<Aggregate>? owner = null) {
                Relation = edge;
                Owner = owner ?? base.Owner;
            }
            internal override GraphNode<Aggregate> Owner { get; }
            internal override GraphEdge<Aggregate> Relation { get; }
            internal GraphNode<Aggregate> RefTo => MemberAggregate;
            /// <summary><see cref="RefTo"/>と全く同じもの。より名前がわかりやすい左記の利用を推奨。</summary>
            internal override GraphNode<Aggregate> MemberAggregate => Relation.Terminal;
            internal override string CSharpTypeName => new DataClassForSaveRefTarget(Relation.Terminal).CSharpClassName;
            internal override string TypeScriptTypename => new DataClassForSaveRefTarget(Relation.Terminal).TypeScriptTypeName;

            internal IEnumerable<ValueMember> GetForeignKeys() {
                foreach (var fk in Relation.Terminal.GetKeys()) {
                    if (fk is Schalar schalar) {
                        yield return new Schalar(
                            Relation.Initial,
                            new ValueMember.InheritInfo { Relation = Relation, Member = schalar, },
                            schalar.GraphNode.Item);

                    } else if (fk is Variation variation) {
                        yield return new Variation(
                            Relation.Initial,
                            new ValueMember.InheritInfo { Relation = Relation, Member = variation });
                    }
                }
            }
        }
        internal class Parent : RelationMember {
            internal Parent(GraphEdge<Aggregate> edge, GraphNode<Aggregate>? owner = null) {
                Relation = edge;
                Owner = owner ?? base.Owner;
            }
            internal override GraphEdge<Aggregate> Relation { get; }
            internal GraphNode<Aggregate> ParentAggregate => MemberAggregate;
            /// <summary><see cref="ParentAggregate"/>と全く同じもの。より名前がわかりやすい左記の利用を推奨。</summary>
            internal override GraphNode<Aggregate> MemberAggregate => Relation.Initial;
            internal override GraphNode<Aggregate> Owner { get; }
            internal override string MemberName => PARENT_PROPNAME;

            internal override string CSharpTypeName => new DataClassForSaveRefTarget(Relation.Initial).CSharpClassName;
            internal override string TypeScriptTypename => new DataClassForSaveRefTarget(Relation.Initial).TypeScriptTypeName;

            internal IEnumerable<ValueMember> GetForeignKeys() {
                foreach (var parentPk in Relation.Initial.GetKeys()) {
                    if (parentPk is Schalar schalar) {
                        yield return new Schalar(
                            Relation.Terminal,
                            new ValueMember.InheritInfo { Relation = Relation, Member = schalar },
                            schalar.GraphNode.Item);

                    } else if (parentPk is Variation variation) {
                        yield return new Variation(
                            Relation.Terminal,
                            new ValueMember.InheritInfo { Relation = Relation, Member = variation });
                    }
                }
            }
        }
        #endregion MEMBER IMPLEMEMT
    }
}
