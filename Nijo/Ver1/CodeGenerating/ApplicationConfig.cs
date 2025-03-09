using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.CodeGenerating {
    /// <summary>
    /// スキーマ定義で設定できるアプリケーション単位のコンフィグ
    /// </summary>
    public class ApplicationConfig {

        /// <summary>
        /// C#側ソースコードの名前空間
        /// </summary>
        public required string RootNamespace { get; init; }
    }
}
