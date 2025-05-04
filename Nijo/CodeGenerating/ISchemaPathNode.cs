using Nijo.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.CodeGenerating {
    /// <summary>
    /// 辿ってきた経路のノードになりうるオブジェクト
    /// </summary>
    public interface ISchemaPathNode {
        /// <summary>
        /// このノードと対応するXML要素
        /// </summary>
        XElement XElement { get; }
        /// <summary>
        /// 1つ前のノード
        /// </summary>
        ISchemaPathNode? PreviousNode { get; }


        #region empty
        /// <summary>
        /// 実質null
        /// </summary>
        public static ISchemaPathNode Empty = new EmptySchemaPathNode();
        private class EmptySchemaPathNode : ISchemaPathNode {
            XElement ISchemaPathNode.XElement => new XElement("Empty");
            ISchemaPathNode? ISchemaPathNode.PreviousNode => null;
        }
        #endregion empty
    }

    public static partial class SchemaPathNodeExtensions {

        /// <summary>
        /// エントリーを返す
        /// </summary>
        public static ISchemaPathNode GetEntry(this ISchemaPathNode node) {
            // 1個前のノードがnullならエントリー
            var current = node;
            while (current.PreviousNode != null) {
                current = current.PreviousNode;
            }
            return current;
        }

        /// <summary>
        /// エントリーからのパスを辿る。よりエントリーに近い方が先に列挙される
        /// </summary>
        public static IEnumerable<ISchemaPathNode> GetPathFromEntry(this ISchemaPathNode node) {
            var stack = new Stack<ISchemaPathNode>();
            stack.Push(node);

            var current = node;
            while (current.PreviousNode != null) {
                stack.Push(current.PreviousNode);
                current = current.PreviousNode;
            }

            // エントリーが先なのでLIFOで返す
            foreach (var item in stack) {
                yield return item;
            }
        }
    }
}
