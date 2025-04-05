using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Nijo.Ver1.ImmutableSchema {
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
                    SchemaParseContext.E_NodeType.ChildAggregate => new ChildAggreagte(el, _ctx, this),
                    SchemaParseContext.E_NodeType.ChildrenAggregate => new ChildrenAggreagte(el, _ctx, this),
                    SchemaParseContext.E_NodeType.Ref => new RefToMember(el, _ctx, this),
                    SchemaParseContext.E_NodeType.ValueMember => new ValueMember(el, _ctx, this),
                    SchemaParseContext.E_NodeType.StaticEnumValue => new Models.StaticEnumModelModules.StaticEnumValueDef(el, _ctx, this),
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
        /// つまりキーに <see cref="RefToMember"/> が含まれるならばそのRefのメンバーを列挙します。
        /// </summary>
        public IEnumerable<ValueMember> GetKeyVMs() {
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
            if (PreviousNode is AggregateBase agg && agg._xElement == this._xElement.Parent) {
                return agg;
            }

            // 子から親に辿る場合
            return _ctx.ToAggregateBase(_xElement.Parent ?? throw new InvalidOperationException(), this);
        }

        /// <summary>
        /// 祖先集約を列挙します。ルート集約が先
        /// </summary>
        public IEnumerable<AggregateBase> EnumerateAncestors() {
            throw new NotImplementedException();
        }
        /// <summary>
        /// この集約と祖先集約を列挙します。ルート集約が先
        /// </summary>
        public IEnumerable<AggregateBase> EnumerateThisAndAncestors() {
            yield return this;

            foreach (var ancestor in EnumerateAncestors()) {
                yield return ancestor;
            }
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
                var nodeType = _ctx.GetNodeType(el);
                AggregateBase aggregate = nodeType switch {
                    SchemaParseContext.E_NodeType.RootAggregate => new RootAggregate(el, _ctx, null),
                    SchemaParseContext.E_NodeType.ChildAggregate => new ChildAggreagte(el, _ctx, prev),
                    SchemaParseContext.E_NodeType.ChildrenAggregate => new ChildrenAggreagte(el, _ctx, prev),
                    _ => throw new InvalidOperationException($"不正なノード種別: {nodeType}({el})"),
                };
                yield return aggregate;
                prev = aggregate;
            }
        }

        /// <summary>
        /// 子孫集約を列挙します。
        /// </summary>
        public IEnumerable<AggregateBase> EnumerateDescendants() {
            return GetMembers().OfType<AggregateBase>();
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
            return aggregate._xElement.Parent == this._xElement;
        }
        /// <summary>
        /// この集約が引数の集約の子か否かを返します。
        /// （<see cref="ChildAggreagte"/> または <see cref="ChildrenAggreagte"/> のいずれでもtrue）
        /// </summary>
        public bool IsChildOf(AggregateBase aggregate) {
            return this._xElement.Parent == aggregate._xElement;
        }
        /// <summary>
        /// この集約が引数の集約の祖先か否かを返します。
        /// </summary>
        public bool IsAncestorOf(AggregateBase aggregate) {
            return aggregate._xElement.Ancestors().Contains(this._xElement);
        }
        #endregion 親子


        #region 外部参照
        /// <summary>
        /// この集約がメソッドの引数の集約の唯一のキーか否かを返します。
        /// </summary>
        /// <param name="refFrom">参照元</param>
        public bool IsSingleKeyOf(AggregateBase refFrom) {
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
                && agg._xElement == this._xElement;
        }
        public static bool operator ==(AggregateBase? left, AggregateBase? right) => ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.Equals(right);
        public static bool operator !=(AggregateBase? left, AggregateBase? right) => !(left == right);
        #endregion 等価比較

        public override string ToString() {
            // デバッグ用
            return $"{GetType().Name}({this.GetFullPath().Select(x => x.XElement.Name.LocalName).Join(">")})";
        }
    }

    /// <summary>
    /// 集約ルート
    /// </summary>
    public class RootAggregate : AggregateBase {
        internal RootAggregate(XElement xElement, SchemaParseContext ctx, ISchemaPathNode? previous)
            : base(xElement, ctx, previous) { }

        public string LatinName => _ctx.GetLatinName(_xElement);
        public bool IsReadOnly => _ctx.ParseIsAttribute(_xElement).Any(attr => attr.Key == "readonly"); // TODO ver.1

        public IModel Model => _ctx.FindModel(_xElement) ?? throw new InvalidOperationException();

        public override AggregateBase AsEntry() {
            return new RootAggregate(_xElement, _ctx, null);
        }

        #region DataModelと全く同じ型のQueryModel, CommandModel を生成するかどうか
        private const string IS_GENERATE_DEFAULT_QUERY_MODEL = "generate-default-query-model";
        private const string IS_GENERATE_BATCH_UPDATE_COMMAND = "generate-batch-update-command";
        public bool GenerateDefaultQueryModel => _ctx.ParseIsAttribute(_xElement).Any(attr => attr.Key == IS_GENERATE_DEFAULT_QUERY_MODEL);
        public bool GenerateBatchUpdateCommand => _ctx.ParseIsAttribute(_xElement).Any(attr => attr.Key == IS_GENERATE_BATCH_UPDATE_COMMAND);
        #endregion DataModelと全く同じ型のQueryModel, CommandModel を生成するかどうか
    }

    /// <summary>
    /// 親集約と1対1で対応する子集約。
    /// </summary>
    public class ChildAggreagte : AggregateBase, IRelationalMember {
        internal ChildAggreagte(XElement xElement, SchemaParseContext ctx, ISchemaPathNode? previous)
            : base(xElement, ctx, previous) { }

        public decimal Order => _ctx.GetIndexInSiblings(_xElement);
        public AggregateBase Owner => _xElement.Parent == PreviousNode?.XElement
            ? ((AggregateBase?)PreviousNode ?? throw new InvalidOperationException()) // パスの巻き戻しの場合
            : _ctx.ToAggregateBase(_xElement.Parent ?? throw new InvalidOperationException(), this);

        internal const string IS_HAS_LIFECYCLE = "has-lifecycle";
        /// <summary>画面上で追加削除されるタイミングが親と異なるかどうか</summary>
        public bool HasLifeCycle => _ctx.ParseIsAttribute(_xElement).Any(attr => attr.Key == IS_HAS_LIFECYCLE);

        AggregateBase IRelationalMember.MemberAggregate => this;

        public override AggregateBase AsEntry() {
            return new ChildAggreagte(_xElement, _ctx, null);
        }
    }

    /// <summary>
    /// 親集約と1対多で対応する子集約。
    /// </summary>
    public class ChildrenAggreagte : AggregateBase, IRelationalMember {
        internal ChildrenAggreagte(XElement xElement, SchemaParseContext ctx, ISchemaPathNode? previous)
            : base(xElement, ctx, previous) { }

        public decimal Order => _ctx.GetIndexInSiblings(_xElement);
        public AggregateBase Owner => _xElement.Parent == PreviousNode?.XElement
            ? ((AggregateBase?)PreviousNode ?? throw new InvalidOperationException()) // パスの巻き戻しの場合
            : _ctx.ToAggregateBase(_xElement.Parent ?? throw new InvalidOperationException(), this);
        AggregateBase IRelationalMember.MemberAggregate => this;

        public override AggregateBase AsEntry() {
            return new ChildrenAggreagte(_xElement, _ctx, null);
        }
    }
}
