using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.Document;

/// <summary>
/// 自動生成されたコードを説明するドキュメント。
/// </summary>
public class MarkdownDocument : IMultiAggregateSourceFile {

    public MarkdownDocument AddToIndexReadme(RootAggregate rootAggregate) {
        _rootAggregates.Add(rootAggregate);
        return this;
    }
    private readonly List<RootAggregate> _rootAggregates = new();

    void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
        // 特になし
    }

    void IMultiAggregateSourceFile.Render(CodeRenderingContext ctx) {
        ctx.DocumentDirectory(dir => {
            // README.md
            dir.Generate(RenderIndexReadme(ctx));

            // 集約ごとの説明ファイル
            foreach (var rootAggregate in _rootAggregates) {
                dir.Generate(new SourceFile {
                    FileName = ToFileName(rootAggregate),
                    Contents = rootAggregate.Model.GenerateDocumentMarkdown(rootAggregate),
                });
            }
        });
    }

    /// <summary>
    /// 大元のREADME.mdをレンダリングします。
    /// </summary>
    private SourceFile RenderIndexReadme(CodeRenderingContext ctx) {
        return new SourceFile {
            FileName = "README.md",
            Contents = $$"""
                # {{ctx.Config.ApplicationName}} の自動生成されたモジュール
                NijoApplicationBuilderにより生成された主要なモジュールの概要を説明します。

                {{ctx.Config.ApplicationName}}では以下の集約が定義されています。
                それぞれがどういった責務を持ち、どういったソースコードが生成されたかは各ファイルを参照してください。

                {{_rootAggregates.SelectTextTemplate(root => $$"""
                - {{ToLink(root)}}
                """)}}
                """,
        };

        static string ToLink(RootAggregate rootAggregate) {
            // リンクテキスト部分のエスケープ（[]の中の部分）
            string escapedLinkText = rootAggregate.DisplayName
                .Replace("[", "\\[")
                .Replace("]", "\\]");

            // URL部分のエスケープ（()の中の部分）
            string escapedUrl = Uri.EscapeDataString(ToFileName(rootAggregate));

            return $"[{escapedLinkText}](./{escapedUrl})";
        }
    }

    private static string ToFileName(RootAggregate rootAggregate) {
        return $"{rootAggregate.DisplayName.ToFileNameSafe()}.md";
    }
}
