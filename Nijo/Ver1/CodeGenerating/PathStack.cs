using Nijo.Util.DotnetEx;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Nijo.Models.CommandModelFeatures.CommandParameter;

namespace Nijo.Ver1.CodeGenerating {
    /// <summary>
    /// <see cref="ImmutableSchema.AggregateBase"/> や
    /// <see cref="ImmutableSchema.IAggregateMember"/> の、
    /// エントリーからのパスの履歴情報。
    /// 
    /// 以下のようにオブジェクトからそのプロパティまでアクセスするソースを生成する際に、
    /// ルート集約からそのメンバーまでのパスが必要になるのでそれに使う。
    /// <code>
    /// x.Prop1.Prop2.Prop3 = y.Prop1.Prop2.Prop3.ToString();
    /// </code>
    /// </summary>
    public class PathStack {
        /// <summary>
        /// エントリーポイント
        /// </summary>
        public static PathStack Entry() {
            return new PathStack([]);
        }

        private PathStack(Stack<XElement> stack) {
            _stack = stack;
        }
        private readonly Stack<XElement> _stack;

        /// <summary>
        /// 引数のメンバーが末尾に積まれた状態の、
        /// 新しい <see cref="PathStack"/> のインスタンスを返します。
        /// なお、 a => b => c => b のように1つ前の要素に戻るよう指定された場合、
        /// 末尾に新しいパスを積むのではなく、現在のスタックの最後の要素がクリアされます。
        /// </summary>
        public PathStack Trace(XElement pushElement) {
            var stack = new Stack<XElement>(_stack);

            var last2 = stack.Reverse().Skip(1).FirstOrDefault();
            if (pushElement == last2) {
                // 巻き戻し
                stack.Pop();
            } else {
                // 末尾に新しいパスを積む
                stack.Push(pushElement);
            }
            return new PathStack(stack);
        }

        /// <summary>
        /// エントリーからのパスを列挙する。
        /// </summary>
        /// <param name="parentName">
        /// 子から親を辿るパスが表れたときの、親を表す名前。
        /// 未指定の場合に子から親を辿るパスが表れた場合は例外を送出する。
        /// </param>
        /// <returns>区切り文字で結合される前のパス</returns>
        public IEnumerable<string> GetFullPathFromEntry(string? parentName = null) {
            var previous = (XElement?)null;
            foreach (var current in _stack) {
                if (current == previous?.Parent) {
                    // 子から親を辿る場合はルールが特殊
                    if (parentName == null) throw new InvalidOperationException("子から親を辿るパスが検出されました。");
                    yield return parentName;

                } else {
                    // スキーマ解析コンテキストの物理名算出処理と合わせる必要あり
                    yield return current.Name.LocalName;
                }
                previous = current;
            }
        }
        /// <summary>
        /// Refエントリー（全体のエントリーのXML要素のツリーの外に出る境界のXML要素）以降のパスを返す。
        /// </summary>
        /// <param name="parentName">
        /// 子から親を辿るパスが表れたときの、親を表す名前。
        /// 未指定の場合に子から親を辿るパスが表れた場合は例外を送出する。
        /// </param>
        /// <returns>区切り文字で結合される前のパス</returns>
        public IEnumerable<string> GetFullPathFromRefEntry(string? parentName = null) {
            if (_stack.Count == 0) yield break;

            // スタックを逆順に辿ったときのXML要素の親（XDocument.Rootの1つ下）を取得する。
            // パスの経路がこの要素の子孫から外に出た瞬間がRefエントリーとなる。
            var rootOfEntry = _stack.Reverse().First().AncestorsAndSelf().Last();

            var previous = (XElement?)null;
            foreach (var current in _stack) {

                // 全体のエントリーのXML要素の子孫ならば列挙せず次の要素へ
                var root = current.AncestorsAndSelf().Last();
                if (root == rootOfEntry) continue;

                if (current == previous?.Parent) {
                    // 子から親を辿る場合はルールが特殊
                    if (parentName == null) throw new InvalidOperationException("子から親を辿るパスが検出されました。");
                    yield return parentName;

                } else {
                    // スキーマ解析コンテキストの物理名算出処理と合わせる必要あり
                    yield return current.Name.LocalName;
                }
                previous = current;
            }
        }

        public override string ToString() {
            return GetFullPathFromEntry().Join(".");
        }
    }
}
