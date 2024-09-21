using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient {
    /// <summary>
    /// VForm2のソースを組み立てる。
    /// VForm2.tsx と密接に関わっているのでそちらも併せて参照のこと
    /// </summary>
    internal class VerticalFormBuilder : VerticalFormSection {
        internal VerticalFormBuilder(string? label = null, E_VForm2LabelType? labelType = null, params string[] additionalAttributes) : base(label, labelType, additionalAttributes) {
        }

        /// <summary>
        /// VForm2.Root としてレンダリングします。
        /// VForm2.Indent としてレンダリングする場合は <see cref="VerticalFormSection.Render(CodeRenderingContext)"/> を使用のこと。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="label">ラベル</param>
        /// <param name="maxDepth">インデントの最大の深さ。未指定の場合は自動計算される。</param>
        /// <param name="estimatedLabelWidthRem">ラベル列の横幅。単位はCSSのrem。基本的にはそのフォーム中で登場する最も長い項目名の文字数</param>
        /// <returns></returns>
        public string RenderAsRoot(CodeRenderingContext context, string? label, int? maxDepth, decimal? estimatedLabelWidthRem) {
            var attrs = new List<string>();

            // ラベル
            if (label != null) {
                var escaped = label.Replace("\"", "&quot;");
                attrs.Add($"label=\"{escaped}\"");
            }

            // 深さ未指定の場合は自動計算
            maxDepth ??= _childItems
                .DefaultIfEmpty()
                .Max(x => x?.GetMaxDepth() ?? 0);
            attrs.Add($"maxDepth={{{maxDepth}}}");

            // ラベル列の横幅
            if (estimatedLabelWidthRem.HasValue) attrs.Add($"estimatedLabelWidth=\"{estimatedLabelWidthRem}rem\"");

            attrs.AddRange(_additionalAttributes);

            return $$"""
                <VForm2.Root {{attrs.Join(" ")}}>
                  {{WithIndent(RenderBody(context), "  ")}}
                </VForm2.Root>
                """;
        }


        /// <summary>
        /// VForm2では技術的な理由によりその実装の一部をコンテナクエリ(@container)で実装している。
        /// またその中身もCSSファイルに直で書くよりループを回して自動生成した方が楽な箇所があり、
        /// それをこのメソッドでレンダリングしている。
        /// </summary>
        internal static string RenderContainerQuery() {

            const int MAX_COLUMN = 5;  // 最大列数。通常のデスクトップPCならこの列数まで用意しておけば足りるだろう
            const int MAX_MEMBER = 100; // CSSクラスを生成する数。1つの親要素の直下に並ぶメンバーの限界値
            const int THRESHOLD = 320; // 列数が切り替わる閾値（px）

            return Enumerable.Range(1, MAX_COLUMN).SelectTextTemplate(col => {

                // minmaxはそのレイアウトが適用されるコンテナ横幅の最小から最大までの幅。
                // 例えば閾値が400pxの場合、各列数ごとの具体的な値は以下
                // 
                // COL: 1      2       3       4       5
                // -------------------------------------------
                // MIN: -    ,  800px, 1200px, 1600px, 2000px
                // MAX: 799px, 1199px, 1599px, 1999px, -

                var minmax = new List<string>();
                if (col > 1) minmax.Add($"(min-width: {(col - 1) * THRESHOLD + THRESHOLD}px)");
                if (col < MAX_COLUMN) minmax.Add($"(max-width: {col * THRESHOLD + THRESHOLD - 1}px)");

                return $$"""

                    /* VForm2: 横{{col}}列の場合のレイアウト */
                    @container {{minmax.Join(" and ")}} {
                      .vform-template-column {
                        grid-template-columns: calc((var(--vform-indent-size) * var(--vform-max-depth)) + var(--vform-label-width)) 1fr{{(col >= 2 ? $" repeat({col - 1}, var(--vform-label-width) 1fr)" : "")}};
                      }
                    {{Enumerable.Range(1, MAX_MEMBER).SelectTextTemplate(i => $$"""

                      .vform-vertical-{{i}}-items {
                        grid-template-rows: repeat({{Math.Ceiling((decimal)i / col)}}, auto);
                      }
                    """)}}
                    }
                    """;
            });
        }
    }

    internal interface IVerticalFormParts {
        int GetMaxDepth();
        IEnumerable<IVerticalFormParts> GetChildItems();
        string Render(CodeRenderingContext context);
    }

    /// <summary>
    /// VForm2 の入れ子セクション
    /// </summary>
    internal class VerticalFormSection : IVerticalFormParts {
        internal VerticalFormSection(string? label, E_VForm2LabelType? labelType, params string[] additionalAttributes) {
            _label = label;
            _labelType = labelType;
            _additionalAttributes = additionalAttributes;
        }

        protected readonly string[] _additionalAttributes;
        protected readonly string? _label;
        protected readonly E_VForm2LabelType? _labelType;
        protected readonly List<IVerticalFormParts> _childItems = new();

        /// <summary>このセクションに項目を追加します。</summary>
        internal void AddItem(bool wide, string? label, E_VForm2LabelType? labelType, string contents) {
            var item = new VerticalFormItem(wide, label, labelType, contents);
            _childItems.Add(item);
        }
        /// <summary>このセクションに入れ子の子セクションを追加します。</summary>
        internal VerticalFormSection AddSection(string? label, E_VForm2LabelType? labelType, params string[] additionalAttributes) {
            var section = new VerticalFormSection(label, labelType, additionalAttributes);
            _childItems.Add(section);
            return section;
        }
        /// <summary>このセクションに任意の項目を追加します。</summary>
        internal void AddUnknownParts(string sourceCode) {
            _childItems.Add(new UnknownFormItem(sourceCode));
        }

        int IVerticalFormParts.GetMaxDepth() {
            return _childItems.DefaultIfEmpty().Max(x => x?.GetMaxDepth() ?? 0) + 1;
        }
        IEnumerable<IVerticalFormParts> IVerticalFormParts.GetChildItems() {
            return _childItems;
        }

        public string Render(CodeRenderingContext context) {
            string label;
            if (_label == null) {
                label = string.Empty;
            } else if (_labelType == E_VForm2LabelType.String) {
                var escaped = _label.Replace("\"", "&quot;");
                label = $$"""
                     label="{{escaped}}"
                    """;
            } else {
                label = $$"""
                     label={{{_label}}}
                    """;
            }

            return $$"""
                <VForm2.Indent {{_additionalAttributes.Select(x => $"{x} ").Join(" ")}}{{label}}>
                  {{WithIndent(RenderBody(context), "  ")}}
                </VForm2.Indent>
                """;
        }
        /// <summary>
        /// 本体部分をレンダリングします。
        /// wideでない要素の塊はAutoColumnでラッピングしなければならないというルールを隠蔽する役割を持ちます。
        /// </summary>
        protected IEnumerable<string> RenderBody(CodeRenderingContext context) {
            var currentIsNoWideItem = false;
            foreach (var item in _childItems) {
                if (item is VerticalFormItem vItem && !vItem.IsWide) {
                    if (currentIsNoWideItem == false) {
                        yield return "<VForm2.AutoColumn>";
                        currentIsNoWideItem = true;
                    }
                } else {
                    if (currentIsNoWideItem == true) {
                        yield return "</VForm2.AutoColumn>";
                        currentIsNoWideItem = false;
                    }
                }
                yield return currentIsNoWideItem
                    ? $$"""
                          {{WithIndent(item.Render(context), "  ")}}
                        """
                    : item.Render(context);
            }
            if (currentIsNoWideItem) {
                yield return "</VForm2.AutoColumn>";
            }
        }
    }

    /// <summary>
    /// VForm2の末端項目
    /// </summary>
    internal class VerticalFormItem : IVerticalFormParts {
        internal VerticalFormItem(bool isWide, string? label, E_VForm2LabelType? labelType, string contents) {
            IsWide = isWide;
            _label = label;
            _labelType = labelType;
            _contents = contents;
        }
        /// <summary>この項目がフォームの横幅いっぱい占有する項目か否か</summary>
        internal bool IsWide { get; }
        private readonly string? _label;
        private readonly E_VForm2LabelType? _labelType;
        private readonly string _contents;

        int IVerticalFormParts.GetMaxDepth() {
            return 0;
        }
        IEnumerable<IVerticalFormParts> IVerticalFormParts.GetChildItems() {
            yield break;
        }
        string IVerticalFormParts.Render(CodeRenderingContext context) {
            string label;
            if (_label == null) {
                label = string.Empty;
            } else if (_labelType == E_VForm2LabelType.String) {
                var escaped = _label.Replace("\"", "&quot;");
                label = $$"""
                     label="{{escaped}}"
                    """;
            } else {
                label = $$"""
                     label={(
                      {{WithIndent(_label, "  ")}}
                    )}
                    """;
            }
            return $$"""
                <VForm2.Item{{(IsWide ? " wideValue" : "")}}{{label}}>
                  {{WithIndent(_contents, "  ")}}
                </VForm2.Item>
                """;
        }
    }

    /// <summary>
    /// 任意の要素
    /// </summary>
    internal class UnknownFormItem : IVerticalFormParts {

        internal UnknownFormItem(string sourceCode) {
            _sourceCode = sourceCode;
        }
        private readonly string _sourceCode;

        IEnumerable<IVerticalFormParts> IVerticalFormParts.GetChildItems() {
            yield break;
        }
        int IVerticalFormParts.GetMaxDepth() {
            return 0;
        }
        string IVerticalFormParts.Render(CodeRenderingContext context) {
            return _sourceCode;
        }
    }

    /// <summary>ラベルがただの文字列か JSX.Element か</summary>
    internal enum E_VForm2LabelType {
        String,
        JsxElement,
    }
}
