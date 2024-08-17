using Nijo.Core;
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
        internal required CodeRenderingContext CodeRenderingContext { get; init; }
        /// <summary>
        /// React hook form の register関数の実際の名前
        /// （そのときのソースコードの都合で変数名にエイリアスがついていることがあるので）
        /// </summary>
        public required string Register { get; init; }

        public required E_ReactPageRenderingObjectType RenderingObjectType { get; init; }

        /// <summary>
        /// エラーメッセージ表示コンポーネントをレンダリングします。
        /// </summary>
        public required Func<AggregateMember.ValueMember, string> RenderErrorMessage { get; init; }

        /// <summary>
        /// 子配列コンポーネント以下で必要になる、配列中の何番目の要素かを表す変数のリスト
        /// </summary>
        public required IEnumerable<string> AncestorsIndexes { get; init; }
    }

    /// <summary>
    /// どの種類のオブジェクトをレンダリングしているか
    /// </summary>
    public enum E_ReactPageRenderingObjectType {
        SearchCondition,
        DataClassForDisplay,
        RefTarget,
    }
}
