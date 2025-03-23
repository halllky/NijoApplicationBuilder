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

        internal AggregateBase(XElement xElement, SchemaParseContext ctx, PathStack pathStack) {
            _xElement = xElement;
            _ctx = ctx;
            Path = pathStack;
        }
        private protected readonly XElement _xElement;
        private protected readonly SchemaParseContext _ctx;

        /// <summary>
        /// エントリーからこの集約までのパス
        /// </summary>
        public PathStack Path { get; }

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
        /// <item>子(Child, Children): 列挙します。</item>
        /// <item>参照先(RefTo): 列挙します。</item>
        /// </list>
        /// </summary>
        public IEnumerable<IAggregateMember> GetMembers() {
            foreach (var el in _xElement.Elements()) {
                var nodeType = _ctx.GetNodeType(el);
                yield return nodeType switch {
                    SchemaParseContext.E_NodeType.ChildAggregate => new ChildAggreagte(el, _ctx, Path.Trace(el)),
                    SchemaParseContext.E_NodeType.ChildrenAggregate => new ChildrenAggreagte(el, _ctx, Path.Trace(el)),
                    SchemaParseContext.E_NodeType.Ref => new RefToMember(el, _ctx, Path.Trace(el)),
                    SchemaParseContext.E_NodeType.ValueMember => new ValueMember(el, _ctx, Path.Trace(el)),
                    SchemaParseContext.E_NodeType.StaticEnumValue => new Models.StaticEnumModelModules.StaticEnumValueDef(el, _ctx, Path.Trace(el)),
                    _ => throw new InvalidOperationException($"メンバーでない種類: {nodeType}（{el}）"),
                };
            }
        }
        /// <summary>
        /// 主キー項目のうち <see cref="ValueMember"/> を列挙します。
        /// つまりキーに <see cref="RefToMember"/> が含まれるならばそのRefのメンバーを列挙します。
        /// </summary>
        public IEnumerable<ValueMember> GetKeyMembers() {
            foreach (var member in GetMembers()) {
                if (member is ValueMember vm && vm.IsKey) {
                    yield return vm;

                } else if (member is RefToMember refTo && refTo.IsKey) {
                    foreach (var refToVm in refTo.RefTo.GetKeyMembers()) {
                        yield return refToVm;
                    }
                }
            }
        }

        /// <summary>
        /// この集約の親を返します。
        /// </summary>
        public AggregateBase? GetParent() {
            if (_xElement.Parent == null) return null;
            return _ctx.ToAggregateBase(_xElement.Parent, Path);
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
        internal RootAggregate(XElement xElement, SchemaParseContext ctx, PathStack pathStack)
            : base(xElement, ctx, pathStack) { }

        public string LatinName => _ctx.GetLatinName(_xElement);
        public bool IsReadOnly => _ctx.ParseIsAttribute(_xElement).Any(attr => attr.Key == "readonly"); // TODO ver.1

        public IModel Model => _ctx.FindModel(_xElement) ?? throw new InvalidOperationException();

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
        internal ChildAggreagte(XElement xElement, SchemaParseContext ctx, PathStack pathStack)
            : base(xElement, ctx, pathStack) { }

        public decimal Order => _ctx.GetIndexInSiblings(_xElement);
        public AggregateBase Owner => _ctx.ToAggregateBase(_xElement.Parent ?? throw new InvalidOperationException(), Path);
        public AggregateBase MemberAggregate => _ctx.ToAggregateBase(_xElement, Path);
    }

    /// <summary>
    /// 親集約と1対多で対応する子集約。
    /// </summary>
    public class ChildrenAggreagte : AggregateBase, IRelationalMember {
        internal ChildrenAggreagte(XElement xElement, SchemaParseContext ctx, PathStack pathStack)
            : base(xElement, ctx, pathStack) { }

        public decimal Order => _ctx.GetIndexInSiblings(_xElement);
        public AggregateBase Owner => _ctx.ToAggregateBase(_xElement.Parent ?? throw new InvalidOperationException(), Path);
        public AggregateBase MemberAggregate => _ctx.ToAggregateBase(_xElement, Path);
    }
}
