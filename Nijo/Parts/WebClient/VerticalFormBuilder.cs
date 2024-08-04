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
        internal VerticalFormBuilder() : base(null, null) {
        }

        public override string Render(CodeRenderingContext context) {
            var maxDepth = _childItems
                .DefaultIfEmpty()
                .Max(x => x?.GetMaxDepth() ?? 0);

            return $$"""
                <VForm2.Root maxDepth={{{maxDepth}}}>
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

            // 横5列までのレイアウトまで用意しておけば足りるだろう
            var columns = Enumerable.Range(1, 5).ToArray();

            // 1つの親要素の直下に並ぶメンバーの数としては20個まで考慮しておけばおおよそ足りるだろう
            var array = Enumerable.Range(1, 20).ToArray();

            return columns.SelectTextTemplate(colCount => {
                var isFirst = colCount == columns.Min();
                var isLast = colCount == columns.Max();

                // そのレイアウトが適用されるコンテナ横幅の最小から最大までの幅。各列数ごとの具体的な値は以下
                // 
                // COL: 1      2       3       4       5
                // -------------------------------------------
                // MIN: -    ,  800px, 1200px, 1600px, 2000px
                // MAX: 799px, 1199px, 1599px, 1999px, -
                //
                var minmax = new List<string>();
                if (!isFirst) minmax.Add($"(min-width: {(colCount - 1) * 400 + 400}px)");
                if (!isLast) minmax.Add($"(max-width: {colCount * 400 + 400 - 1}px)");

                return $$"""

                    /* VForm2: 横{{colCount}}列の場合のレイアウト */
                    @container {{minmax.Join(" and ")}} {
                      .vform-template-column {
                        grid-template-columns: calc((var(--vform-indent-size) * var(--vform-max-depth)) + var(--vform-label-width)) 1fr{{(colCount >= 2 ? $" repeat({colCount - 1}, var(--vform-label-width) 1fr)" : "")}};
                      }
                    {{array.SelectTextTemplate(i => $$"""

                      .vform-vertical-{{i}}-items {
                        grid-template-rows: repeat({{Math.Ceiling((decimal)i / colCount)}}, auto);
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
        internal VerticalFormSection(string? label, E_VForm2LabelType? labelType) {
            _label = label;
            _labelType = labelType;
        }

        protected readonly string? _label;
        protected readonly E_VForm2LabelType? _labelType;
        protected readonly List<IVerticalFormParts> _childItems = new();

        /// <summary>このセクションに項目を追加します。</summary>
        internal void AddItem(bool wide, string? label, E_VForm2LabelType? labelType, string contents) {
            var item = new VerticalFormItem(wide, label, labelType, contents);
            _childItems.Add(item);
        }
        /// <summary>このセクションに入れ子の子セクションを追加します。</summary>
        internal VerticalFormSection AddSection(string? label, E_VForm2LabelType? labelType) {
            var section = new VerticalFormSection(label, labelType);
            _childItems.Add(section);
            return section;
        }

        int IVerticalFormParts.GetMaxDepth() {
            return _childItems.DefaultIfEmpty().Max(x => x?.GetMaxDepth() ?? 0) + 1;
        }
        IEnumerable<IVerticalFormParts> IVerticalFormParts.GetChildItems() {
            return _childItems;
        }

        public virtual string Render(CodeRenderingContext context) {
            string label;
            if (_label == null) {
                label = string.Empty;
            } else if (_labelType == E_VForm2LabelType.String) {
                label = $$"""
                     label="{{_label}}"
                    """;
            } else {
                label = $$"""
                     label={(
                      {{WithIndent(_label, "  ")}}
                    )}
                    """;
            }

            return $$"""
                <VForm2.Indent{{label}}>
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
                label = $$"""
                     label="{{_label}}"
                    """;
            } else {
                label = $$"""
                     label={(
                      {{WithIndent(_label, "  ")}}
                    )}
                    """;
            }
            return $$"""
                <VForm2.Item{{(IsWide ? " wide" : "")}}{{label}}>
                  {{WithIndent(_contents, "  ")}}
                </VForm2.Item>
                """;
        }
    }

    /// <summary>ラベルがただの文字列か JSX.Element か</summary>
    internal enum E_VForm2LabelType {
        String,
        JsxElement,
    }
}
