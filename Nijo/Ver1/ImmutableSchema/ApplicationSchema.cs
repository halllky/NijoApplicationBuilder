using Nijo.Ver1.MutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.ImmutableSchema {
    /// <summary>
    /// アプリケーションスキーマ。
    /// </summary>
    public class ApplicationSchema {

        /// <summary>
        /// <see cref="MutableSchemaNodeCollection"/> がスキーマ定義として不正な状態を持たないかを検証し、
        /// 検証に成功した場合はアプリケーションスキーマのインスタンスを返します。
        /// </summary>
        /// <param name="collection">入力検証前のスキーマノードの集合</param>
        /// <param name="schema">作成完了後のスキーマ</param>
        /// <param name="errors">エラーがある場合はここにその内容が格納される</param>
        /// <returns>スキーマの作成に成功したかどうか</returns>
        internal static bool TryBuild(MutableSchemaNodeCollection collection, out ApplicationSchema schema, out ICollection<string> errors) {
            throw new NotImplementedException();
        }

    }
}
