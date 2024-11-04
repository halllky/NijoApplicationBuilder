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
        /// この項目が常に読み取り専用の場合は "<see cref="READONLY"/>" の定数文字列を返す。
        /// この項目が画面表示時の条件によって読み取り専用か否かが変わる場合はその条件が含まれた式を返す。
        /// この項目が読み取り専用にならない場合は空文字を返す。
        /// </summary>
        public required Func<AggregateMember.ValueMember, string> RenderReadOnlyStatement { get; init; }
        /// <summary>
        /// 自動生成されるアプリケーションのReactコンポーネントの入力コントロールは、
        /// それが読み取り専用か否かを表すboolean型のプロパティを持っている。
        /// そのプロパティの名前。
        /// </summary>
        internal const string READONLY = "readOnly";

        /// <summary>
        /// react-hook-form のフィールドパス取得処理。
        /// レンダリングするオブジェクト（検索条件のオブジェクトだったり詳細画面のオブジェクトだったりする）によってパスが変わるので。
        /// </summary>
        public required Func<AggregateMember.ValueMember, IEnumerable<string>> GetReactHookFormFieldPath { get; init; }

        /// <summary>
        /// 画面側ソースでReactコンポーネントに任意の属性を追加する。
        /// この処理は <see cref="IAggregateMemberType.RenderSingleViewVFormBody"/> の中で必ず呼ばれる必要がある。
        /// </summary>
        public Action<AggregateMember.ValueMember, List<string>>? EditComponentAttributes { get; init; }
    }
}
