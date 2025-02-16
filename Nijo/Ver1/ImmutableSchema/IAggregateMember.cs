using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.MutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.ImmutableSchema {
    /// <summary>
    /// モデルの属性。
    /// xxxID, xxx名, xxx日付, ... などの単一の値、
    /// ref-toによる外部参照、
    /// ChildやChildrenといった子要素のうちのいずれか。
    /// </summary>
    public interface IAggregateMember {
        /// <summary>
        /// 物理名
        /// </summary>
        string PhysicalName { get; }

        /// <summary>
        /// 表示用名称
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// この属性を保持している集約
        /// </summary>
        AggregateBase Owner { get; }

        /// <summary>
        /// スキーマ定義でこのメンバーが定義されている順番。
        /// ref-toを辿るごとに小数桁が1桁ずつ下がっていくためにdecimal
        /// </summary>
        decimal Order { get; }
    }

    /// <summary>
    /// モデルの属性のうち、xxxID, xxx名, xxx日付, ... などのような単一の値。
    /// </summary>
    public sealed class ValueMember : IAggregateMember {
        internal ValueMember(GraphNode<MutableSchemaNode> schemaNode, CodeRenderingContext ctx) {
            _schemaNode = schemaNode;
            _ctx = ctx;
        }
        private readonly GraphNode<MutableSchemaNode> _schemaNode;
        private readonly CodeRenderingContext _ctx;

        public string PhysicalName => _schemaNode.Item.GetPhysicalName();
        public string DisplayName => _schemaNode.Item.GetDisplayName();
        public decimal Order => _schemaNode.Item.GetIndexInSiblings();

        public AggregateBase Owner {
            get {
                var parentNode = _schemaNode.In.Single().Initial.As<MutableSchemaNode>();
                return AggregateBase.Parse(parentNode);
            }
        }

        /// <summary>
        /// この属性の型
        /// </summary>
        public IValueMemberType Type {
            get {
                var typeKey = _schemaNode.Item.GetNodeType() ?? throw new InvalidOperationException();
                return _ctx.MemberTypeResolver.Resolve(typeKey);
            }
        }
    }

    /// <summary>
    /// モデルの属性のうち、外部参照。
    /// </summary>
    public class RefToMember : IAggregateMember {
        internal RefToMember(GraphEdge<MutableSchemaNode> relation) {
            _relation = relation;
        }

        private readonly GraphEdge<MutableSchemaNode> _relation;
        public string PhysicalName => _relation.Initial.Item.GetPhysicalName();
        public string DisplayName => _relation.Initial.Item.GetDisplayName();
        public decimal Order => _relation.Initial.Item.GetIndexInSiblings();

        public AggregateBase Owner {
            get {
                var owner = _relation.Initial.In.Single().As<MutableSchemaNode>().Initial;
                return AggregateBase.Parse(owner);
            }
        }
    }
}
