using Nijo.Models.ReadModel2Features;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts.Utility;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core {
    public interface IAggregateMemberType {
        /// <summary>
        /// TODO: 廃止予定
        /// </summary>
        SearchBehavior SearchBehavior { get; }

        string GetCSharpTypeName();
        string GetTypeScriptTypeName();

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

        string GetSearchConditionCSharpType();
        string GetSearchConditionTypeScriptType();

        /// <summary>
        /// 検索条件の絞り込み処理をレンダリングします。
        /// </summary>
        /// <param name="member">検索対象のメンバーの情報</param>
        /// <param name="query"> <see cref="IQueryable{T}"/> の変数の名前</param>
        /// <param name="searchCondition">検索処理のパラメータの値の変数の名前</param>
        /// <returns> <see cref="IQueryable{T}"/> の変数に絞り込み処理をつけたものを再代入するソースコード</returns>
        string RenderFilteringStatement(SearchConditionMember member, string query, string searchCondition);
    }

    /// <summary>
    /// 文字列系メンバー型
    /// </summary>
    public abstract class StringMemberType : IAggregateMemberType {

        /// <summary>
        /// 検索時の挙動。
        /// 既定値は <see cref="E_SearchBehavior.PartialMatch"/>
        /// </summary>
        protected virtual E_SearchBehavior SearchBehavior { get; } = E_SearchBehavior.PartialMatch;
        SearchBehavior IAggregateMemberType.SearchBehavior => SearchBehavior switch {
            E_SearchBehavior.PartialMatch => Core.SearchBehavior.PartialMatch,
            E_SearchBehavior.ForwardMatch => Core.SearchBehavior.ForwardMatch,
            E_SearchBehavior.BackwardMatch => Core.SearchBehavior.BackwardMatch,
            _ => Core.SearchBehavior.Strict,
        };

        public virtual string GetCSharpTypeName() => "string";
        public virtual string GetTypeScriptTypeName() => "string";

        public virtual string GetSearchConditionCSharpType() => "string";
        public virtual string GetSearchConditionTypeScriptType() => "string";

        public virtual ReactInputComponent GetReactComponent() {
            return new ReactInputComponent { Name = "Input.Word" };
        }

        public virtual IGridColumnSetting GetGridColumnEditSetting() {
            return new TextColumnSetting {
            };
        }
        public string RenderFilteringStatement(SearchConditionMember member, string query, string searchCondition) {
            var isArray = member.Member.Owner.EnumerateAncestorsAndThis().Any(a => a.IsChildrenMember());
            var path = member.Member.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.CSharp);
            var fullpathNullable = $"{searchCondition}.{path.Join("?.")}";
            var fullpathNotNull = $"{searchCondition}.{path.Join(".")}";
            var entityOwnerPath = member.Member.Owner.GetFullPathAsDbEntity().Join(".");
            var entityMemberPath = member.Member.GetFullPathAsDbEntity().Join(".");
            var method = SearchBehavior switch {
                E_SearchBehavior.PartialMatch => "Contains",
                E_SearchBehavior.ForwardMatch => "StartsWith",
                E_SearchBehavior.BackwardMatch => "EndsWith",
                _ => "Equals",
            };

            return $$"""
                if (!string.IsNullOrWhiteSpace({{fullpathNullable}})) {
                    var trimmed = {{fullpathNotNull}}.Trim();
                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{entityOwnerPath}}.Any(y => y.{{member.MemberName}}.{{method}}(trimmed)));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{entityMemberPath}}.{{method}}(trimmed));
                """)}}
                }
                """;
        }

        /// <summary>
        /// 文字列検索の挙動
        /// </summary>
        public enum E_SearchBehavior {
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
        }
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

        public string RenderFilteringStatement(SearchConditionMember member, string query, string searchCondition) {
            var isArray = member.Member.Owner.EnumerateAncestorsAndThis().Any(a => a.IsChildrenMember());
            var path = member.Member.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.CSharp);
            var nullableFullPathFrom = $"{searchCondition}.{path.Join("?.")}.{FromTo.FROM}";
            var nullableFullPathTo = $"{searchCondition}.{path.Join("?.")}.{FromTo.TO}";
            var fullPathFrom = $"{searchCondition}.{path.Join(".")}.{FromTo.FROM}";
            var fullPathTo = $"{searchCondition}.{path.Join(".")}.{FromTo.TO}";
            var ownerPath = member.Member.Owner.GetFullPathAsDbEntity().Join(".");
            var memberPath = member.Member.GetFullPathAsDbEntity().Join(".");
            return $$"""
                if ({{nullableFullPathFrom}} != null && {{nullableFullPathTo}} != null) {
                    // from, to のうち to の方が小さい場合は from-to を逆に読み替える
                    var min = {{fullPathFrom}} < {{fullPathTo}}
                        ? {{fullPathFrom}}
                        : {{fullPathTo}};
                    var max = {{fullPathFrom}} < {{fullPathTo}}
                        ? {{fullPathTo}}
                        : {{fullPathFrom}};
                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{ownerPath}}.Any(y =>
                        y.{{member.MemberName}} >= min &&
                        y.{{member.MemberName}} <= max));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x =>
                        x.{{memberPath}} >= min &&
                        x.{{memberPath}} <= max);
                """)}}

                } else if ({{nullableFullPathFrom}} != null) {
                    var from = {{fullPathFrom}};
                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{ownerPath}}.Any(y => y.{{member.MemberName}} >= from));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{memberPath}} >= from);
                """)}}

                } else if ({{nullableFullPathTo}} != null) {
                    var to = {{fullPathTo}};
                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{ownerPath}}.Any(y => y.{{member.MemberName}} <= to));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{memberPath}} <= to);
                """)}}
                }
                """;
        }
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
