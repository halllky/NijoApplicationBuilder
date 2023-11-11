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

        internal static IEnumerable<AggregateMemberBase> GetMembers(this GraphNode<Aggregate> aggregate) {
            var parent = aggregate.GetParent();
            if (parent != null) {
                var parentPKs = parent.Initial.GetKeys().ToArray();
                for (var i = 0; i < parentPKs.Length; i++) {
                    yield return new KeyOfParent(aggregate, parentPKs[i], i);
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

        /// <summary>糖衣構文</summary>
        internal static IEnumerable<ValueMember> GetKeysAndNames(this GraphNode<Aggregate> aggregate) {
            return new RefTargetKeyName(aggregate).GetKeysAndNames();
        }
        /// <summary>糖衣構文</summary>
        internal static IEnumerable<ValueMember> GetKeys(this GraphNode<Aggregate> aggregate) {
            return new RefTargetKeyName(aggregate).GetKeys();
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
            internal abstract string MemberName { get; }
            internal abstract decimal Order { get; }
            internal abstract string CSharpTypeName { get; }
            internal abstract string TypeScriptTypename { get; }

            internal virtual IEnumerable<string> GetFullPath(GraphNode<Aggregate>? since = null) {
                var skip = since != null;
                foreach (var edge in Owner.PathFromEntry()) {
                    if (skip && edge.Source?.As<Aggregate>() == since) skip = false;
                    if (skip) continue;
                    yield return edge.RelationName;
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
            internal Schalar(GraphNode<AggregateMemberNode> aggregateMemberNode) {
                _node = aggregateMemberNode;
            }
            private readonly GraphNode<AggregateMemberNode> _node;

            internal override GraphNode<Aggregate> Owner => _node.Source!.Initial.As<Aggregate>();
            internal override IReadOnlyMemberOptions Options => _node.Item;
            internal override decimal Order => _node.Source!.GetMemberOrder();
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
            internal Variation(VariationGroup<Aggregate> group) {
                _group = group;

                Options = new MemberOptions {
                    MemberName = group.GroupName,
                    MemberType = new AggregateMemberTypes.VariationSwitch(),
                    IsKey = group.IsPrimary,
                    IsDisplayName = group.IsInstanceName,
                    IsRequired = group.RequiredAtDB,
                    InvisibleInGui = false,
                };
            }
            private readonly VariationGroup<Aggregate> _group;

            internal override IReadOnlyMemberOptions Options { get; }
            internal override GraphNode<Aggregate> Owner => _group.Owner;
            internal override decimal Order => _group.MemberOrder;

            internal IEnumerable<VariationItem> GetGroupItems() {
                foreach (var kv in _group.VariationAggregates) {
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
            internal Ref(GraphEdge<Aggregate> edge) {
                Relation = edge;
            }

            internal override GraphEdge<Aggregate> Relation { get; }
            internal override string CSharpTypeName => new RefTargetKeyName(Relation.Terminal).CSharpClassName;
            internal override string TypeScriptTypename => new RefTargetKeyName(Relation.Terminal).TypeScriptTypeName;

            internal IEnumerable<KeyOfRefTarget> GetForeignKeys() {
                return Relation.Terminal
                    .GetKeys()
                    .Select((refTargetMember, index) => new KeyOfRefTarget(this, refTargetMember, index));
            }
        }
        internal class KeyOfRefTarget : ValueMember {
            internal KeyOfRefTarget(Ref refMember, ValueMember refTargetMember, int refTargetMemberOrder) {
                _refMember = refMember;
                Options = refTargetMember.Options.Clone(opt => {
                    opt.MemberName = $"{refMember.MemberName}_{refTargetMember.MemberName}";
                    opt.IsKey = refMember.Relation.IsPrimary();
                    opt.IsDisplayName = refMember.Relation.IsInstanceName();
                    opt.IsRequired = refMember.Relation.IsRequired();
                });
                Original = refTargetMember;
                Order = refMember.Order + (refTargetMemberOrder / 1000);
            }
            private readonly Ref _refMember;

            internal override IReadOnlyMemberOptions Options { get; }
            internal override decimal Order { get; }
            internal override GraphNode<Aggregate> Owner => _refMember.Owner;
            internal ValueMember Original { get; }
        }

        internal class KeyOfParent : ValueMember {
            internal KeyOfParent(GraphNode<Aggregate> childAggregate, ValueMember parentPK, int parentPkOrder) {
                Owner = childAggregate;
                Original = parentPK;
                Options = parentPK.Options.Clone(opt => {
                    var declaring = GetDeclaringMember();
                    opt.MemberName = declaring.Owner.Item.ClassName + declaring.MemberName;
                });
                Order = parentPK.Order + (parentPkOrder / 1000);
            }

            internal override GraphNode<Aggregate> Owner { get; }
            internal override IReadOnlyMemberOptions Options { get; }
            internal override decimal Order { get; }

            /// <summary>
            /// 大元が祖父母の主キーだった場合でも親のメンバーを返す
            /// </summary>
            internal ValueMember Original { get; }

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
