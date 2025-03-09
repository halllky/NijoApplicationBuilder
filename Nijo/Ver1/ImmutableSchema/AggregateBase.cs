using Nijo.Util.DotnetEx;
using Nijo.Ver1.MutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.ImmutableSchema {
    /// <summary>
    /// モデルの集約。
    /// 集約ルート, Child, Children, VariationItem のいずれか。
    /// </summary>
    public abstract class AggregateBase {

        #region static
        /// <summary>
        /// 集約ルート, Child, Children, VariationItem のいずれかを判断
        /// </summary>
        internal static AggregateBase Parse(GraphNode<MutableSchemaNode> schemaNode) {
            throw new NotImplementedException();
        }
        #endregion static


        internal AggregateBase(GraphNode<MutableSchemaNode> schemaNode) {
            _schemaNode = schemaNode;
        }
        private protected readonly GraphNode<MutableSchemaNode> _schemaNode;

        /// <summary>
        /// 物理名
        /// </summary>
        public string PhysicalName => _schemaNode.Item.GetPhysicalName();
        /// <summary>
        /// 表示用名称
        /// </summary>
        public string DisplayName => _schemaNode.Item.GetDisplayName();

        /// <summary>
        /// この集約が持つメンバーを列挙します。
        /// <list type="bullet">
        /// <item>親: 列挙しません。</item>
        /// <item>子(Child, Children, VariationItem): 列挙します。</item>
        /// <item>参照先(RefTo): 列挙します。</item>
        /// </list>
        /// </summary>
        public IEnumerable<IAggregateMember> GetMembers() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// この集約の親を返します。
        /// </summary>
        public AggregateBase? GetParent() {
            var parentChildEdge = _schemaNode.GetParent();
            if (parentChildEdge == null) return null;
            return Parse(parentChildEdge.Initial);
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
        internal AggregateBase GetRoot() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 子孫集約を列挙します。
        /// </summary>
        public IEnumerable<AggregateBase> EnumerateDescendants() {
            throw new NotImplementedException();
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
    }

    /// <summary>
    /// 集約ルート
    /// </summary>
    public class RootAggregate : AggregateBase {
        internal RootAggregate(GraphNode<MutableSchemaNode> schemaNode) : base(schemaNode) {
        }

        public string LatinName => throw new NotImplementedException();
        public bool IsReadOnly => _schemaNode.Item.IsAttributes.Contains("ここどうする？");

        public bool GenerateDefaultQueryModel { get; internal set; }
        public bool GenerateBatchUpdateCommand { get; internal set; }
    }

    /// <summary>
    /// 親集約と1対1で対応する子集約。
    /// </summary>
    public class ChildAggreagte : AggregateBase, IRelationalMember {
        internal ChildAggreagte(GraphEdge<MutableSchemaNode> relation) : base(relation.Terminal) {
            _relation = relation;
        }
        private readonly GraphEdge<MutableSchemaNode> _relation;

        public string RelationPhysicalName => throw new NotImplementedException("GraphEdgeの属性で定義されている物理名を返す");
        public decimal Order => _relation.Terminal.Item.GetIndexInSiblings();
        public AggregateBase Owner => Parse(_relation.Initial);
    }

    /// <summary>
    /// 親集約と1対多で対応する子集約。
    /// </summary>
    public class ChildrenAggreagte : AggregateBase, IRelationalMember {
        internal ChildrenAggreagte(GraphEdge<MutableSchemaNode> relation) : base(relation.Terminal) {
            _relation = relation;
        }
        private readonly GraphEdge<MutableSchemaNode> _relation;

        public string RelationPhysicalName => throw new NotImplementedException("GraphEdgeの属性で定義されている物理名を返す");
        public decimal Order => _relation.Terminal.Item.GetIndexInSiblings();
        public AggregateBase Owner => Parse(_relation.Initial);
    }
}
