using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.CodeGenerating {
    /// <summary>
    /// 1つのファイル内に複数の集約のソースが生成されるソースファイル
    /// </summary>
    public interface IMultiAggregateSourceFile {

        /// <summary>
        /// このソースファイルが必要とする他のソースファイルを登録します。
        /// </summary>
        void RegisterDependencies(IMultiAggregateSourceFileManager ctx);

        /// <summary>
        /// ソースコードの生成を実行します。
        /// </summary>
        void Render(CodeRenderingContext ctx);
    }

    /// <summary>
    /// <see cref="IMultiAggregateSourceFile"/> の状態を管理する
    /// </summary>
    public interface IMultiAggregateSourceFileManager {
        /// <inheritdoc cref="GeneratedProjectOptions"/>
        GeneratedProjectOptions Config { get; }
        /// <summary>
        /// 1つのファイルの中に複数の集約の情報が含まれるソースファイルを使用します。
        /// </summary>
        T Use<T>() where T : IMultiAggregateSourceFile, new();
    }
}
