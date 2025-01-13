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
        /// <summary>nijo.xmlで指定が無い場合はnullになる</summary>
        public bool? WideInVForm { get; set; }
        public bool IsCombo { get; set; }
        public bool IsRadio { get; set; }
        public string? DisplayName { get; set; }
        public string? DbName { get; set; }
        /// <summary>検索条件の挙動</summary>
        public E_SearchBehavior? SearchBehavior { get; set; }

        /// <summary>文字列型の最大長</summary>
        public int? MaxLength { get; set; }

        public string? EnumSqlParamType { get; set; }

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
                internal required Func<RefForeignKeyProxySetting.LogicClass?> GetRefForeignKeyProxy { get; init; }
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
                            new ValueMember.InheritInfo { Relation = Relation, Member = schalar, GetRefForeignKeyProxy = () => GetForeignKeyProxy(schalar) },
                            schalar.GraphNode.Item);

                    } else if (fk is Variation variation) {
                        yield return new Variation(
                            Relation.Initial,
                            new ValueMember.InheritInfo { Relation = Relation, Member = variation, GetRefForeignKeyProxy = () => GetForeignKeyProxy(variation) });
                    }
                }

                RefForeignKeyProxySetting.LogicClass? GetForeignKeyProxy(ValueMember pkOfRef) {
                    var foreignKeyProxies = Relation.Attributes
                        .TryGetValue(DirectedEdgeExtensions.REL_ATTR_PROXY, out var p)
                        ? (RefForeignKeyProxySetting[]?)p
                        : null;
                    if (foreignKeyProxies == null) return null;

                    foreach (var proxy in foreignKeyProxies) {
                        if (proxy.TryGetLogicClass(this, pkOfRef, out var logicClass)) {
                            return logicClass;
                        }
                    }

                    // 参照先に更に別の集約への参照かつキーかつ代理外部キーがある場合
                    var proxyRecursively = pkOfRef.Inherits?.GetRefForeignKeyProxy();
                    if (proxyRecursively != null) return proxyRecursively;

                    return null;
                }
            }


            /// <summary>
            /// このrefが区分マスタへの参照の場合、その詳細情報を返します。
            /// </summary>
            internal DynamicEnumTypeInfo? GetDynamicEnumTypeInfo(Util.CodeGenerating.CodeRenderingContext ctx) {
                if (!Relation.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_DYNAMIC_ENUM_TYPE_NAME, out var dynamicEnumTypePhysicalName)
                    || string.IsNullOrWhiteSpace((string?)dynamicEnumTypePhysicalName)) {
                    return null;
                }

                return ctx.Schema.DynamicEnumTypeInfo.Single(x => x.PhysicalName == (string)dynamicEnumTypePhysicalName);
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
                            new ValueMember.InheritInfo { Relation = Relation, Member = schalar, GetRefForeignKeyProxy = () => null },
                            schalar.GraphNode.Item);

                    } else if (parentPk is Variation variation) {
                        yield return new Variation(
                            Relation.Terminal,
                            new ValueMember.InheritInfo { Relation = Relation, Member = variation, GetRefForeignKeyProxy = () => null });
                    }
                }
            }
        }
        #endregion MEMBER IMPLEMEMT
    }


    #region 代理外部キー
    /// <summary>
    /// 通常はref-to毎にDBの外部キーのカラムが生成されるところ、
    /// そのうちの一部を、ref-toが存在しなかった場合であっても存在する元々あるカラムで代替させる設定。
    /// </summary>
    public class RefForeignKeyProxySetting {
        /// <summary>
        /// nijo ui （nijo.xml）で指定されたパスの解釈
        /// </summary>
        /// <param name="value">xmlで指定されたパス</param>
        /// <param name="availableRefToKeys">参照先のキーの属性名として指定できる名前の一覧</param>
        /// <param name="availableProxies">代理キーの名前として指定できるものの一覧</param>
        /// <param name="proxy">戻り値</param>
        /// <returns>エラーがある場合はエラーメッセージを返します。エラーが無い場合はnull。</returns>
        internal static string? ParseOrGetErrorMessage(
            string value,
            IEnumerable<AvailableItem> availableRefToKeys,
            IEnumerable<AvailableItem> availableProxies,
            out RefForeignKeyProxySetting proxy) {
            proxy = new RefForeignKeyProxySetting([], []);

            var arr = value.Split('=');
            if (arr.Length != 2) return $"代理キー指定 '{value}' が不正です。'ref.略.略.略=this.略.略.略' のように等号で結ばれた式である必要があります。";

            var member = arr[0].Split('.');
            var proxyMember = arr[1].Split('.');

            if (member.Length <= 1) return $"代理キー指定 '{value}' が不正です。参照先のどのキーを代理するかが指定されていません。";
            if (proxyMember.Length <= 1) return $"代理キー指定 '{value}' が不正です。参照先のキーを自身のどの属性で代理するかが指定されていません。";

            // 使用可能なパスが指定されているかどうか（参照先のキー）
            var current = availableRefToKeys;
            for (int i = 0; i < member.Length; i++) {
                var path = member[i];
                if (i == 0) {
                    if (path != "ref") return $"代理キー指定 '{value}' が不正です。代理キー指定の等号の前は 'ref.略.略.略=this.略.略.略' のように'ref'で始まる必要があります。";
                } else {
                    var found = current.FirstOrDefault(x => x.RelationPhysicalName == path);
                    if (found == null) return $"代理キー指定 '{value}' が不正です。等号の前の{i + 1}番目に項目名 '{path}' は指定できません。この位置に使用できるのは {current.Select(x => x.RelationPhysicalName).Join(", ")} のいずれかです。";
                    current = found.GetNeighborItems().ToArray();
                }
            }

            // 使用可能なパスが指定されているかどうか（代理キー）
            current = availableProxies;
            for (int i = 0; i < proxyMember.Length; i++) {
                var path = proxyMember[i];
                if (i == 0) {
                    if (path != "this") return $"代理キー指定 '{value}' が不正です。代理キー指定の等号の後は 'ref.略.略.略=this.略.略.略' のように'this'で始まる必要があります。";
                } else {
                    var found = current.FirstOrDefault(x => x.RelationPhysicalName == proxyMember[i]);
                    if (found == null) return $"代理キー指定 '{value}' が不正です。等号の後の{i + 1}番目に項目名 '{path}' は指定できません。この位置に使用できるのは {current.Select(x => x.RelationPhysicalName).Join(", ")} のいずれかです。";
                    current = found.GetNeighborItems().ToArray();
                }
            }

            proxy = new RefForeignKeyProxySetting(
                member.Skip(1).ToArray(),
                proxyMember.Skip(1).ToArray());
            return null;
        }
        public class AvailableItem {
            public required string RelationPhysicalName { get; init; }
            public required Func<IEnumerable<AvailableItem>> GetNeighborItems { get; init; }
        }

        private RefForeignKeyProxySetting(string[] refKeyMemberPath, string[] proxyMemberPath) {
            _refKeyMemberPath = refKeyMemberPath;
            _proxyMemberPath = proxyMemberPath;
        }

        /// <summary>
        /// 紐づけ対象となるメンバーの名前。
        /// もしこの設定がなければ生成されていたであろうメンバーの物理名。
        /// </summary>
        private readonly string[] _refKeyMemberPath;
        /// <summary>
        /// 紐づけ対象となるメンバーの名前。
        /// <see cref="_refKeyMemberPath"/> のかわりに使われるメンバーへの物理名のパス
        /// </summary>
        private readonly string[] _proxyMemberPath;

        /// <summary>
        /// このインスタンスの設定値と引数のValueMemberが一致する場合はロジック付きクラスを返す
        /// </summary>
        internal bool TryGetLogicClass(AggregateMember.Ref @ref, AggregateMember.ValueMember pkOfRef, out LogicClass logicClass) {
            var isMach = pkOfRef.Declared
                .GetFullPathAsForSave(since: @ref.RefTo)
                .SequenceEqual(_refKeyMemberPath);
            if (isMach) {
                logicClass = new LogicClass(this, @ref, pkOfRef);
                return true;
            } else {
                logicClass = null!;
                return false;
            }
        }

        /// <summary>
        /// <see cref="RefForeignKeyProxySetting"/> クラスにソースコード自動生成の各所で使える便利ロジックを持たせたもの
        /// </summary>
        internal class LogicClass {
            internal LogicClass(RefForeignKeyProxySetting settings, AggregateMember.Ref @ref, AggregateMember.ValueMember pkOfRef) {
                _settings = settings;
                _ref = @ref;
                _pkOfRef = pkOfRef;
            }
            private readonly RefForeignKeyProxySetting _settings;
            private readonly AggregateMember.Ref _ref;
            private readonly AggregateMember.ValueMember _pkOfRef;

            /// <summary>
            /// <see cref="LogicClass"/> のコンストラクタの引数のValueMemberと対応する代理外部キーを返します。
            /// なお、代理外部キーが祖先の主キーである場合、代理外部キーそれ自身ではなく、
            /// 引数のRefの集約の中にある、当該祖先の主キーを継承したメンバーを返します。
            /// </summary>
            internal AggregateMember.ValueMember GetProxyMember() {

                // refのOwnerを基点に代理キーを探して返す
                var currentAggregate = _ref.Owner;
                for (int i = 0; i < _settings._proxyMemberPath.Length; i++) {
                    var isLast = i == _settings._proxyMemberPath.Length - 1;
                    var name = _settings._proxyMemberPath[i];
                    if (isLast) {
                        var ownColumn = currentAggregate
                            .GetMembers()
                            .OfType<AggregateMember.ValueMember>()
                            .Single(m => m.MemberName == name);

                        // 代理外部キーが祖先の主キーである場合、代理外部キーそれ自身ではなく、
                        // 引数のRefの集約の中にある、当該祖先の主キーを継承したメンバーを返す
                        if (_settings._proxyMemberPath.First() == AggregateMember.PARENT_PROPNAME) {
                            var parent = _ref.Owner.GetParent();
                            ownColumn = _ref.Owner
                                .GetMembers()
                                .OfType<AggregateMember.ValueMember>()
                                .Single(x => x.Inherits?.Member.Declared == ownColumn
                                          && x.Inherits?.Relation == parent);
                        }
                        return ownColumn;

                    } else {
                        var member = currentAggregate
                            .GetMembers()
                            .OfType<AggregateMember.RelationMember>()
                            .Single(m => m.MemberName == name);
                        currentAggregate = member.MemberAggregate;
                    }
                }

                throw new InvalidOperationException("ここまで来ることは無いはず");
            }
        }
    }
    #endregion 代理外部キー
}
