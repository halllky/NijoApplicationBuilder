using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        internal required bool InvisibleInGui { get; init; }

        public override string ToString() => Id.Value;
    }


    internal static class AggregateMember {

        internal static IEnumerable<AggregateMemberBase> GetMembers(this GraphNode<Aggregate> aggregate) {
            var parent = aggregate.GetParent();
            if (parent != null) {
                foreach (var parentPK in parent.Initial.GetKeyMembers()) {
                    yield return new KeyOfParent(aggregate, parentPK);
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
                foreach (var refPK in refMember.GetForeignKeys()) yield return refPK;
            }
        }
        internal static IEnumerable<ValueMember> GetKeyMembers(this GraphNode<Aggregate> aggregate) {
            return aggregate
                .GetMembers()
                .OfType<ValueMember>()
                .Where(member => member.IsPrimary);
        }
        internal static IEnumerable<ValueMember> GetInstanceNameMembers(this GraphNode<Aggregate> aggregate) {
            return aggregate
                .GetMembers()
                .OfType<ValueMember>()
                .Where(member => member.IsInstanceName)
                .ToArray();
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
            internal abstract string PropertyName { get; }
            internal abstract string CSharpTypeName { get; }
            internal abstract string TypeScriptTypename { get; }
            internal abstract bool InvisilbeInGui { get; }

            internal virtual IEnumerable<string> GetFullPath(GraphNode<Aggregate>? since = null) {
                var skip = since != null;
                foreach (var edge in Owner.PathFromEntry()) {
                    if (skip && edge.Source?.As<Aggregate>() == since) skip = false;
                    if (skip) continue;
                    yield return edge.RelationName;
                }
                yield return PropertyName;
            }

            public override string ToString() {
                return GetFullPath().Join(".");
            }
        }
        internal abstract class ValueMember : AggregateMemberBase {
            internal abstract bool IsPrimary { get; }

            internal bool IsInstanceName
                => SettingIsInstanceName()
                || (!Owner.Item.HasNameMember && IsPrimary); // 集約中に名前が無い場合はキーを名前のかわりに使う
            protected abstract bool SettingIsInstanceName();

            internal abstract bool RequiredAtDB { get; }
            internal abstract IAggregateMemberType MemberType { get; }

            internal DbColumn GetDbColumn() {
                return new DbColumn {
                    Owner = Owner.As<IEFCoreEntity>(),
                    PropertyName = PropertyName,
                    MemberType = MemberType,
                    IsPrimary = IsPrimary,
                    IsInstanceName = IsInstanceName,
                    RequiredAtDB = IsPrimary || RequiredAtDB,
                    InvisibleInGui = InvisilbeInGui,
                };
            }
        }
        internal abstract class RelationMember : AggregateMemberBase {
            protected RelationMember(GraphEdge<Aggregate> relation) {
                Relation = relation;
            }
            internal GraphEdge<Aggregate> Relation { get; }
            internal GraphNode<Aggregate> MemberAggregate => Relation.Terminal;
            internal override bool InvisilbeInGui => Relation.InvisibleInGui();
            internal NavigationProperty GetNavigationProperty() {
                return new NavigationProperty(Relation);
            }
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
            protected override bool SettingIsInstanceName() => _node.Item.IsInstanceName;
            internal override bool RequiredAtDB => _node.Item.IsPrimary || !_node.Item.Optional;
            internal override bool InvisilbeInGui => _node.Item.InvisibleInGui;
            internal override IAggregateMemberType MemberType => _node.Item.Type;
        }

        internal class Children : RelationMember {
            internal Children(GraphEdge<Aggregate> edge) : base(edge) { }

            internal override GraphNode<Aggregate> Owner => Relation.Initial;
            internal override string PropertyName => Relation.RelationName;
            internal override string CSharpTypeName => $"List<{Relation.Terminal.Item.ClassName}>";
            internal override string TypeScriptTypename => $"{Relation.Terminal.Item.TypeScriptTypeName}[]";
        }

        internal class Child : RelationMember {
            internal Child(GraphEdge<Aggregate> edge) : base(edge) { }

            internal override GraphNode<Aggregate> Owner => Relation.Initial;
            internal override string PropertyName => Relation.RelationName;
            internal override string CSharpTypeName => Relation.Terminal.Item.ClassName;
            internal override string TypeScriptTypename => Relation.Terminal.Item.TypeScriptTypeName;
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

            internal override bool IsPrimary => _group.IsPrimary;
            protected override bool SettingIsInstanceName() => _group.IsInstanceName;
            internal override bool RequiredAtDB => _group.IsPrimary || _group.RequiredAtDB;
            internal override bool InvisilbeInGui => _group.InvisibleInGui;
            internal override IAggregateMemberType MemberType { get; } = new AggregateMemberTypes.VariationSwitch();

            internal IEnumerable<VariationItem> GetGroupItems() {
                foreach (var kv in _group.VariationAggregates) {
                    yield return new VariationItem(this, kv.Key, kv.Value);
                }
            }
        }

        internal class VariationItem : RelationMember {
            internal VariationItem(Variation group, string key, GraphEdge<Aggregate> edge) : base(edge) {
                Group = group;
                Key = key;
            }

            internal Variation Group { get; }
            internal string Key { get; }

            internal override GraphNode<Aggregate> Owner => Relation.Initial;
            internal override string PropertyName => Relation.RelationName;
            internal override string CSharpTypeName => Relation.Terminal.Item.ClassName;
            internal override string TypeScriptTypename => Relation.Terminal.Item.TypeScriptTypeName;
        }

        internal class Ref : RelationMember {
            internal Ref(GraphEdge<Aggregate> edge) : base(edge) { }

            internal override GraphNode<Aggregate> Owner => Relation.Initial;
            internal override string PropertyName => Relation.RelationName;
            internal override string CSharpTypeName => AggregateInstanceKeyNamePair.CLASSNAME;
            internal override string TypeScriptTypename => AggregateInstanceKeyNamePair.TS_DEF;

            internal IEnumerable<ValueMember> GetForeignKeys() {
                return Relation.Terminal
                    .GetKeyMembers()
                    .Select(refTargetMember => new KeyOfRefTarget(this, refTargetMember));
            }
        }
        internal class KeyOfRefTarget : ValueMember {
            internal KeyOfRefTarget(Ref refMember, ValueMember refTargetMember) {
                _refMember = refMember;
                _refTargetMember = refTargetMember;
            }
            private readonly Ref _refMember;
            private readonly ValueMember _refTargetMember;

            internal override bool IsPrimary => _refMember.Relation.IsPrimary();
            protected override bool SettingIsInstanceName() => _refMember.Relation.IsInstanceName();
            internal override bool RequiredAtDB => _refMember.Relation.IsRequired();
            internal override bool InvisilbeInGui => _refMember.InvisilbeInGui;
            internal override IAggregateMemberType MemberType => _refTargetMember.MemberType;
            internal override GraphNode<Aggregate> Owner => _refMember.Owner;
            internal override string PropertyName => $"{_refMember.PropertyName}_{_refTargetMember.PropertyName}";
            internal override string CSharpTypeName => _refTargetMember.CSharpTypeName;
            internal override string TypeScriptTypename => _refTargetMember.TypeScriptTypename;
        }

        internal class KeyOfParent : ValueMember {
            internal KeyOfParent(GraphNode<Aggregate> childAggregate, ValueMember parentPK) {
                _childAggregate = childAggregate;
                Original = parentPK;
            }
            private readonly GraphNode<Aggregate> _childAggregate;
            /// <summary>
            /// 大元が祖父母の主キーだった場合でも親のメンバーを返す
            /// </summary>
            internal ValueMember Original { get; }

            internal override bool IsPrimary => true;
            protected override bool SettingIsInstanceName() => false;
            internal override bool RequiredAtDB => true;
            internal override bool InvisilbeInGui => Original.InvisilbeInGui;
            internal override IAggregateMemberType MemberType => Original.MemberType;
            internal override GraphNode<Aggregate> Owner => _childAggregate;
            internal override string PropertyName => $"{Original.Owner.Item.ClassName}_{Original.PropertyName}";
            internal override string CSharpTypeName => Original.CSharpTypeName;
            internal override string TypeScriptTypename => Original.TypeScriptTypename;

            /// <summary>
            /// 大元が祖父母の主キーだった場合は祖父母のメンバーを返す
            /// </summary>
            internal ValueMember GetDeclaringMember() {
                var member = Original;
                while (member is KeyOfParent memberAsParentPK) {
                    member = memberAsParentPK.Original;
                }
                return member;
            }
        }
        #endregion MEMBER IMPLEMEMT
    }
}
