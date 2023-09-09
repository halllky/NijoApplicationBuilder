using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HalApplicationBuilder.Core {

    internal class AggregateMemberNode : IGraphNode {
        public required NodeId Id { get; init; }
        internal required string Name { get; init; }
        internal required IAggregateMemberType Type { get; init; }
        internal required bool IsPrimary { get; init; }
        internal required bool IsInstanceName { get; init; }
        internal required bool Optional { get; init; }

        public override string ToString() => Id.Value;
    }


    internal static class AggregateMember {

        internal static IEnumerable<AggregateMemberBase> GetMembers(this GraphNode<Aggregate> aggregate) {
            var parent = aggregate.GetParent();
            if (parent != null) {
                foreach (var parentPK in parent.Initial.GetKeyMembers()) {
                    yield return new ParentPK(aggregate, parentPK);
                }
            }
            foreach (var member in aggregate.GetMemberNodes()) {
                yield return new Schalar(member);
            }
            foreach (var edge in aggregate.GetChildrenEdges()) {
                yield return new Children(edge);
            }
            foreach (var edge in aggregate.GetChildEdges()) {
                yield return new Child(edge);
            }
            foreach (var group in aggregate.GetVariationGroups()) {
                var variationGroup = new Variation(group);
                yield return variationGroup;
                foreach (var item in variationGroup.GetGroupItems()) yield return item;
            }
            foreach (var edge in aggregate.GetRefEdge()) {
                var refMember = new Ref(edge);
                yield return refMember;
                foreach (var refPK in refMember.GetRefTargetKeys()) yield return refPK;
            }
        }
        internal static IEnumerable<ValueMember> GetKeyMembers(this GraphNode<Aggregate> aggregate) {
            return aggregate
                .GetMembers()
                .OfType<ValueMember>()
                .Where(member => member.IsPrimary);
        }
        internal static IEnumerable<ValueMember> GetInstanceNameMembers(this GraphNode<Aggregate> aggregate) {
            var nameMembers = aggregate
                .GetMembers()
                .OfType<ValueMember>()
                .Where(member => member.IsInstanceName)
                .ToArray();
            // name属性のメンバーが無い場合はキーを表示名称にする
            return nameMembers.Any()
                ? nameMembers
                : aggregate.GetKeyMembers();
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

        [Obsolete]
        internal static IEnumerable<Schalar> GetSchalarProperties(this GraphNode<Aggregate> aggregate) {
            return aggregate
                .GetMembers()
                .Where(prop => prop is Schalar)
                .Cast<Schalar>();
        }
        [Obsolete]
        internal static IEnumerable<Variation> GetVariationSwitchProperties(this GraphNode<Aggregate> aggregate) {
            return aggregate
                .GetMembers()
                .Where(prop => prop is Variation)
                .Cast<Variation>();
        }


        internal const string BASE_CLASS_NAME = "AggregateInstanceBase";
        internal const string TO_DB_ENTITY_METHOD_NAME = "ToDbEntity";
        internal const string FROM_DB_ENTITY_METHOD_NAME = "FromDbEntity";


        #region MEMBER BASE
        internal abstract class AggregateMemberBase : ValueObject {
            internal abstract GraphNode<Aggregate> Owner { get; }
            internal abstract string PropertyName { get; }
            internal abstract string CSharpTypeName { get; }
            internal abstract string TypeScriptTypename { get; }

            internal virtual IEnumerable<string> GetFullPath(GraphNode<Aggregate>? since = null) {
                var skip = since != null;
                foreach (var edge in Owner.PathFromEntry()) {
                    if (skip && edge.Source?.As<Aggregate>() == since) skip = false;
                    if (skip) continue;
                    yield return edge.RelationName;
                }
                yield return PropertyName;
            }

            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return Owner;
                yield return PropertyName;
            }
            public override string ToString() {
                return GetFullPath().Join(".");
            }
        }
        internal abstract class ValueMember : AggregateMemberBase {
            internal abstract bool IsPrimary { get; }
            internal abstract bool IsInstanceName { get; }
            internal abstract bool RequiredAtDB { get; }
            internal abstract IAggregateMemberType MemberType { get; }

            internal abstract DbColumn.DbColumnBase GetDbColumn();
        }
        internal abstract class RelationMember : AggregateMemberBase {
            internal abstract GraphNode<Aggregate> MemberAggregate { get; }
            internal abstract NavigationProperty GetNavigationProperty();
        }
        #endregion MEMBER BASE


        #region MEMBER IMPLEMEMT
        internal class Schalar : ValueMember {
            internal Schalar(GraphNode<AggregateMemberNode> aggregateMemberNode) {
                _node = aggregateMemberNode;
            }
            private readonly GraphNode<AggregateMemberNode> _node;

            internal override GraphNode<Aggregate> Owner => _node.Source!.Initial.As<Aggregate>();
            internal override string PropertyName => _node.Item.Name;
            internal override string CSharpTypeName => _node.Item.Type.GetCSharpTypeName();
            internal override string TypeScriptTypename => _node.Item.Type.GetTypeScriptTypeName();

            internal override bool IsPrimary => _node.Item.IsPrimary;
            internal override bool IsInstanceName => _node.Item.IsInstanceName;
            internal override bool RequiredAtDB => _node.Item.IsPrimary || !_node.Item.Optional;
            internal override IAggregateMemberType MemberType => _node.Item.Type;

            internal override DbColumn.DbColumnBase GetDbColumn() {
                return new DbColumn.AggregateMemberColumn(this);
            }
        }

        internal class Children : RelationMember {
            internal Children(GraphEdge<Aggregate> edge) {
                _edge = edge;
            }
            private readonly GraphEdge<Aggregate> _edge;

            internal override GraphNode<Aggregate> Owner => _edge.Initial;
            internal override string PropertyName => _edge.RelationName;
            internal override string CSharpTypeName => $"List<{_edge.Terminal.Item.ClassName}>";
            internal override string TypeScriptTypename => $"{_edge.Terminal.Item.TypeScriptTypeName}[]";
            internal override GraphNode<Aggregate> MemberAggregate => _edge.Terminal;
            internal override NavigationProperty GetNavigationProperty() {
                return new NavigationProperty(_edge);
            }
        }

        internal class Child : RelationMember {
            internal Child(GraphEdge<Aggregate> edge) {
                _edge = edge;
            }
            private readonly GraphEdge<Aggregate> _edge;

            internal override GraphNode<Aggregate> Owner => _edge.Initial;
            internal override string PropertyName => _edge.RelationName;
            internal override string CSharpTypeName => _edge.Terminal.Item.ClassName;
            internal override string TypeScriptTypename => _edge.Terminal.Item.TypeScriptTypeName;
            internal override GraphNode<Aggregate> MemberAggregate => _edge.Terminal;

            internal override NavigationProperty GetNavigationProperty() {
                return new NavigationProperty(_edge);
            }
        }

        internal class Variation : ValueMember {
            internal Variation(VariationGroup<Aggregate> group) {
                _group = group;
            }
            private readonly VariationGroup<Aggregate> _group;

            internal override GraphNode<Aggregate> Owner => _group.Owner;
            internal override string PropertyName => _group.GroupName;
            internal override string CSharpTypeName => MemberType.GetCSharpTypeName();
            internal override string TypeScriptTypename => MemberType.GetTypeScriptTypeName();

            internal override bool IsPrimary => false; // TODO
            internal override bool IsInstanceName => false; // TODO
            internal override bool RequiredAtDB => false; // TODO
            internal override IAggregateMemberType MemberType { get; } = new AggregateMemberTypes.VariationSwitch();

            internal override DbColumn.DbColumnBase GetDbColumn() {
                return new DbColumn.VariationTypeColumn(this);
            }
            internal IEnumerable<VariationItem> GetGroupItems() {
                foreach (var kv in _group.VariationAggregates) {
                    yield return new VariationItem(this, kv.Key, kv.Value);
                }
            }
        }

        internal class VariationItem : RelationMember {
            internal VariationItem(Variation group, string key, GraphEdge<Aggregate> edge) {
                Group = group;
                Key = key;
                _edge = edge;
            }
            private readonly GraphEdge<Aggregate> _edge;

            internal Variation Group { get; }
            internal string Key { get; }

            internal override GraphNode<Aggregate> Owner => _edge.Initial;
            internal override string PropertyName => _edge.RelationName;
            internal override string CSharpTypeName => _edge.Terminal.Item.ClassName;
            internal override string TypeScriptTypename => _edge.Terminal.Item.TypeScriptTypeName;
            internal override GraphNode<Aggregate> MemberAggregate => _edge.Terminal;

            internal override NavigationProperty GetNavigationProperty() {
                return new NavigationProperty(_edge);
            }
        }

        internal class Ref : RelationMember {
            internal Ref(GraphEdge<Aggregate> edge) {
                _edge = edge;
            }
            private readonly GraphEdge<Aggregate> _edge;

            internal override GraphNode<Aggregate> Owner => _edge.Initial;
            internal override string PropertyName => _edge.RelationName;
            internal override string CSharpTypeName => AggregateInstanceKeyNamePair.CLASSNAME;
            internal override string TypeScriptTypename => AggregateInstanceKeyNamePair.TS_DEF;

            internal override GraphNode<Aggregate> MemberAggregate => _edge.Terminal;

            internal bool IsPrimary => _edge.IsPrimary();
            internal bool IsInstanceName => _edge.IsInstanceName();
            internal bool RequiredAtDB => _edge.IsRequired();

            internal override NavigationProperty GetNavigationProperty() {
                return new NavigationProperty(_edge);
            }
            internal IEnumerable<ValueMember> GetRefTargetKeys() {
                return _edge.Terminal
                    .GetKeyMembers()
                    .Select(refTargetMember => new RefTargetMember(this, refTargetMember));
            }
            internal IEnumerable<ValueMember> GetRefTargetNameMembers() {
                return _edge.Terminal
                    .GetInstanceNameMembers()
                    .Select(refTargetMember => new RefTargetMember(this, refTargetMember));
            }
        }
        internal class RefTargetMember : ValueMember {
            internal RefTargetMember(Ref refMember, ValueMember refTargetMember) {
                _refMember = refMember;
                _refTargetMember = refTargetMember;
            }
            private readonly Ref _refMember;
            private readonly ValueMember _refTargetMember;

            internal override bool IsPrimary => _refMember.IsPrimary;
            internal override bool IsInstanceName => _refMember.IsInstanceName;
            internal override bool RequiredAtDB => _refMember.RequiredAtDB;
            internal override IAggregateMemberType MemberType => _refTargetMember.MemberType;
            internal override GraphNode<Aggregate> Owner => _refMember.Owner;
            internal override string PropertyName => $"{_refMember.PropertyName}_{_refTargetMember.PropertyName}";
            internal override string CSharpTypeName => _refTargetMember.CSharpTypeName;
            internal override string TypeScriptTypename => _refTargetMember.TypeScriptTypename;

            internal override DbColumn.DbColumnBase GetDbColumn() {
                return new DbColumn.RefTargetTablePKColumn(_refMember, _refTargetMember.GetDbColumn());
            }
        }

        internal class ParentPK : ValueMember {
            internal ParentPK(GraphNode<Aggregate> childAggregate, ValueMember parentPK) {
                _childAggregate = childAggregate;
                _parentPK = parentPK;
            }
            private readonly GraphNode<Aggregate> _childAggregate;
            private readonly ValueMember _parentPK;

            internal override bool IsPrimary => true;
            internal override bool IsInstanceName => false;
            internal override bool RequiredAtDB => true;
            internal override IAggregateMemberType MemberType => _parentPK.MemberType;
            internal override GraphNode<Aggregate> Owner => _childAggregate;
            internal override string PropertyName => _parentPK.PropertyName;
            internal override string CSharpTypeName => _parentPK.CSharpTypeName;
            internal override string TypeScriptTypename => _parentPK.TypeScriptTypename;

            internal override DbColumn.DbColumnBase GetDbColumn() {
                return new DbColumn.ParentTablePKColumn(_childAggregate.As<IEFCoreEntity>(), _parentPK.GetDbColumn());
            }
        }
        #endregion MEMBER IMPLEMEMT
    }
}
