using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        internal static AggregateBase Parse(XElement xElement, SchemaParseContext ctx) {
            if (xElement.Parent == xElement.Document?.Root) {
                return new RootAggregate(xElement, ctx);
            }
            throw new NotImplementedException();
        }
        #endregion static


        internal AggregateBase(XElement xElement, SchemaParseContext ctx) {
            _xElement = xElement;
            _ctx = ctx;
        }
        private protected readonly XElement _xElement;
        private protected readonly SchemaParseContext _ctx;

        /// <summary>
        /// 物理名
        /// </summary>
        public string PhysicalName => _ctx.GetPhysicalName(_xElement);
        /// <summary>
        /// 表示用名称
        /// </summary>
        public string DisplayName => _ctx.GetDisplayName(_xElement);

        /// <summary>
        /// この集約が持つメンバーを列挙します。
        /// <list type="bullet">
        /// <item>親: 列挙しません。</item>
        /// <item>子(Child, Children, VariationItem): 列挙します。</item>
        /// <item>参照先(RefTo): 列挙します。</item>
        /// </list>
        /// </summary>
        public IEnumerable<IAggregateMember> GetMembers() {
            foreach (var el in _xElement.Elements()) {
                var nodeType = _ctx.GetNodeType(el);
                yield return nodeType switch {
                    SchemaParseContext.E_NodeType.ChildAggregate => new ChildAggreagte(el, _ctx),
                    SchemaParseContext.E_NodeType.ChildrenAggregate => new ChildrenAggreagte(el, _ctx),
                    SchemaParseContext.E_NodeType.Ref => new RefToMember(el, _ctx),
                    SchemaParseContext.E_NodeType.ValueMember => new ValueMember(el, _ctx),
                    _ => throw new InvalidOperationException($"メンバーでない種類: {nodeType}（{el}）"),
                };
            }
        }

        /// <summary>
        /// この集約の親を返します。
        /// </summary>
        public AggregateBase? GetParent() {
            if (_xElement.Parent == null) return null;
            return Parse(_xElement.Parent, _ctx);
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
            var aggregateRootElement = SchemaParseContext.GetAggregateRootElement(_xElement)
                ?? throw new InvalidOperationException();
            return Parse(aggregateRootElement, _ctx);
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
    }

    /// <summary>
    /// 集約ルート
    /// </summary>
    public class RootAggregate : AggregateBase {
        internal RootAggregate(XElement xElement, SchemaParseContext ctx) : base(xElement, ctx) {
        }

        public string LatinName => _ctx.GetLatinName(_xElement);
        public bool IsReadOnly => _ctx.ParseIsAttribute(_xElement).Any(attr => attr.Key == "readonly"); // TODO ver.1

        public IModel Model => _ctx.FindModel(_xElement) ?? throw new InvalidOperationException();

        public bool GenerateDefaultQueryModel { get; internal set; }
        public bool GenerateBatchUpdateCommand { get; internal set; }
    }

    /// <summary>
    /// 親集約と1対1で対応する子集約。
    /// </summary>
    public class ChildAggreagte : AggregateBase, IRelationalMember {
        internal ChildAggreagte(XElement xElement, SchemaParseContext ctx) : base(xElement, ctx) {
        }

        public string RelationPhysicalName => throw new NotImplementedException("GraphEdgeの属性で定義されている物理名を返す");
        public decimal Order => _ctx.GetIndexInSiblings(_xElement);
        public AggregateBase Owner => Parse(_xElement.Parent!, _ctx);
    }

    /// <summary>
    /// 親集約と1対多で対応する子集約。
    /// </summary>
    public class ChildrenAggreagte : AggregateBase, IRelationalMember {
        internal ChildrenAggreagte(XElement xElement, SchemaParseContext ctx) : base(xElement, ctx) {
        }

        public string RelationPhysicalName => throw new NotImplementedException("GraphEdgeの属性で定義されている物理名を返す");
        public decimal Order => _ctx.GetIndexInSiblings(_xElement);
        public AggregateBase Owner => Parse(_xElement.Parent!, _ctx);
    }
}
