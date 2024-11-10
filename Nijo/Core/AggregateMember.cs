using Nijo.Models.WriteModel2Features;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core {
    internal class AggregateMemberNode : IGraphNode {
        public NodeId Id { get; set; } = NodeId.Empty;
        public string MemberName { get; set; } = string.Empty;
        public required IAggregateMemberType MemberType { get; set; }
        public bool IsKey { get; set; }
        public bool IsDisplayName { get; set; }
        public bool IsNameLike { get; set; }
        public bool IsRequired { get; set; }
        public bool InvisibleInGui { get; set; }
        [Obsolete("#60 最終的にnijo.xmlではなくApp.tsxで決められるようにするためこの属性は削除する")]
        public string? SingleViewCustomUiComponentName { get; set; }
        [Obsolete("#60 最終的にnijo.xmlではなくApp.tsxで決められるようにするためこの属性は削除する")]
        public string? SearchConditionCustomUiComponentName { get; set; }
        public TextBoxWidth? UiWidth { get; set; }
        public bool WideInVForm { get; set; }
        public bool IsCombo { get; set; }
        public bool IsRadio { get; set; }
        public string? DisplayName { get; set; }
        public string? DbName { get; set; }

        public override string ToString() => Id.Value;
    }


    public static class AggregateMember {

        /// <summary>
        /// この集約に属するメンバーを列挙します。
        /// </summary>
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
                (string)edge.Attributes[DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE]! == DirectedEdgeExtensions.REL_ATTRVALUE_HAVING);
            foreach (var edge in memberEdges) {
                yield return new Schalar(edge.Terminal.As<AggregateMemberNode>());
            }

            var childrenEdges = aggregate.Out.Where(edge =>
                edge.IsParentChild()
                && edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_MULTIPLE, out var isArray)
                && (bool)isArray!);
            foreach (var edge in childrenEdges) {
                yield return new Children(edge.As<Aggregate>());
            }

            var childEdges = aggregate.Out.Where(edge =>
                edge.IsParentChild()
                && (!edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_MULTIPLE, out var isArray) || (bool)isArray! == false)
                && (!edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUPNAME, out var groupName) || (string)groupName! == string.Empty));
            foreach (var edge in childEdges) {
                yield return new Child(edge.As<Aggregate>());
            }

            var variationGroups = aggregate.Out
                .Where(edge =>
                    edge.IsParentChild()
                    && (!edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_MULTIPLE, out var isArray) || (bool)isArray! == false)
                    && edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUPNAME, out var groupName)
                    && (string)groupName! != string.Empty)
                .GroupBy(edge => (string)edge.Attributes[DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUPNAME]!)
                .Select(group => new VariationGroup<Aggregate> {
                    GroupName = group.Key,
                    VariationAggregates = group.ToDictionary(
                        edge => (string)edge.Attributes[DirectedEdgeExtensions.REL_ATTR_VARIATIONSWITCH]!,
                        edge => edge.As<Aggregate>()),
                    DisplayName = (string)group.First().Attributes[DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUP_DISPLAYNAME]!,
                    DbName = group.First().Attributes
                        .TryGetValue(DirectedEdgeExtensions.REL_ATTR_DB_NAME, out var dbName)
                        && !string.IsNullOrWhiteSpace((string?)dbName)
                        ? (string)dbName!
                        : null,
                    MemberOrder = group.First().GetMemberOrder(),
                    IsCombo = group.First().Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_IS_COMBO, out var isCombo) && (bool?)isCombo == true,
                    IsRadio = group.First().Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_IS_RADIO, out var isRadio) && (bool?)isRadio == true,
                });
            foreach (var group in variationGroups) {
                var variationGroup = new Variation(group);
                yield return variationGroup;
                foreach (var item in variationGroup.GetGroupItems()) yield return item;
            }

            var refEdges = aggregate.Out.Where(edge =>
                edge.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, out var type)
                && (string)type! == DirectedEdgeExtensions.REL_ATTRVALUE_REFERENCE);
            foreach (var edge in refEdges) {
                var refMember = new Ref(edge.As<Aggregate>());
                yield return refMember;
                foreach (var refPK in refMember.GetForeignKeys()) yield return refPK;
            }
        }

        /// <summary>
        /// この集約に属するメンバーのうちキーを列挙します。
        /// 自身のメンバーのうちキー属性が指定されているもの、親集約、参照先がキーである場合はその参照先と参照先のキー、がキーになりえます。
        /// </summary>
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


        #region MEMBER BASE
        internal const string PARENT_PROPNAME = "Parent";

        /// <summary>
        /// 集約メンバーはすべてこのクラスを継承します。
        /// </summary>
        public abstract class AggregateMemberBase : ValueObject {
            internal abstract GraphNode<Aggregate> Owner { get; }
            /// <summary>
            /// TODO: ParentだとDeclaringAggregateは子ではなく親になるのが違和感。
            /// そもそもDeclaringAggregateはValueMemberにのみ存在する概念と考えるのが自然な気がする。
            /// </summary>
            internal abstract GraphNode<Aggregate> DeclaringAggregate { get; }

            /// <summary>
            /// 物理名
            /// </summary>
            internal abstract string MemberName { get; }
            /// <summary>
            /// 画面表示名
            /// </summary>
            internal abstract string DisplayName { get; }

            /// <summary>
            /// スキーマ定義でこのメンバーが定義されている順番
            /// </summary>
            internal abstract decimal Order { get; }

            public override string ToString() {
                return Owner
                    .PathFromEntry()
                    .Select(edge => edge.RelationName)
                    .Concat([MemberName])
                    .Join(".");
            }
        }
        /// <summary>
        /// 値メンバー。
        /// 数値や文字列など普通のメンバー（<see cref="Schalar"/>）のほか、
        /// バリエーションがある場合はそのバリエーションがどの種類なのかを切り替えるスイッチ（<see cref="Variation"/>）がこれに含まれます。
        /// </summary>
        internal abstract class ValueMember : AggregateMemberBase {
            protected ValueMember(InheritInfo? inherits) {
                Inherits = inherits;
            }

            internal abstract AggregateMemberNode Options { get; }

            private string? _membername;
            internal sealed override string MemberName {
                get {
                    if (_membername == null) {
                        if (Inherits == null) {
                            _membername = Options.MemberName;

                        } else if (Inherits.Relation.IsParentChild()) {
                            // 親のメンバーと対応して暗黙的に定義されるメンバーの名前
                            _membername = $"PARENT_{Inherits.Member.MemberName}";
                        } else {
                            // 参照先のメンバーと対応して暗黙的に定義されるメンバーの名前
                            _membername = $"{Inherits.Relation.RelationName}_{Inherits.Member.MemberName}";
                        }
                    }
                    return _membername;
                }
            }

            internal override string DisplayName => Options.DisplayName ?? MemberName;

            private string? _dbColumnName;
            /// <summary>
            /// DBカラム名
            /// </summary>
            //internal string DbColumnName => Options.DbName ?? MemberName;
            internal string DbColumnName {
                get {
                    if (_dbColumnName == null) {
                        if (Inherits == null) {
                            _dbColumnName = Options.DbName ?? Options.MemberName;

                        } else if (Inherits.Relation.IsParentChild()) {
                            // 親のメンバーと対応して暗黙的に定義されるメンバーの名前
                            _dbColumnName = $"PARENT_{Inherits.Member.DbColumnName}";
                        } else {
                            // 参照先のメンバーと対応して暗黙的に定義されるメンバーの名前
                            var prefix = Inherits.Relation.Attributes
                                .TryGetValue(DirectedEdgeExtensions.REL_ATTR_DB_NAME, out var dbName)
                                && !string.IsNullOrWhiteSpace((string?)dbName)
                                ? (string)dbName!
                                : Inherits.Relation.RelationName;
                            _dbColumnName = $"{prefix}_{Inherits.Member.DbColumnName}";
                        }
                    }
                    return _dbColumnName;
                }
            }

            internal sealed override GraphNode<Aggregate> DeclaringAggregate => Inherits?.Member.DeclaringAggregate ?? Owner;

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

        /// <summary>
        /// リレーションメンバー。
        /// 親（<see cref="Parent"/>）、子（<see cref="Child"/>, <see cref="Children"/>, <see cref="VariationItem"/>）、参照先（<see cref="Ref"/>）がこれに含まれます。
        /// </summary>
        internal abstract class RelationMember : AggregateMemberBase {
            internal abstract GraphEdge<Aggregate> Relation { get; }
            internal abstract GraphNode<Aggregate> MemberAggregate { get; }

            internal override GraphNode<Aggregate> Owner => Relation.Initial;
            internal override GraphNode<Aggregate> DeclaringAggregate => Relation.Initial;
            internal override string MemberName => Relation.RelationName;
            internal override string DisplayName => Relation.GetDisplayName() ?? MemberName;
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
        /// <summary>
        /// 数値や文字列などいわゆる普通の値メンバー。
        /// 有向グラフ上にはこのメンバーと対応するノードが存在します。
        /// </summary>
        internal class Schalar : ValueMember {
            internal Schalar(GraphNode<AggregateMemberNode> aggregateMemberNode) : base(null) {
                GraphNode = aggregateMemberNode;
                Owner = aggregateMemberNode.Source!.Initial.As<Aggregate>();
                Options = GraphNode.Item;
            }
            internal Schalar(GraphNode<Aggregate> owner, InheritInfo inherits, AggregateMemberNode options) : base(inherits) {
                GraphNode = ((Schalar)inherits.Member).GraphNode;
                Owner = owner;
                Options = options;
            }
            internal GraphNode<AggregateMemberNode> GraphNode { get; }
            internal override GraphNode<Aggregate> Owner { get; }

            internal override AggregateMemberNode Options { get; }
            internal override decimal Order => GraphNode.Source!.GetMemberOrder();

            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return Owner;
                yield return GraphNode;
                yield return Inherits?.Relation;
                yield return Inherits?.Member;
            }
        }

        /// <summary>
        /// 親集約1件に対して複数存在する子集約。
        /// </summary>
        internal class Children : RelationMember {
            internal Children(GraphEdge<Aggregate> edge) {
                Relation = edge;
            }
            internal override GraphEdge<Aggregate> Relation { get; }
            internal GraphNode<Aggregate> ChildrenAggregate => MemberAggregate;
            /// <summary><see cref="ChildrenAggregate"/>と全く同じもの。より名前がわかりやすい左記の利用を推奨。</summary>
            internal override GraphNode<Aggregate> MemberAggregate => Relation.Terminal;
        }

        /// <summary>
        /// 親集約1件に対して1件存在する子集約。
        /// </summary>
        internal class Child : RelationMember {
            internal Child(GraphEdge<Aggregate> edge) {
                Relation = edge;
            }
            internal override GraphEdge<Aggregate> Relation { get; }
            internal GraphNode<Aggregate> ChildAggregate => MemberAggregate;
            /// <summary><see cref="ChildAggregate"/>と全く同じもの。より名前がわかりやすい左記の利用を推奨。</summary>
            internal override GraphNode<Aggregate> MemberAggregate => Relation.Terminal;
        }

        /// <summary>
        /// バリエーションの切り替え用メンバー。
        /// 子集約のデータ型が複数の種類のうちから1種類をとりうるとき、
        /// それらのうちどの種類に属するかを切り替えるためのメンバー。
        /// 子集約は <see cref="VariationItem"/>
        /// </summary>
        internal class Variation : ValueMember {
            internal Variation(VariationGroup<Aggregate> group) : base(null) {
                VariationGroup = group;
                Options = new AggregateMemberNode {
                    MemberName = group.GroupName,
                    MemberType = new AggregateMemberTypes.VariationSwitch(group),
                    IsKey = group.IsPrimary,
                    IsDisplayName = group.IsInstanceName,
                    IsNameLike = group.IsNameLike,
                    IsRequired = group.RequiredAtDB,
                    InvisibleInGui = false,
                    SingleViewCustomUiComponentName = null,
                    SearchConditionCustomUiComponentName = null,
                    UiWidth = null,
                    WideInVForm = false,
                    IsCombo = group.IsCombo,
                    IsRadio = group.IsRadio,
                    DbName = group.DbName,
                    DisplayName = group.DisplayName,
                };
                Owner = group.Owner;
            }
            internal Variation(GraphNode<Aggregate> owner, InheritInfo inherits) : base(inherits) {
                VariationGroup = ((Variation)inherits.Member).VariationGroup;
                Options = inherits.Member.Options;
                Owner = owner;
            }

            internal VariationGroup<Aggregate> VariationGroup { get; }
            internal override AggregateMemberNode Options { get; }
            internal override GraphNode<Aggregate> Owner { get; }
            internal override decimal Order => VariationGroup.MemberOrder;

            internal string CsEnumType => VariationGroup.CsEnumType;

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

        /// <summary>
        /// バリエーションの子集約。親集約1件に対して1件存在する。
        /// 子集約のデータ型が複数の種類のうちから1種類をとりうるとき、その子集約側。
        /// それらのうちどの種類に属するかの切り替え用メンバーは <see cref="Variation"/>。
        /// </summary>
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

            /// <summary>この集約のタイプを表すTypeScriptの区分値</summary>
            internal string TsValue => Relation.RelationName;
        }

        /// <summary>
        /// 参照先。
        /// 有向グラフ上はエッジとして表現される。
        /// </summary>
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

            /// <summary>生成後のソースで外から注入して、中で React context 経由で参照するコンポーネント。ValueMemberまたはRefでのみ使用</summary>
            [Obsolete("#60 最終的にnijo.xmlではなくApp.tsxで決められるようにするためこの属性は削除する")]
            internal string? SingleViewCustomUiComponentName => Relation.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_SINGLEVIEW_CUSTOM_UI_COMPONENT_NAME, out var s) ? (string?)s : null;
            /// <summary>生成後のソースで外から注入して、中で React context 経由で参照するコンポーネント。ValueMemberまたはRefでのみ使用</summary>
            [Obsolete("#60 最終的にnijo.xmlではなくApp.tsxで決められるようにするためこの属性は削除する")]
            internal string? SearchConditionCustomUiComponentName => Relation.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_SEARCHCONDITION_CUSTOM_UI_COMPONENT_NAME, out var s) ? (string?)s : null;

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

        /// <summary>
        /// 親集約。
        /// </summary>
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
