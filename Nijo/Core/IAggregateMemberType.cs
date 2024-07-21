using Nijo.Parts.Utility;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core {
    public interface IAggregateMemberType {
        SearchBehavior SearchBehavior { get; }

        string GetCSharpTypeName();
        string GetTypeScriptTypeName();

        string GetSearchConditionCSharpType();
        string GetSearchConditionTypeScriptType();

        /// <summary>
        /// 詳細画面用のReactの入力コンポーネントを設定するコードの詳細を行います。
        /// </summary>
        ReactInputComponent GetReactComponent();
        /// <summary>
        /// グリッド用のReactの入力コンポーネントを設定するコードの詳細を行います。
        /// </summary>
        IGridColumnSetting GetGridColumnEditSetting();

        /// <summary>
        /// コード自動生成時に呼ばれる。C#の列挙体の定義を作成するなどの用途を想定している。
        /// </summary>
        void GenerateCode(CodeRenderingContext context) { }
    }

    /// <summary>
    /// 数値や日付など連続した量をもつ値
    /// </summary>
    public abstract class SchalarMemberType : IAggregateMemberType {
        public SearchBehavior SearchBehavior => SearchBehavior.Range;

        public abstract string GetCSharpTypeName();
        public abstract string GetTypeScriptTypeName();

        public string GetSearchConditionCSharpType() {
            var type = GetCSharpTypeName();
            return $"{FromTo.CLASSNAME}<{type}?>";
        }
        public string GetSearchConditionTypeScriptType() {
            var type = GetTypeScriptTypeName();
            return $"{{ {FromTo.FROM_TS}?: {type}, {FromTo.TO_TS}?: {type} }}";
        }

        public abstract IGridColumnSetting GetGridColumnEditSetting();
        public abstract ReactInputComponent GetReactComponent();
    }

    /// <summary>
    /// 検索処理の挙動
    /// </summary>
    public enum SearchBehavior {
        /// <summary>
        /// 完全一致。
        /// 発行されるSQL文: WHERE DBの値 = 検索条件
        /// </summary>
        Strict,
        /// <summary>
        /// 部分一致。
        /// 発行されるSQL文: WHERE DBの値 LIKE '%検索条件%'
        /// </summary>
        PartialMatch,
        /// <summary>
        /// 前方一致。
        /// 発行されるSQL文: WHERE DBの値 LIKE '検索条件%'
        /// </summary>
        ForwardMatch,
        /// <summary>
        /// 後方一致。
        /// 発行されるSQL文: WHERE DBの値 LIKE '%検索条件'
        /// </summary>
        BackwardMatch,
        /// <summary>
        /// 範囲検索。
        /// 発行されるSQL文: WHERE DBの値 >= 検索条件.FROM
        ///                AND   DBの値 <= 検索条件.TO
        /// </summary>
        Range,
        /// <summary>
        /// 列挙体など。
        /// 発行されるSQL文: WHERE DBの値 IN (画面で選択された値1, 画面で選択された値2, ...)
        /// </summary>
        Contains,
    }
    /// <summary>
    /// 詳細画面用のReactの入力コンポーネント
    /// </summary>
    public sealed class ReactInputComponent {
        public required string Name { get; init; }
        public Dictionary<string, string> Props { get; init; } = [];

        /// <summary>
        /// <see cref="Props"/> をReactのコンポーネントのレンダリングの呼び出し時用の記述にして返す
        /// </summary>
        internal IEnumerable<string> GetPropsStatement() {
            foreach (var p in Props) {
                if (p.Value == string.Empty)
                    yield return $" {p.Key}";
                else if (p.Value.StartsWith("\"") && p.Value.EndsWith("\""))
                    yield return $" {p.Key}={p.Value}";
                else
                    yield return $" {p.Key}={{{p.Value}}}";
            }
        }
    }
    /// <summary>
    /// グリッド用のReactの入力コンポーネントの設定
    /// </summary>
    public interface IGridColumnSetting {
        /// <summary>
        /// セルの値を表示するとき、テキストボックスの初期値を設定するときに使われる文字列フォーマット処理。
        /// 第1引数はフォーマット前の値が入っている変数の名前、第2引数はフォーマット後の値が入る変数の名前。
        /// </summary>
        public Func<string, string, string>? GetValueFromRow { get; set; }
        /// <summary>
        /// テキストボックスで入力された文字列をデータクラスのプロパティに設定するときのフォーマット処理。
        /// 第1引数はフォーマット前の値が入っている変数の名前、第2引数はフォーマット後の値が入る変数の名前。
        /// </summary>
        public Func<string, string, string>? SetValueToRow { get; set; }
    }
    /// <summary>
    /// テキストボックスで編集するカラムの設定
    /// </summary>
    public sealed class TextColumnSetting : IGridColumnSetting {
        public Func<string, string, string>? GetValueFromRow { get; set; }
        public Func<string, string, string>? SetValueToRow { get; set; }
    }
    /// <summary>
    /// コンボボックスで編集するカラムの設定
    /// </summary>
    public sealed class ComboboxColumnSetting : IGridColumnSetting {
        public required string OptionItemTypeName { get; set; }
        public required string Options { get; set; }
        public required string EmitValueSelector { get; set; }
        public required string MatchingKeySelectorFromEmitValue { get; set; }
        public required string MatchingKeySelectorFromOption { get; set; }
        public required string TextSelector { get; set; }
        public required Func<string, string, string> OnClipboardCopy { get; set; }
        public required Func<string, string, string> OnClipboardPaste { get; set; }
        public Func<string, string, string>? GetDisplayText { get; set; }
        public Func<string, string, string>? GetValueFromRow { get; set; }
        public Func<string, string, string>? SetValueToRow { get; set; }
    }
    /// <summary>
    /// 非同期コンボボックスで編集するカラムの設定
    /// </summary>
    public sealed class AsyncComboboxColumnSetting : IGridColumnSetting {
        public required string OptionItemTypeName { get; set; }
        public required string QueryKey { get; set; }
        public required string Query { get; set; }
        public required string EmitValueSelector { get; set; }
        public required string MatchingKeySelectorFromEmitValue { get; set; }
        public required string MatchingKeySelectorFromOption { get; set; }
        public required string TextSelector { get; set; }
        public required Func<string, string, string> OnClipboardCopy { get; set; }
        public required Func<string, string, string> OnClipboardPaste { get; set; }
        public Func<string, string, string>? GetValueFromRow { get; set; }
        public Func<string, string, string>? SetValueToRow { get; set; }
    }
}
