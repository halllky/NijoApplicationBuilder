using Nijo.Models.ReadModel2Features;
using Nijo.Models.RefTo;
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
    internal interface IAggregateMemberType {
        string GetCSharpTypeName();
        string GetTypeScriptTypeName();

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
        /// 検索条件の絞り込み処理（WHERE句組み立て処理）をレンダリングします。
        /// </summary>
        /// <param name="member">検索対象のメンバーの情報</param>
        /// <param name="query"> <see cref="IQueryable{T}"/> の変数の名前</param>
        /// <param name="searchCondition">検索処理のパラメータの値の変数の名前</param>
        /// <param name="searchConditionObject">検索条件のオブジェクトの型</param>
        /// <param name="searchQueryObject">検索結果のクエリのオブジェクトの型</param>
        /// <returns> <see cref="IQueryable{T}"/> の変数に絞り込み処理をつけたものを再代入するソースコード</returns>
        string RenderFilteringStatement(AggregateMember.ValueMember member, string query, string searchCondition, E_SearchConditionObject searchConditionObject, E_SearchQueryObject searchQueryObject);
        /// <summary>
        /// 検索条件欄のUI（"VerticalForm.Item" の子要素）をレンダリングします。
        /// </summary>
        /// <param name="vm">検索対象のメンバーの情報</param>
        /// <param name="ctx">コンテキスト引数</param>
        /// <param name="searchConditionObject">検索条件のオブジェクトの型</param>
        string RenderSearchConditionVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx);
        /// <summary>
        /// 詳細画面のUI（"VerticalForm.Item" の子要素）をレンダリングします。
        /// </summary>
        /// <param name="vm">検索対象のメンバーの情報</param>
        /// <param name="ctx">コンテキスト引数</param>
        string RenderSingleViewVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx);

        /// <summary>
        /// ソース生成後プロジェクトでカスタマイズされた検索条件UIを使用する場合の、当該検索条件UIのReactコンポーネントのpropsのうち、
        /// どのコンポーネントでも必要な共通のプロパティを除いたものを "プロパティ名: 型名" の配列の形で列挙します。
        /// 未指定の場合はそういった追加のプロパティは特になし。
        /// </summary>
        IEnumerable<string> EnumerateSearchConditionCustomFormUiAdditionalProps() => [];
        /// <summary>
        /// ソース生成後プロジェクトでカスタマイズされた詳細画面フォームUIを使用する場合の、当該詳細画面フォームUIのReactコンポーネントのpropsのうち、
        /// どのコンポーネントでも必要な共通のプロパティを除いたものを "プロパティ名: 型名" の配列の形で列挙します。
        /// 未指定の場合はそういった追加のプロパティは特になし。
        /// </summary>
        IEnumerable<string> EnumerateSingleViewCustomFormUiAdditionalProps() => [];
    }

    /// <summary>検索条件のオブジェクトの型</summary>
    internal enum E_SearchConditionObject {
        /// <summary>検索条件の型は <see cref="Models.ReadModel2Features.SearchCondition"/> </summary>
        SearchCondition,
        /// <summary>検索条件の型は <see cref="Models.RefTo.RefSearchCondition"/> </summary>
        RefSearchCondition,
    }
    /// <summary>検索結果のクエリのオブジェクトの型</summary>
    internal enum E_SearchQueryObject {
        /// <summary>クエリのオブジェクトの型は <see cref="Models.WriteModel2Features.EFCoreEntity"/> </summary>
        EFCoreEntity,
        /// <summary>クエリのオブジェクトの型は <see cref="Models.ReadModel2Features.SearchResult"/> </summary>
        SearchResult,
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
        /// <summary>
        /// 入力に使われる React コンポーネントの名前
        /// </summary>
        protected abstract string ReactComponentName { get; }

        public virtual string GetCSharpTypeName() => "string";
        public virtual string GetTypeScriptTypeName() => "string";

        public virtual string GetSearchConditionCSharpType() => "string";
        public virtual string GetSearchConditionTypeScriptType() => "string";

        public virtual IGridColumnSetting GetGridColumnEditSetting() {
            return new TextColumnSetting {
            };
        }
        string IAggregateMemberType.RenderFilteringStatement(AggregateMember.ValueMember member, string query, string searchCondition, E_SearchConditionObject searchConditionObject, E_SearchQueryObject searchQueryObject) {
            var pathFromSearchCondition = searchConditionObject == E_SearchConditionObject.SearchCondition
                ? member.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.CSharp)
                : member.Declared.GetFullPathAsRefSearchConditionFilter(E_CsTs.CSharp);
            var fullpathNullable = $"{searchCondition}.{pathFromSearchCondition.Join("?.")}";
            var fullpathNotNull = $"{searchCondition}.{pathFromSearchCondition.Join(".")}";
            var method = SearchBehavior switch {
                E_SearchBehavior.PartialMatch => "Contains",
                E_SearchBehavior.ForwardMatch => "StartsWith",
                E_SearchBehavior.BackwardMatch => "EndsWith",
                _ => "Equals",
            };
            var whereFullpath = searchQueryObject == E_SearchQueryObject.SearchResult
                ? member.GetFullPathAsSearchResult(E_CsTs.CSharp, out var isArray)
                : member.GetFullPathAsDbEntity(E_CsTs.CSharp, out isArray);

            return $$"""
                if (!string.IsNullOrWhiteSpace({{fullpathNullable}})) {
                    var trimmed = {{fullpathNotNull}}.Trim();
                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{member.MemberName}}.{{method}}(trimmed)));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}}.{{method}}(trimmed));
                """)}}
                }
                """;
        }

        string IAggregateMemberType.RenderSearchConditionVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var attrs = new List<string>();
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");
            attrs.Add($"{{...{ctx.Register}(`{fullpath}`)}}");

            return $$"""
                <Input.Word {{attrs.Join(" ")}}/>
                """;
        }

        string IAggregateMemberType.RenderSingleViewVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var attrs = new List<string>();
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");
            attrs.Add($"{{...{ctx.Register}(`{fullpath}`)}}");

            attrs.Add(ctx.RenderReadOnlyStatement(vm.Declared));

            if (vm.Options.UiWidth != null) attrs.Add($"className=\"min-w-[{vm.Options.UiWidth.GetCssValue()}]\"");

            return $$"""
                <{{ReactComponentName}} {{attrs.Join(" ")}}/>
                {{ctx.RenderErrorMessage(vm)}}
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

        public virtual void GenerateCode(CodeRenderingContext context) { }

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

        string IAggregateMemberType.RenderFilteringStatement(AggregateMember.ValueMember member, string query, string searchCondition, E_SearchConditionObject searchConditionObject, E_SearchQueryObject searchQueryObject) {
            var pathFromSearchCondition = searchConditionObject == E_SearchConditionObject.SearchCondition
                ? member.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.CSharp)
                : member.Declared.GetFullPathAsRefSearchConditionFilter(E_CsTs.CSharp);
            var nullableFullPathFrom = $"{searchCondition}.{pathFromSearchCondition.Join("?.")}?.{FromTo.FROM}";
            var nullableFullPathTo = $"{searchCondition}.{pathFromSearchCondition.Join("?.")}?.{FromTo.TO}";
            var fullPathFrom = $"{searchCondition}.{pathFromSearchCondition.Join(".")}.{FromTo.FROM}";
            var fullPathTo = $"{searchCondition}.{pathFromSearchCondition.Join(".")}.{FromTo.TO}";
            var whereFullpath = searchQueryObject == E_SearchQueryObject.SearchResult
                ? member.GetFullPathAsSearchResult(E_CsTs.CSharp, out var isArray)
                : member.GetFullPathAsDbEntity(E_CsTs.CSharp, out isArray);

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
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{member.MemberName}} >= min && y.{{member.MemberName}} <= max));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}} >= min && x.{{whereFullpath.Join(".")}} <= max);
                """)}}

                } else if ({{nullableFullPathFrom}} != null) {
                    var from = {{fullPathFrom}};
                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{member.MemberName}} >= from));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}} >= from);
                """)}}

                } else if ({{nullableFullPathTo}} != null) {
                    var to = {{fullPathTo}};
                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{member.MemberName}} <= to));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}} <= to);
                """)}}
                }
                """;
        }

        /// <summary>コンポーネント名</summary>
        protected abstract string ComponentName { get; }
        /// <summary>レンダリングされるコンポーネントの属性をレンダリングします</summary>
        protected abstract IEnumerable<string> RenderAttributes();

        string IAggregateMemberType.RenderSearchConditionVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");

            return $$"""
                <div className="flex flex-nowrap items-center gap-1">
                  <{{ComponentName}} {...{{ctx.Register}}(`{{fullpath}}.{{FromTo.FROM_TS}}`)} {{RenderAttributes().Select(x => $"{x} ").Join("")}}/>
                  <span className="select-none">～</span>
                  <{{ComponentName}} {...{{ctx.Register}}(`{{fullpath}}.{{FromTo.TO_TS}}`)} {{RenderAttributes().Select(x => $"{x} ").Join("")}}/>
                </div>
                """;
        }

        string IAggregateMemberType.RenderSingleViewVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");

            var attrs = RenderAttributes().ToList();
            var readOnly = ctx.RenderReadOnlyStatement(vm.Declared);
            if (readOnly != null) attrs.Add(readOnly);

            if (vm.Options.UiWidth != null) attrs.Add($"className=\"min-w-[{vm.Options.UiWidth.GetCssValue()}]\"");

            return $$"""
                <{{ComponentName}} {...{{ctx.Register}}(`{{fullpath}}`)} {{attrs.Join(" ")}}/>
                {{ctx.RenderErrorMessage(vm)}}
                """;
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
