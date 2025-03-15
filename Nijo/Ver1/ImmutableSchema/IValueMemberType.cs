using Nijo.Ver1.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.ImmutableSchema {
    /// <summary>
    /// モデルの属性の種類。
    /// 単語型、日付型、整数型、…など
    /// </summary>
    public interface IValueMemberType {
        /// <summary>
        /// XMLスキーマ定義でこの型を指定するときの型名
        /// </summary>
        string SchemaTypeName { get; }

        /// <summary>
        /// C#型名（ドメインロジック用）
        /// </summary>
        string CsDomainTypeName { get; }
        /// <summary>
        /// C#型名（EFCoreやJSONとの変換に用いられるプリミティブ型）
        /// </summary>
        string CsPrimitiveTypeName { get; }
        /// <summary>
        /// TypeScript型名
        /// </summary>
        string TsTypeName { get; }

        /// <summary>
        /// 型に由来する生成ソースがある場合はここでレンダリングする
        /// </summary>
        void GenerateCode(CodeRenderingContext ctx);
    }
}
