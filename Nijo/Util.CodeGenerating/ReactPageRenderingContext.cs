using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.CodeGenerating {
    /// <summary>
    /// Reactのページのソースコードのレンダリング処理は複数箇所に散らばることが多いので、
    /// それらの間で情報をやり取りするためのクラス。
    /// 例えば、React hook form のregister関数のインスタンス名など。
    /// </summary>
    internal class ReactPageRenderingContext {
        /// <summary>
        /// React hook form の register関数の実際の名前
        /// （そのときのソースコードの都合で変数名にエイリアスがついていることがあるので）
        /// </summary>
        public required string Register { get; init; }
    }
}
