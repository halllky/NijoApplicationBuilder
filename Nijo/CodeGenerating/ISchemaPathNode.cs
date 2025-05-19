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

        /// <summary>
        /// 指定した要素が、ツリールート（XMLルート要素の直下要素）を起点とした
        /// ツリー内で何番目に現れるかを計算します。
        /// </summary>
        /// <param name="targetElement">順序を調べたい要素</param>
        /// <returns>要素の出現順序（0ベース）。要素が見つからない場合は-1</returns>
        public static int GetOrderInTree(this ISchemaPathNode node) {
            // ツリールートを特定する
            XElement? treeRoot = GetTreeRoot(node.XElement);
            if (treeRoot == null) return -1;

            // 深さ優先探索で要素の順序を計算
            int order = 0;
            return FindElementOrder(treeRoot, node.XElement, ref order);
        }

        /// <summary>
        /// 指定した要素が属するツリーのルート要素を取得します。
        /// （XMLルート要素の直下要素がツリールートとなります）
        /// </summary>
        /// <param name="element">起点となる要素</param>
        /// <returns>ツリールート要素</returns>
        private static XElement? GetTreeRoot(XElement element) {
            XElement current = element;

            // XMLルート要素まで遡る
            while (current.Parent != null) {
                current = current.Parent;
            }

            // XMLルート要素から、targetElementが属する直下の要素を見つける
            XElement xmlRoot = current;
            XElement? ancestor = element;

            while (ancestor.Parent != xmlRoot) {
                ancestor = ancestor.Parent;
                if (ancestor == null)
                    return null;
            }

            return ancestor;
        }

        /// <summary>
        /// 深さ優先探索で要素の順序を見つけます。
        /// </summary>
        /// <param name="currentElement">現在探索中の要素</param>
        /// <param name="targetElement">探している要素</param>
        /// <param name="order">現在の順序番号（参照渡し）</param>
        /// <returns>要素が見つかった場合はその順序、見つからない場合は-1</returns>
        static int FindElementOrder(XElement currentElement, XElement targetElement, ref int order) {
            // 現在の要素が目標要素と同じかチェック
            if (ReferenceEquals(currentElement, targetElement)) {
                return order;
            }

            // 順序番号をインクリメント
            order++;

            // 子要素を深さ優先で探索
            foreach (XElement child in currentElement.Elements()) {
                int result = FindElementOrder(child, targetElement, ref order);
                if (result != -1) {
                    return result;
                }
            }

            return -1;
        }
    }
}
