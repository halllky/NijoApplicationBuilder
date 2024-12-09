using Nijo.Models.ReadModel2Features;
using Nijo.Models.RefTo;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts.BothOfClientAndServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core {
    internal interface IAggregateMemberType {
        /// <summary>
        /// nijo ui の画面上に表示される名前
        /// </summary>
        string GetUiDisplayName();
        /// <summary>
        /// 説明文。このメンバー型がどういった種類のデータを表すのか、
        /// 代表的な挙動や特徴的な挙動はどういったものなのかを記載してください。
        /// </summary>
        string GetHelpText();

        string GetCSharpTypeName();
        string GetTypeScriptTypeName();

        /// <summary>
        /// コード自動生成時に呼ばれる。C#の列挙体の定義を作成するなどの用途を想定している。
        /// </summary>
        void GenerateCode(CodeRenderingContext context) { }

        string GetSearchConditionCSharpType(AggregateMember.ValueMember vm);
        string GetSearchConditionTypeScriptType(AggregateMember.ValueMember vm);

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

        /// <summary>
        /// <see cref="Parts.WebClient.DataTable.CellType"/> で使用される列定義生成ヘルパーメソッドの名前
        /// </summary>
        string DataTableColumnDefHelperName { get; }
        /// <summary>
        /// <see cref="DataTableColumnDefHelperName"/> のメソッド本体をレンダリングします。
        /// </summary>
        Parts.WebClient.DataTable.CellType.Helper RenderDataTableColumnDefHelper(CodeRenderingContext ctx);
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
    internal abstract class StringMemberType : IAggregateMemberType {
        public abstract string GetUiDisplayName();
        public abstract string GetHelpText();

        /// <summary>
        /// 検索時の挙動。
        /// 既定値は <see cref="E_SearchBehavior.PartialMatch"/>
        /// </summary>
        protected virtual E_SearchBehavior GetSearchBehavior(AggregateMember.ValueMember vm) => E_SearchBehavior.PartialMatch;
        /// <summary>
        /// 複数行にわたる文字列になる可能性があるかどうか
        /// </summary>
        protected virtual bool MultiLine => false;
        /// <summary>
        /// 入力に使われる React コンポーネントの名前
        /// </summary>
        protected virtual string ReactComponentName => MultiLine
            ? "Input.Description"
            : "Input.Word";

        public virtual string GetCSharpTypeName() => "string";
        public virtual string GetTypeScriptTypeName() => "string";

        public virtual string GetSearchConditionCSharpType(AggregateMember.ValueMember vm) {
            return vm.Options.SearchBehavior == E_SearchBehavior.Range
                ? $"{FromTo.CLASSNAME}<string>"
                : $"string";
        }
        public virtual string GetSearchConditionTypeScriptType(AggregateMember.ValueMember vm) {
            return vm.Options.SearchBehavior == E_SearchBehavior.Range
                ? $"{{ {FromTo.FROM_TS}?: string, {FromTo.TO_TS}?: string }}"
                : $"string";
        }

        private protected virtual string RenderFilteringStatement(AggregateMember.ValueMember member, string query, string searchCondition, E_SearchConditionObject searchConditionObject, E_SearchQueryObject searchQueryObject) {
            var pathFromSearchCondition = searchConditionObject == E_SearchConditionObject.SearchCondition
                ? member.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.CSharp)
                : member.Declared.GetFullPathAsRefSearchConditionFilter(E_CsTs.CSharp);
            var whereFullpath = searchQueryObject == E_SearchQueryObject.SearchResult
                ? member.GetFullPathAsSearchResult(E_CsTs.CSharp, out var isArray)
                : member.GetFullPathAsDbEntity(E_CsTs.CSharp, out isArray);

            if (GetSearchBehavior(member) == E_SearchBehavior.Range) {
                var nullableFullPathFrom = $"{searchCondition}.{pathFromSearchCondition.Join("?.")}?.{FromTo.FROM}";
                var nullableFullPathTo = $"{searchCondition}.{pathFromSearchCondition.Join("?.")}?.{FromTo.TO}";
                var fullPathFrom = $"{searchCondition}.{pathFromSearchCondition.Join(".")}.{FromTo.FROM}";
                var fullPathTo = $"{searchCondition}.{pathFromSearchCondition.Join(".")}.{FromTo.TO}";
                return $$"""
                    if (!string.IsNullOrWhiteSpace({{nullableFullPathFrom}}) && !string.IsNullOrWhiteSpace({{nullableFullPathTo}})) {
                        // from, to のうち to の方が小さい場合は from-to を逆に読み替える
                        var min = string.Compare({{fullPathFrom}}, {{fullPathTo}}) < 0
                            ? {{fullPathFrom}}.Trim()
                            : {{fullPathTo}}.Trim();
                        var max = string.Compare({{fullPathFrom}}, {{fullPathTo}}) < 0
                            ? {{fullPathTo}}.Trim()
                            : {{fullPathFrom}}.Trim();
                    {{If(isArray, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => string.Compare(y.{{member.MemberName}}, min) >= 0 && string.Compare(y.{{member.MemberName}}, max) <= 0));
                    """).Else(() => $$"""
                        {{query}} = {{query}}.Where(x => string.Compare(x.{{whereFullpath.Join(".")}}, min) >= 0 && string.Compare(x.{{whereFullpath.Join(".")}}, max) <= 0);
                    """)}}

                    } else if (!string.IsNullOrWhiteSpace({{nullableFullPathFrom}})) {
                        var from = {{fullPathFrom}}.Trim();
                    {{If(isArray, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => string.Compare(y.{{member.MemberName}}, from) >= 0));
                    """).Else(() => $$"""
                        {{query}} = {{query}}.Where(x => string.Compare(x.{{whereFullpath.Join(".")}}, from) >= 0);
                    """)}}

                    } else if (!string.IsNullOrWhiteSpace({{nullableFullPathTo}})) {
                        var to = {{fullPathTo}}.Trim();
                    {{If(isArray, () => $$"""
                        {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => string.Compare(y.{{member.MemberName}}, to) <= 0));
                    """).Else(() => $$"""
                        {{query}} = {{query}}.Where(x => string.Compare(x.{{whereFullpath.Join(".")}}, to) <= 0);
                    """)}}
                    }
                    """;

            } else {
                var fullpathNullable = $"{searchCondition}.{pathFromSearchCondition.Join("?.")}";
                var fullpathNotNull = $"{searchCondition}.{pathFromSearchCondition.Join(".")}";
                var method = GetSearchBehavior(member) switch {
                    E_SearchBehavior.PartialMatch => "Contains",
                    E_SearchBehavior.ForwardMatch => "StartsWith",
                    E_SearchBehavior.BackwardMatch => "EndsWith",
                    _ => "Equals",
                };
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
        }
        string IAggregateMemberType.RenderFilteringStatement(AggregateMember.ValueMember member, string query, string searchCondition, E_SearchConditionObject searchConditionObject, E_SearchQueryObject searchQueryObject) {
            return RenderFilteringStatement(member, query, searchCondition, searchConditionObject, searchQueryObject);
        }

        string IAggregateMemberType.RenderSearchConditionVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var attrs = new List<string>();
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");

            if (vm.Options.UiWidth != null) {
                var rem = vm.Options.UiWidth.GetCssValue();
                attrs.Add($"className=\"min-w-[{rem}]\"");
                attrs.Add($"inputClassName=\"max-w-[{rem}] min-w-[{rem}]\"");
            }

            ctx.EditComponentAttributes?.Invoke(vm, attrs);

            if (GetSearchBehavior(vm) == E_SearchBehavior.Range) {
                return $$"""
                    <div className="flex flex-nowrap items-center gap-1">
                      <Input.Word {...{{ctx.Register}}(`{{fullpath}}.{{FromTo.FROM_TS}}`)} {{attrs.Join(" ")}}/>
                      <span className="select-none">～</span>
                      <Input.Word {...{{ctx.Register}}(`{{fullpath}}.{{FromTo.TO_TS}}`)} {{attrs.Join(" ")}}/>
                    </div>
                    """;

            } else {
                return $$"""
                    <Input.Word {...{{ctx.Register}}(`{{fullpath}}`)} {{attrs.Join(" ")}}/>
                    """;
            }
        }

        string IAggregateMemberType.RenderSingleViewVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var attrs = new List<string>();
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");
            attrs.Add($"{{...{ctx.Register}(`{fullpath}`)}}");

            attrs.Add(ctx.RenderReadOnlyStatement(vm.Declared));

            if (vm.Options.UiWidth != null) {
                var rem = vm.Options.UiWidth.GetCssValue();
                attrs.Add($"className=\"min-w-[{rem}]\"");
                attrs.Add($"inputClassName=\"max-w-[{rem}] min-w-[{rem}]\"");
            }

            ctx.EditComponentAttributes?.Invoke(vm, attrs);

            return $$"""
                <{{ReactComponentName}} {{attrs.Join(" ")}}/>
                {{ctx.RenderErrorMessage(vm)}}
                """;
        }

        public string DataTableColumnDefHelperName => MultiLine
            ? "multiLineText"
            : "text";
        Parts.WebClient.DataTable.CellType.Helper IAggregateMemberType.RenderDataTableColumnDefHelper(CodeRenderingContext ctx) {
            var returnType = $"{Parts.WebClient.DataTable.CellType.RETURNS_ONE_COLUMN}<TRow, {GetTypeScriptTypeName()} | undefined>";
            var body = $$"""
                /** 文字列 */
                const {{DataTableColumnDefHelperName}}: {{returnType}} = (header, getValue, setValue, opt) => ({
                  ...opt,
                  id: `${opt?.headerGroupName}::${header}`,
                  header,
                  render: row => <PlainCell>{getValue(row)}</PlainCell>,
                  onClipboardCopy: row => getValue(row) ?? '',
                  editSetting: opt?.readOnly === true ? undefined : {
                    type: {{(MultiLine ? "'multiline-text'" : "'text'")}},
                    readOnly: typeof opt?.readOnly === 'function'
                      ? opt.readOnly
                      : undefined,
                    onStartEditing: row => getValue(row),
                    onEndEditing: setValue,
                    onClipboardPaste: setValue,
                  },
                })
                """;
            return new() {
                FunctionName = DataTableColumnDefHelperName,
                ReturnType = returnType,
                Body = body,
            };
        }
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
        /// <summary>
        /// 範囲検索。
        /// 発行されるSQL文: WHERE DBの値 BETWEEN '検索条件1個目' AND '検索条件2個目'
        /// </summary>
        Range,
    }

    /// <summary>
    /// 数値や日付など連続した量をもつ値
    /// </summary>
    internal abstract class SchalarMemberType : IAggregateMemberType {
        public abstract string GetUiDisplayName();
        public abstract string GetHelpText();

        public virtual void GenerateCode(CodeRenderingContext context) { }

        public abstract string GetCSharpTypeName();
        public abstract string GetTypeScriptTypeName();

        public string GetSearchConditionCSharpType(AggregateMember.ValueMember vm) {
            var type = GetCSharpTypeName();
            return $"{FromTo.CLASSNAME}<{type}?>";
        }
        public string GetSearchConditionTypeScriptType(AggregateMember.ValueMember vm) {
            var type = GetTypeScriptTypeName();
            return $"{{ {FromTo.FROM_TS}?: {type}, {FromTo.TO_TS}?: {type} }}";
        }

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
        private protected abstract IEnumerable<string> RenderAttributes(AggregateMember.ValueMember vm, FormUIRenderingContext ctx);

        private protected virtual string RenderSearchConditionVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");

            return $$"""
                <div className="flex flex-nowrap items-center gap-1">
                  <{{ComponentName}} {...{{ctx.Register}}(`{{fullpath}}.{{FromTo.FROM_TS}}`)} {{RenderAttributes(vm, ctx).Select(x => $"{x} ").Join("")}}/>
                  <span className="select-none">～</span>
                  <{{ComponentName}} {...{{ctx.Register}}(`{{fullpath}}.{{FromTo.TO_TS}}`)} {{RenderAttributes(vm, ctx).Select(x => $"{x} ").Join("")}}/>
                </div>
                """;
        }
        string IAggregateMemberType.RenderSearchConditionVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) => RenderSearchConditionVFormBody(vm, ctx);

        string IAggregateMemberType.RenderSingleViewVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");

            var attrs = RenderAttributes(vm, ctx).ToList();
            var readOnly = ctx.RenderReadOnlyStatement(vm.Declared);
            if (readOnly != null) attrs.Add(readOnly);

            if (vm.Options.UiWidth != null) {
                var rem = vm.Options.UiWidth.GetCssValue();
                attrs.Add($"className=\"min-w-[{rem}]\"");
                attrs.Add($"inputClassName=\"max-w-[{rem}] min-w-[{rem}]\"");
            }

            ctx.EditComponentAttributes?.Invoke(vm, attrs);

            return $$"""
                <{{ComponentName}} {...{{ctx.Register}}(`{{fullpath}}`)} {{attrs.Join(" ")}}/>
                {{ctx.RenderErrorMessage(vm)}}
                """;
        }

        public abstract string DataTableColumnDefHelperName { get; }
        public abstract Parts.WebClient.DataTable.CellType.Helper RenderDataTableColumnDefHelper(CodeRenderingContext ctx);
    }
}
