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
    internal abstract class VForm2 {

        /// <summary>
        /// VForm2.Root
        /// </summary>
        internal class RootNode : VForm2 {
            internal RootNode(Label? label, int? maxDepth, decimal? estimatedLabelWidthRem, params string[] additionalAttributes) {
                _label = label;
                _maxDepth = maxDepth;
                _estimatedLabelWidthRem = estimatedLabelWidthRem;
                _additionalAttributes = additionalAttributes;
            }
            private readonly Label? _label;
            private readonly int? _maxDepth;
            private readonly decimal? _estimatedLabelWidthRem;
            private readonly string[] _additionalAttributes;

            protected override bool ShouldWrapAutoColumn => false;

            internal override string Render(CodeRenderingContext ctx) {
                var attrs = new List<string>();

                if (_label != null) {
                    attrs.Add(_label.Render(ctx));
                }
                if (_maxDepth != null) {
                    attrs.Add($"maxDepth={{{_maxDepth}}}");
                } else {
                    attrs.Add($"maxDepth={{{GetMaxIndentDepth()}}}");
                }
                if (_estimatedLabelWidthRem != null) {
                    attrs.Add($"estimatedLabelWidth=\"{_estimatedLabelWidthRem}rem\"");
                }

                attrs.AddRange(_additionalAttributes);

                return $$"""
                    <VForm2.Root{{attrs.Select(x => " " + x).Join("")}}>
                      {{WithIndent(RenderChildren(ctx), "  ")}}
                    </VForm2.Root>
                    """;
            }
        }

        /// <summary>
        /// VForm2.Indent
        /// </summary>
        internal class IndentNode : VForm2 {
            internal IndentNode(Label? label, params string[] additionalAttributes) {
                _label = label;
                _additionalAttributes = additionalAttributes;
            }
            private readonly Label? _label;
            private readonly string[] _additionalAttributes;

            protected override bool ShouldWrapAutoColumn => false;

            internal override string Render(CodeRenderingContext ctx) {
                var attrs = new List<string>();

                if (_label != null) {
                    attrs.Add(_label.Render(ctx));
                }
                attrs.AddRange(_additionalAttributes);

                return $$"""
                    <VForm2.Indent{{attrs.Select(x => " " + x).Join("")}}>
                      {{WithIndent(RenderChildren(ctx), "  ")}}
                    </VForm2.Indent>
                    """;
            }
        }

        /// <summary>
        /// VForm2.Item
        /// </summary>
        internal class ItemNode : VForm2 {
            internal ItemNode(Label? label, bool isWide, string contents, params string[] additionalAttributes) {
                _label = label;
                _isWide = isWide;
                _contents = contents;
                _additionalAttributes = additionalAttributes;
            }
            private readonly Label? _label;
            private readonly bool _isWide;
            private readonly string _contents;
            private readonly string[] _additionalAttributes;

            protected override bool ShouldWrapAutoColumn => !_isWide;

            internal override VForm2 Append(VForm2 node) {
                throw new InvalidOperationException($"{nameof(ItemNode)}には子要素を追加できない");
            }
            internal override string Render(CodeRenderingContext ctx) {
                var attrs = new List<string>();

                if (_isWide) {
                    attrs.Add("wideValue");
                }
                if (_label != null) {
                    attrs.Add(_label.Render(ctx));
                }
                attrs.AddRange(_additionalAttributes);

                return $$"""
                    <VForm2.Item{{attrs.Select(x => " " + x).Join("")}}>
                      {{WithIndent(_contents, "  ")}}
                    </VForm2.Item>
                    """;
            }
        }

        /// <summary>
        /// 任意のReactNode
        /// </summary>
        internal class UnknownNode : VForm2 {
            internal UnknownNode(string sourceCode, bool isWide) {
                _sourceCode = sourceCode;
                _isWide = isWide;
            }
            private readonly string _sourceCode;
            private readonly bool _isWide;

            protected override bool ShouldWrapAutoColumn => !_isWide;

            internal override VForm2 Append(VForm2 node) {
                throw new InvalidOperationException($"{nameof(UnknownNode)}には子要素を追加できない");
            }
            internal override string Render(CodeRenderingContext ctx) {
                return _sourceCode;
            }
        }


        private VForm2() { }
        private readonly List<VForm2> _childNodes = new();

        /// <summary>
        /// このノードの子要素を追加します。
        /// </summary>
        /// <returns>このインスタンス自身を返します。</returns>
        internal virtual VForm2 Append(VForm2 node) {
            _childNodes.Add(node);
            return this;
        }

        #region AutoColumn
        /// <summary>
        /// 適宜 VForm.AutoColumn でラッピングしながら子要素をレンダリングします。
        /// </summary>
        protected string RenderChildren(CodeRenderingContext ctx) {
            var sourceCode = new StringBuilder();
            bool isInAutoColumn = false;
            foreach (var child in _childNodes) {
                if (child.ShouldWrapAutoColumn) {
                    if (!isInAutoColumn) {
                        sourceCode.AppendLine("<VForm2.AutoColumn>");
                        isInAutoColumn = true;
                    }
                    sourceCode.AppendLine($$"""
                          {{WithIndent(child.Render(ctx), "  ")}}
                        """);

                } else {
                    if (isInAutoColumn) {
                        sourceCode.AppendLine("</VForm2.AutoColumn>");
                        isInAutoColumn = false;
                    }
                    sourceCode.AppendLine($$"""
                        {{child.Render(ctx)}}
                        """);
                }
            }
            if (isInAutoColumn) {
                sourceCode.AppendLine("</VForm2.AutoColumn>");
            }
            return sourceCode.ToString().TrimEnd();
        }
        /// <summary>
        /// このノードが VForm.AutoColumn でラッピングされるか否かを返します。
        /// </summary>
        protected abstract bool ShouldWrapAutoColumn { get; }
        #endregion AutoColumn

        #region 深さ計算
        protected int GetMaxIndentDepth() {
            var maxDepthChildren = _childNodes
                .OfType<IndentNode>()
                .ToArray();

            if (maxDepthChildren.Length == 0) {
                return 0;
            } else {
                return maxDepthChildren.Max(node => node.GetMaxIndentDepth()) + 1;
            }
        }
        #endregion 深さ計算

        /// <summary>
        /// VForm2のソースコードをレンダリングします。
        /// </summary>
        internal abstract string Render(CodeRenderingContext ctx);


        #region ラベル
        internal abstract class Label {
            internal abstract string Render(CodeRenderingContext ctx);
        }
        internal class StringLabel : Label {
            internal StringLabel(string value) => _value = value;
            private readonly string _value;
            internal override string Render(CodeRenderingContext ctx) {
                return $$"""
                    label="{{_value.Replace("\"", "&quot;")}}"
                    """;
            }
        }
        internal class JSXElementLabel : Label {
            internal JSXElementLabel(string value) => _value = value;
            private readonly string _value;
            internal override string Render(CodeRenderingContext ctx) {
                return $$"""
                    label={{{WithIndent(_value, "")}}}
                    """;
            }
        }
        #endregion ラベル


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
                // MAX: 800px, 1200px, 1600px, 2000px, -

                var minmax = new List<string>();
                if (col > 1) minmax.Add($"(min-width: {(col - 1) * THRESHOLD + THRESHOLD}px)");
                if (col < MAX_COLUMN) minmax.Add($"(max-width: {col * THRESHOLD + THRESHOLD}px)");

                return $$"""

                    /* VForm2: 横{{col}}列の場合のレイアウト */
                    @container {{minmax.Join(" and ")}} {
                      .vform-template-column {
                        grid-template-columns: calc((1px * var(--vform-max-depth)) + var(--vform-label-width)) 1fr{{(col >= 2 ? $" repeat({col - 1}, var(--vform-label-width) 1fr)" : "")}};
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
}
