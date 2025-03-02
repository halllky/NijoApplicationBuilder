using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.CodeGenerating {
    /// <summary>
    /// 1つのファイル内に複数の集約のソースが生成されるソースファイル
    /// </summary>
    public abstract class MultiAggregateSourceFileBase {

        internal MultiAggregateSourceFileBase(CodeRenderingContext ctx) {
            Context = ctx;
        }
        protected CodeRenderingContext Context { get; }

        /// <summary>
        /// このソースファイルが必要とする他のソースファイルを登録します。
        /// </summary>
        protected virtual void RegisterDependencies() { }

        /// <summary>
        /// このインスタンスが持つ状態を列挙します。
        /// <para>
        /// ある <see cref="MultiAggregateSourceFileBase"/> から別の <see cref="MultiAggregateSourceFileBase"/> が生成されるような、
        /// ソースコード間の複雑な依存関係を解決するための仕組みです。
        /// </para>
        /// <para>
        /// このメソッドは <see cref="RegisterDependencies"/> が実行される度に再評価され、
        /// <see cref="Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/> のロジックによって前回評価時の状態と比較されます。
        /// 前回評価時から差異があったインスタンスは再度 <see cref="RegisterDependencies"/> が実行されます。
        /// これを繰り返し、全ての <see cref="MultiAggregateSourceFileBase"/> の状態が確定して初めて、 <see cref="Render"/> が実行されます。
        /// </para>
        /// </summary>
        internal abstract IEnumerable<object?> EnumerateState();

        /// <summary>
        /// ソースコードの生成を実行します。
        /// </summary>
        private protected abstract void Render();
    }
}
