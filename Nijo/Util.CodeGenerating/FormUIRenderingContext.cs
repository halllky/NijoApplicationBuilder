using Nijo.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.CodeGenerating {
    /// <summary>
    /// フォームUIのレンダリング処理において <see cref="IAggregateMemberType"/> 側が必要とするコンテキスト情報。
    /// 例えば、React hook form のregister関数のインスタンス名など。
    /// </summary>
    internal class FormUIRenderingContext {
        internal required CodeRenderingContext CodeRenderingContext { get; init; }
        /// <summary>
        /// React hook form の register関数の実際の名前
        /// （そのときのソースコードの都合で変数名にエイリアスがついていることがあるので）
        /// </summary>
        public required string Register { get; init; }

        /// <summary>
        /// エラーメッセージ表示コンポーネントをレンダリングします。
        /// </summary>
        public required Func<AggregateMember.ValueMember, string> RenderErrorMessage { get; init; }

        /// <summary>
        /// react-hook-form のフィールドパス取得処理。
        /// レンダリングするオブジェクト（検索条件のオブジェクトだったり詳細画面のオブジェクトだったりする）によってパスが変わるので。
        /// </summary>
        public required Func<AggregateMember.ValueMember, IEnumerable<string>> GetReactHookFormFieldPath { get; init; }
    }
}
