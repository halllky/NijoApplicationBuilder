using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Ver1.MutableSchema {
    /// <summary>
    /// スキーマ定義を構成するノード。
    /// そのノードが集約ルートであるか集約の属性のメンバーであるか等にかかわらず全ての属性を保持できる。
    /// スキーマ定義GUIなどでの使用を想定しているため、不正な状態（例えばChildrenなのに親を持たないなど）であっても許容する。
    /// <see cref="ImmutableSchema"/> 名前空間で使用される前には必ずエラーチェックがかかり、不正な状態は除外される。
    /// </summary>
    internal sealed class MutableSchemaNode : IGraphNode {

        internal MutableSchemaNode(XElement xElement, MutableSchemaNodeCollection collection) {
            _xElement = xElement;
            _collection = collection;
        }
        private readonly XElement _xElement;
        private readonly MutableSchemaNodeCollection _collection;

        /// <summary>
        /// スキーマ定義XML中でこのノードを識別するユニークな値。
        /// 集約にのみ定義され、集約メンバーには定義されない。
        /// </summary>
        internal string UniqueId => _xElement.Attribute("UniqueId")?.Value ?? string.Empty;
        NodeId IGraphNode.Id => new NodeId(UniqueId);

        /// <summary>
        /// 親ノードを返します。
        /// </summary>
        internal MutableSchemaNode? GetParent() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 物理名を返します。
        /// </summary>
        internal string GetPhysicalName() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 表示用名称を返します。
        /// </summary>
        internal string GetDisplayName() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// XMLのis属性で指定されたこのノードの種類を返します。
        /// 特定できない場合はnullを返します。
        /// </summary>
        internal string? GetNodeType() {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// このノードが兄弟要素の中で何番目かを返します。
        /// </summary>
        /// <returns>このノードがルート要素の場合は0を返す</returns>
        internal int GetIndexInSiblings() {
            return _xElement.ElementsBeforeSelf().Count();
        }

        internal IsAttribute IsAttributes => throw new NotImplementedException();
    }

    /// <summary>
    /// XMLのis属性
    /// </summary>
    internal class IsAttribute {

        internal bool Contains(string key) {
            throw new NotImplementedException();
        }

    }

    internal static class MutableSchemaNodeExtensions {
        internal const string REL_PARENT_CHILD = "REL_PARENT_CHILD";

        internal static GraphEdge<MutableSchemaNode>? GetParent(this GraphNode<MutableSchemaNode> schemaNode) {
            return schemaNode.In
                .SingleOrDefault(n => n.Attributes.TryGetValue(REL_PARENT_CHILD, out var val) && (bool)val)
                ?.As<MutableSchemaNode>();
        }
    }
}
