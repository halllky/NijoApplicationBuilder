using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Ver1.CodeGenerating {
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
    }

    public static class SchemaPathNodeExtensions {
        /// <summary>
        /// エントリーからのパスを辿る。エントリーが先に列挙される
        /// </summary>
        public static IEnumerable<ISchemaPathNode> GetFullPathFromEntry(this ISchemaPathNode node) {
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
