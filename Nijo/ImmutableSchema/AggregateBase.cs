using Nijo.CodeGenerating;
using Nijo.SchemaParsing;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Nijo.ImmutableSchema {
    /// <summary>
    /// モデルの集約。
    /// 集約ルート, Child, Children, VariationItem のいずれか。
    /// </summary>
    public abstract class AggregateBase : ISchemaPathNode {

        internal AggregateBase(XElement xElement, SchemaParseContext ctx, ISchemaPathNode? previous) {
            _xElement = xElement;
            _ctx = ctx;
            PreviousNode = previous;
        }
        private protected readonly XElement _xElement;
        private protected readonly SchemaParseContext _ctx;

        XElement ISchemaPathNode.XElement => _xElement;
        public ISchemaPathNode? PreviousNode { get; }

        /// <summary>
        /// 物理名
        /// </summary>
        public string PhysicalName => _ctx.GetPhysicalName(_xElement);
        /// <summary>
        /// 表示用名称
        /// </summary>
        public string DisplayName => _ctx.GetDisplayName(_xElement);
        /// <summary>
        /// データベーステーブル名
        /// </summary>
        public string DbName => _ctx.GetDbName(_xElement);
        /// <summary>
        /// ラテン語名
        /// </summary>
        public string LatinName => _ctx.GetLatinName(_xElement);

        /// <summary>
        /// この集約が参照先エントリーとして参照された場合の名前。
        /// スキーマ定義xmlのType属性の表記と一致しているとメタデータを使った処理が書きやすくて嬉しいので合わせている。
        /// </summary>
        public string RefEntryName => $"ref-to:{EnumerateThisAndAncestors().Select(a => a.PhysicalName).Join("/")}";

        /// <summary>
        /// この集約が持つメンバーを列挙します。
        /// <list type="bullet">
        /// <item>親: 列挙しません。</item>
        /// <item>子(Child, Children): 列挙します。</item>
        /// <item>参照先(RefTo): 列挙します。</item>
        /// </list>
        /// </summary>
        public IEnumerable<IAggregateMember> GetMembers() {
            foreach (var el in _xElement.Elements()) {
                // パスの巻き戻しの場合（この集約の1つ前がこの集約の子、かつその子を列挙しようとしている場合）
                // 新たにインスタンスを作るのでなく1つ前のインスタンスをそのまま使う
                if (el == PreviousNode?.XElement) {
                    yield return (IAggregateMember)PreviousNode;
                    continue;
                }

                var nodeType = _ctx.GetNodeType(el);
                yield return nodeType switch {
                    E_NodeType.ChildAggregate => new ChildAggregate(el, _ctx, this),
                    E_NodeType.ChildrenAggregate => new ChildrenAggregate(el, _ctx, this),
                    E_NodeType.Ref => new RefToMember(el, _ctx, this),
                    E_NodeType.ValueMember => new ValueMember(el, _ctx, this),
                    E_NodeType.StaticEnumValue => new Models.StaticEnumModelModules.StaticEnumValueDef(el, _ctx, this),
                    _ => throw new InvalidOperationException($"メンバーでない種類: {nodeType}（{el}）"),
                };
            }
        }
        /// <summary>
        /// この集約に直接属するキー項目を返します。
        /// つまりキーに <see cref="RefToMember"/> が含まれるならばそのRef自身を列挙します。
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IAggregateMember> GetOwnKeys() {
            return GetMembers().Where(m => m is ValueMember vm && vm.IsKey
                                        || m is RefToMember rm && rm.IsKey);
        }
        /// <summary>
        /// キー項目のうち <see cref="ValueMember"/> を列挙します。
        /// つまりキーに <see cref="RefToMember"/> が含まれるならばそのRefの値メンバーを、
        /// キーに親が含まれるならば親の値メンバーを列挙します。
        /// </summary>
        public IEnumerable<ValueMember> GetKeyVMs() {
            // 親および祖先のキー項目を列挙
            var parent = GetParent();
            if (parent != null) {
                foreach (var parentKeyVm in parent.GetKeyVMs()) {
                    yield return parentKeyVm;
                }
            }
            // 自身のメンバーからキー項目を列挙
            foreach (var member in GetMembers()) {
                if (member is ValueMember vm && vm.IsKey) {
                    yield return vm;

                } else if (member is RefToMember refTo && refTo.IsKey) {
                    foreach (var refToVm in refTo.RefTo.GetKeyVMs()) {
                        yield return refToVm;
                    }
                }
            }
        }


        #region 親子
        /// <summary>
        /// この集約の親を返します。
        /// </summary>
        public AggregateBase? GetParent() {
            // この集約がルート集約の場合
            if (_xElement.Parent == _xElement.Document?.Root) return null;

            // 1つ前の集約が親の場合
            if (PreviousNode is AggregateBase agg && agg._xElement == _xElement.Parent) {
                return agg;
            }

            // 子から親に辿る場合
            return _ctx.ToAggregateBase(_xElement.Parent ?? throw new InvalidOperationException(), this);
        }

        /// <summary>
        /// 祖先集約を列挙します。ルート集約が先
        /// </summary>
        public IEnumerable<AggregateBase> EnumerateAncestors() {
            var ancestors = new List<AggregateBase>();
            var current = GetParent();

            while (current != null) {
                ancestors.Add(current);
                current = current.GetParent();
            }

            // ルート集約が先になるよう逆順にして返す
            return ancestors.AsEnumerable().Reverse();
        }
        /// <summary>
        /// この集約と祖先集約を列挙します。ルート集約が先
        /// </summary>
        public IEnumerable<AggregateBase> EnumerateThisAndAncestors() {
            foreach (var ancestor in EnumerateAncestors()) {
                yield return ancestor;
            }

            yield return this;
        }
        /// <summary>
        /// ルート集約を返します。
        /// </summary>
        internal RootAggregate GetRoot() {
            var ancestor = this;
            while (true) {
                var parent = ancestor.GetParent();
                if (parent == null) return (RootAggregate)ancestor;
                ancestor = parent;
            }
        }

        /// <summary>
        /// ルート集約からこのメンバーまでのパスを列挙する。
        /// 経路情報はクリアされ、ルート集約がエントリーになる。
        /// </summary>
        public IEnumerable<AggregateBase> GetPathFromRoot() {
            var ancesotors = _xElement
                .AncestorsAndSelf()
                .Reverse()
                // ドキュメントルートも祖先に含まれてしまうので除外
                .Where(el => el != _xElement.Document?.Root);

            var prev = (AggregateBase?)null;

            foreach (var el in ancesotors) {
                var aggregate = _ctx.ToAggregateBase(el, prev);
                yield return aggregate;
                prev = aggregate;
            }
        }

        /// <summary>
        /// 子孫集約を列挙します。
        /// </summary>
        public IEnumerable<AggregateBase> EnumerateDescendants() {
            foreach (var child in GetMembers().OfType<AggregateBase>()) {
                yield return child;
                foreach (var descendant in child.EnumerateDescendants()) {
                    yield return descendant;
                }
            }
        }
        /// <summary>
        /// この集約と子孫集約を列挙します。
        /// </summary>
        public IEnumerable<AggregateBase> EnumerateThisAndDescendants() {
            yield return this;

            foreach (var descendant in EnumerateDescendants()) {
                yield return descendant;
            }
        }

        /// <summary>
        /// この集約が引数の集約の親か否かを返します。
        /// </summary>
        public bool IsParentOf(AggregateBase aggregate) {
            return aggregate._xElement.Parent == _xElement;
        }
        /// <summary>
        /// この集約が引数の集約の子か否かを返します。
        /// （<see cref="ChildAggregate"/> または <see cref="ChildrenAggregate"/> のいずれでもtrue）
        /// </summary>
        public bool IsChildOf(AggregateBase aggregate) {
            return _xElement.Parent == aggregate._xElement;
        }
        /// <summary>
        /// この集約が引数の集約の祖先か否かを返します。
        /// </summary>
        public bool IsAncestorOf(AggregateBase aggregate) {
            return aggregate._xElement.Ancestors().Contains(_xElement);
        }
        /// <summary>
        /// この集約が引数の集約の子孫か否かを返します。
        /// </summary>
        public bool IsDescendantOf(AggregateBase aggregate) {
            return aggregate._xElement.Descendants().Contains(_xElement);
        }
        #endregion 親子


        #region 外部参照
        /// <summary>
        /// この集約がメソッドの引数の集約の唯一のキーか否かを返します。
        /// なお、引数の集約がChildrenの場合、Childrenは親の主キーを継承するため、必ずfalseになります。
        /// </summary>
        /// <param name="refFrom">参照元</param>
        public bool IsSingleKeyOf(AggregateBase refFrom) {
            if (refFrom is ChildrenAggregate) return false;

            var keys = refFrom.GetOwnKeys().ToArray();
            if (keys.Length != 1) return false;
            if (keys[0] is not RefToMember rm) return false;
            if (rm.RefTo != this) return false;
            return true;
        }
        /// <summary>
        /// この集約を直接外部参照しているメンバーを列挙します。
        /// </summary>
        public IEnumerable<RefToMember> GetRefFroms() {
            return _ctx
                .FindRefFrom(_xElement)
                .Select(el => el == PreviousNode?.XElement
                    ? (RefToMember)PreviousNode // パスの巻き戻しの場合
                    : new RefToMember(el, _ctx, this));
        }
        #endregion 外部参照


        /// <summary>
        /// <see cref="ISchemaPathNode"/> としての経路情報をクリアした新しいインスタンスを返す
        /// </summary>
        public abstract AggregateBase AsEntry();


        #region 等価比較
        public override int GetHashCode() {
            return _xElement.GetHashCode();
        }
        public override bool Equals(object? obj) {
            return obj is AggregateBase agg
                && agg._xElement == _xElement;
        }
        public static bool operator ==(AggregateBase? left, AggregateBase? right) => ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.Equals(right);
        public static bool operator !=(AggregateBase? left, AggregateBase? right) => !(left == right);
        #endregion 等価比較

        public override string ToString() {
            // デバッグ用
            return $"{GetType().Name}({this.GetPathFromEntry().Select(x => x.XElement.Name.LocalName).Join(">")})";
        }
    }

    /// <summary>
    /// 集約ルート
    /// </summary>
    public class RootAggregate : AggregateBase {
        internal RootAggregate(XElement xElement, SchemaParseContext ctx, ISchemaPathNode? previous)
            : base(xElement, ctx, previous) { }

        public bool IsReadOnly => _xElement.Attribute(BasicNodeOptions.IsReadOnly.AttributeName) != null;
        public IModel Model => _ctx.TryGetModel(_xElement, out var model) ? model : throw new InvalidOperationException("ありえない");

        public override AggregateBase AsEntry() {
            return new RootAggregate(_xElement, _ctx, null);
        }

        #region DataModelと全く同じ型のQueryModel, CommandModel を生成するかどうか
        public bool GenerateDefaultQueryModel => _xElement.Attribute(BasicNodeOptions.GenerateDefaultQueryModel.AttributeName) != null;
        public bool GenerateBatchUpdateCommand => _xElement.Attribute(BasicNodeOptions.GenerateBatchUpdateCommand.AttributeName) != null;
        #endregion DataModelと全く同じ型のQueryModel, CommandModel を生成するかどうか
    }

    /// <summary>
    /// 親集約と1対1で対応する子集約。
    /// </summary>
    public class ChildAggregate : AggregateBase, IRelationalMember {
        internal ChildAggregate(XElement xElement, SchemaParseContext ctx, ISchemaPathNode? previous)
            : base(xElement, ctx, previous) { }

        public decimal Order => _xElement.ElementsBeforeSelf().Count();
        public AggregateBase Owner => _xElement.Parent == PreviousNode?.XElement
            ? (AggregateBase?)PreviousNode ?? throw new InvalidOperationException() // パスの巻き戻しの場合
            : _ctx.ToAggregateBase(_xElement.Parent ?? throw new InvalidOperationException(), this);

        /// <summary>画面上で追加削除されるタイミングが親と異なるかどうか</summary>
        public bool HasLifeCycle => _xElement.Attribute(BasicNodeOptions.HasLifeCycle.AttributeName) != null;

        AggregateBase IRelationalMember.MemberAggregate => this;

        public override AggregateBase AsEntry() {
            return new ChildAggregate(_xElement, _ctx, null);
        }
    }

    /// <summary>
    /// 親集約と1対多で対応する子集約。
    /// </summary>
    public class ChildrenAggregate : AggregateBase, IRelationalMember {
        internal ChildrenAggregate(XElement xElement, SchemaParseContext ctx, ISchemaPathNode? previous)
            : base(xElement, ctx, previous) { }

        public decimal Order => _xElement.ElementsBeforeSelf().Count();
        public AggregateBase Owner => _xElement.Parent == PreviousNode?.XElement
            ? (AggregateBase?)PreviousNode ?? throw new InvalidOperationException() // パスの巻き戻しの場合
            : _ctx.ToAggregateBase(_xElement.Parent ?? throw new InvalidOperationException(), this);
        AggregateBase IRelationalMember.MemberAggregate => this;

        public override AggregateBase AsEntry() {
            return new ChildrenAggregate(_xElement, _ctx, null);
        }

        /// <summary>
        /// Childrenのメンバーに対するループ処理をレンダリングするとき、
        /// そのループ変数として使うために "x", "x0", "x1", ... という名前を返す。
        /// 変数は、宣言方法に気を付ければ、同じ深さのChildrenが複数あっても衝突しない名前になる。
        /// </summary>
        public string GetLoopVarName(string alpha = "x") {
            // 深さ。ルート集約直下のChildrenのとき0になる
            var depth = _xElement.Ancestors().Count() - 2;

            return depth == 0 ? alpha : (alpha + depth);
        }
    }
}
