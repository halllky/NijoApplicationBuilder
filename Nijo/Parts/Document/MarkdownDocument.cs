using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.DataModelModules;
using Nijo.Parts.Common;
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

            // 追加ドキュメント
            dir.Generate(RenderDataModelDiagram(ctx));
            dir.Generate(RenderApiEndpoints(ctx));
            dir.Generate(RenderUiComponentCatalog(ctx));
            dir.Generate(RenderValidationRules(ctx));
            dir.Generate(RenderMigrationGuide(ctx));
            dir.Generate(RenderTestCases(ctx));
            dir.Generate(RenderPerformanceConsiderations(ctx));
            dir.Generate(RenderCustomizationPoints(ctx));
            dir.Generate(RenderTroubleshootingGuide(ctx));
            dir.Generate(RenderGlossary(ctx));
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

                ## 追加ドキュメント
                以下の追加ドキュメントも参照してください：

                - [データモデル関連図](./データモデル関連図.md)
                - [APIエンドポイント一覧](./APIエンドポイント一覧.md)
                - [UIコンポーネントカタログ](./UIコンポーネントカタログ.md)
                - [バリデーションルール一覧](./バリデーションルール一覧.md)
                - [移行ガイド](./移行ガイド.md)
                - [テストケースと期待結果](./テストケースと期待結果.md)
                - [パフォーマンス考慮事項](./パフォーマンス考慮事項.md)
                - [カスタマイズポイントガイド](./カスタマイズポイントガイド.md)
                - [トラブルシューティングガイド](./トラブルシューティングガイド.md)
                - [用語集](./用語集.md)
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

    /// <summary>
    /// データモデル関連図（ER図）を表すMarkdownをレンダリングします。
    /// </summary>
    private SourceFile RenderDataModelDiagram(CodeRenderingContext ctx) {
        var contents = new StringBuilder();
        contents.AppendLine("# データモデル関連図");
        contents.AppendLine();
        contents.AppendLine("各集約間の関連性を視覚的に示す図です。特に`ref-to`で参照している関係性が一目でわかります。");
        contents.AppendLine();
        contents.AppendLine("```mermaid");
        contents.AppendLine("erDiagram");

        // 集約ごとの定義
        foreach (var rootAggregate in _rootAggregates) {
            var efEntity = new EFCoreEntity(rootAggregate);
            contents.AppendLine($"    {rootAggregate.DisplayName} {{");

            // カラム情報を取得
            var columns = efEntity.GetColumns().ToList();
            foreach (var col in columns) {
                // 主キーは特別表記
                string keyIndicator = col.IsKey ? "PK" : "";
                contents.AppendLine($"        {col.CsType} {col.PhysicalName} {keyIndicator}");
            }

            // メタデータカラムも表示
            contents.AppendLine($"        DateTime {EFCoreEntity.CREATED_AT}");
            contents.AppendLine($"        DateTime {EFCoreEntity.UPDATED_AT}");
            contents.AppendLine($"        string {EFCoreEntity.CREATE_USER}");
            contents.AppendLine($"        string {EFCoreEntity.UPDATE_USER}");

            if (rootAggregate is RootAggregate) {
                contents.AppendLine($"        int {EFCoreEntity.VERSION}");
            }

            contents.AppendLine("    }");

            // 子エンティティも表示
            foreach (var child in rootAggregate.EnumerateDescendants()) {
                var childEntity = new EFCoreEntity(child);
                contents.AppendLine($"    {child.DisplayName} {{");

                var childColumns = childEntity.GetColumns().ToList();
                foreach (var col in childColumns) {
                    string keyIndicator = col.IsKey ? "PK" : "";
                    contents.AppendLine($"        {col.CsType} {col.PhysicalName} {keyIndicator}");
                }

                contents.AppendLine($"        DateTime {EFCoreEntity.CREATED_AT}");
                contents.AppendLine($"        DateTime {EFCoreEntity.UPDATED_AT}");
                contents.AppendLine($"        string {EFCoreEntity.CREATE_USER}");
                contents.AppendLine($"        string {EFCoreEntity.UPDATE_USER}");

                contents.AppendLine("    }");
            }
        }

        // 関連性の定義
        foreach (var rootAggregate in _rootAggregates) {
            var efEntity = new EFCoreEntity(rootAggregate);

            // ナビゲーションプロパティを取得して関連を構築
            var navigations = efEntity.GetNavigationProperties().ToList();

            foreach (var nav in navigations) {
                string cardinality;

                if (nav is EFCoreEntity.NavigationOfParentChild parentChild) {
                    if (parentChild.Principal.OtherSideIsMany) {
                        cardinality = "||--o{";
                    } else {
                        cardinality = "||--||";
                    }

                    if (parentChild.Principal.ThisSide == rootAggregate) {
                        contents.AppendLine($"    {parentChild.Principal.ThisSide.DisplayName} {cardinality} {parentChild.Relevant.ThisSide.DisplayName} : \"親子関係\"");
                    }
                } else if (nav is EFCoreEntity.NavigationOfRef refNav) {
                    if (refNav.Principal.OtherSideIsMany) {
                        cardinality = "||--o{";
                    } else {
                        cardinality = "||--o|";
                    }

                    if (refNav.Relevant.ThisSide == rootAggregate) {
                        contents.AppendLine($"    {refNav.Principal.ThisSide.DisplayName} {cardinality} {refNav.Relevant.ThisSide.DisplayName} : \"参照\"");
                    }
                }
            }

            // 子エンティティとの関連
            foreach (var child in rootAggregate.EnumerateDescendants()) {
                if (child.GetParent() == rootAggregate) {
                    string cardinality = child is ChildrenAggregate ? "||--o{" : "||--||";
                    contents.AppendLine($"    {rootAggregate.DisplayName} {cardinality} {child.DisplayName} : \"親子関係\"");
                }
            }
        }

        contents.AppendLine("```");

        return new SourceFile {
            FileName = "データモデル関連図.md",
            Contents = contents.ToString(),
        };
    }

    /// <summary>
    /// APIエンドポイント一覧を表すMarkdownをレンダリングします。
    /// </summary>
    private SourceFile RenderApiEndpoints(CodeRenderingContext ctx) {
        return new SourceFile {
            FileName = "APIエンドポイント一覧.md",
            Contents = $$"""
                # APIエンドポイント一覧 ※このドキュメントは執筆中です。

                自動生成されたAPIの一覧とその使用方法、パラメータの説明などを含むドキュメントです。

                ## REST API

                {{_rootAggregates.SelectTextTemplate(root => $$"""
                ### {{root.DisplayName}} 関連

                | エンドポイント | メソッド | 説明 |
                |--------------|---------|------|
                | `/api/{{root.DisplayName.ToLowerInvariant()}}` | GET | {{root.DisplayName}}の一覧を取得 |
                | `/api/{{root.DisplayName.ToLowerInvariant()}}/{id}` | GET | 指定IDの{{root.DisplayName}}を取得 |
                | `/api/{{root.DisplayName.ToLowerInvariant()}}` | POST | 新規{{root.DisplayName}}を作成 |
                | `/api/{{root.DisplayName.ToLowerInvariant()}}/{id}` | PUT | 指定IDの{{root.DisplayName}}を更新 |
                | `/api/{{root.DisplayName.ToLowerInvariant()}}/{id}` | DELETE | 指定IDの{{root.DisplayName}}を削除 |

                """)}}

                ## リクエスト/レスポンスの例

                以下に各APIのリクエスト/レスポンスの例を示します。実際のパラメータはアプリケーションの実装によって異なる場合があります。
                """
        };
    }

    /// <summary>
    /// UIコンポーネントカタログを表すMarkdownをレンダリングします。
    /// </summary>
    private SourceFile RenderUiComponentCatalog(CodeRenderingContext ctx) {
        return new SourceFile {
            FileName = "UIコンポーネントカタログ.md",
            Contents = $$"""
                # UIコンポーネントカタログ ※このドキュメントは執筆中です。

                自動生成されたUI要素の一覧とその使い方の例を示すドキュメントです。
                特に`CustomizeAllUi="True"`が指定されている場合は重要です。

                ## 共通コンポーネント

                ### フォームコンポーネント

                各データモデルのフォームコンポーネントは以下のような使い方ができます：

                ```tsx
                // 例: 親集約のフォーム
                import { 親集約Form } from '@/components/forms/親集約Form';

                const MyComponent = () => {
                  const handleSubmit = (data) => {
                    // 送信処理
                  };

                  return (
                    <親集約Form onSubmit={handleSubmit} />
                  );
                };
                ```

                ### テーブルコンポーネント

                データ一覧を表示するテーブルコンポーネントの使い方：

                ```tsx
                // 例: 親集約のテーブル
                import { 親集約Table } from '@/components/tables/親集約Table';

                const MyListComponent = () => {
                  const data = [/* ... */]; // データ配列

                  return (
                    <親集約Table data={data} />
                  );
                };
                ```

                ## カスタムコンポーネント

                デフォルトで生成されるコンポーネント以外に、以下のようなカスタムコンポーネントも利用できます：

                - 検索フォーム
                - 詳細表示
                - インラインエディター
                """
        };
    }

    /// <summary>
    /// バリデーションルール一覧を表すMarkdownをレンダリングします。
    /// </summary>
    private SourceFile RenderValidationRules(CodeRenderingContext ctx) {
        var contents = new StringBuilder();
        contents.AppendLine("# バリデーションルール一覧 ※このドキュメントは執筆中です。");
        contents.AppendLine();
        contents.AppendLine("各フィールドに適用されるバリデーションルールの詳細な説明です。");
        contents.AppendLine();

        foreach (var rootAggregate in _rootAggregates) {
            contents.AppendLine($"## {rootAggregate.DisplayName}");
            contents.AppendLine();
            contents.AppendLine("| フィールド | タイプ | 必須 | 検証ルール |");
            contents.AppendLine("|------------|-------|------|------------|");

            var members = rootAggregate.GetMembers().Where(m => m is ValueMember);
            foreach (var member in members) {
                if (member is ValueMember vm) {
                    string validationRules = GetValidationRulesForMember(vm);
                    contents.AppendLine($"| {vm.DisplayName} | {vm.Type.GetType().Name.Replace("Member", "")} | {(vm.IsRequired || vm.IsKey ? "はい" : "いいえ")} | {validationRules} |");
                }
            }

            contents.AppendLine();

            // 子エンティティのバリデーションルールも表示
            foreach (var child in rootAggregate.EnumerateDescendants()) {
                contents.AppendLine($"### {child.DisplayName}");
                contents.AppendLine();
                contents.AppendLine("| フィールド | タイプ | 必須 | 検証ルール |");
                contents.AppendLine("|------------|-------|------|------------|");

                var childMembers = child.GetMembers().Where(m => m is ValueMember);
                foreach (var member in childMembers) {
                    if (member is ValueMember vm) {
                        string validationRules = GetValidationRulesForMember(vm);
                        contents.AppendLine($"| {vm.DisplayName} | {vm.Type.GetType().Name.Replace("Member", "")} | {(vm.IsRequired || vm.IsKey ? "はい" : "いいえ")} | {validationRules} |");
                    }
                }

                contents.AppendLine();
            }
        }

        return new SourceFile {
            FileName = "バリデーションルール一覧.md",
            Contents = contents.ToString(),
        };

        static string GetValidationRulesForMember(ValueMember member) {
            var rules = new List<string>();

            // 基本バリデーション
            if (member.IsKey) {
                rules.Add("主キー");
            }

            // 文字列型のルール
            if (member.MaxLength != null) {
                rules.Add($"最大文字数: {member.MaxLength}文字");
            }

            if (member.CharacterType != null) {
                rules.Add($"文字種: {member.CharacterType}");
            }

            // 数値型のルール
            if (member.TotalDigit != null) {
                rules.Add($"桁数: {member.TotalDigit}桁");

                if (member.DecimalPlace != null && member.DecimalPlace > 0) {
                    rules.Add($"小数点以下: {member.DecimalPlace}桁");
                }
            }

            // 型に基づくルール
            switch (member.Type.GetType().Name) {
                case "DateTimeMember":
                    rules.Add("有効な日時形式");
                    break;
                case "DateMember":
                    rules.Add("有効な日付形式");
                    break;
                case "YearMonthMember":
                    rules.Add("有効な年月形式");
                    break;
                case "YearMember":
                    rules.Add("有効な年形式");
                    break;
                case "WordMember":
                    if (rules.Count == 0) {
                        rules.Add("文字列");
                    }
                    break;
                case "DescriptionMember":
                    if (rules.Count == 0) {
                        rules.Add("長文テキスト");
                    }
                    break;
                case "ByteArrayMember":
                    rules.Add("バイナリデータ");
                    break;
            }

            return string.Join("<br>", rules);
        }
    }

    /// <summary>
    /// 移行ガイドを表すMarkdownをレンダリングします。
    /// </summary>
    private SourceFile RenderMigrationGuide(CodeRenderingContext ctx) {
        return new SourceFile {
            FileName = "移行ガイド.md",
            Contents = $$"""
                # 移行ガイド ※このドキュメントは執筆中です。

                既存システムからのデータ移行方法や注意点を説明するドキュメントです。

                ## データ移行手順

                1. **移行前の準備**
                   - 既存データのバックアップを取得
                   - 新システムのスキーマ確認

                2. **データマッピング**
                   - 既存データと新データモデルのマッピング定義
                   - 変換ロジックの作成

                3. **テスト移行**
                   - テスト環境での移行実施
                   - データ整合性の検証

                4. **本番移行**
                   - 本番環境での移行実施
                   - 移行結果の検証

                ## 移行ツール

                データ移行を支援するツールがプロジェクトに含まれています：

                ```
                migration.bat
                ```

                このツールは以下のような機能を提供します：

                - CSVデータのインポート
                - データ変換処理
                - 整合性チェック

                ## 移行時の注意点

                - 主キーの扱いに注意してください
                - 日付データのフォーマットに注意してください
                - 外部キー参照が適切に設定されていることを確認してください
                """
        };
    }

    /// <summary>
    /// テストケースと期待結果を表すMarkdownをレンダリングします。
    /// </summary>
    private SourceFile RenderTestCases(CodeRenderingContext ctx) {
        return new SourceFile {
            FileName = "テストケースと期待結果.md",
            Contents = $$"""
                # テストケースと期待結果 ※このドキュメントは執筆中です。

                自動生成されたコードの機能テスト例とその期待結果です。

                ## 共通テストケース

                以下のテストケースは全てのエンティティに適用されます：

                ### 基本的なCRUD操作

                | テスト内容 | 入力 | 期待結果 |
                |----------|------|----------|
                | 新規作成 | 有効なデータ | 作成成功、IDが返却される |
                | 新規作成 | 無効なデータ | バリデーションエラー |
                | 読み取り | 有効なID | エンティティが返却される |
                | 読み取り | 無効なID | Not Foundエラー |
                | 更新 | 有効なデータ | 更新成功 |
                | 更新 | 無効なデータ | バリデーションエラー |
                | 削除 | 有効なID | 削除成功 |
                | 削除 | 無効なID | Not Foundエラー |

                ### 並行制御

                | テスト内容 | 入力 | 期待結果 |
                |----------|------|----------|
                | 楽観的排他制御 | 古いバージョンで更新 | 競合エラー |
                | 楽観的排他制御 | 最新バージョンで更新 | 更新成功 |

                ## エンティティ固有のテストケース

                {{_rootAggregates.SelectTextTemplate(root => $$"""
                ### {{root.DisplayName}}

                | テスト内容 | 入力 | 期待結果 |
                |----------|------|----------|
                | 固有ルール検証 | ケース1 | 期待結果1 |
                | 固有ルール検証 | ケース2 | 期待結果2 |

                """)}}
                """
        };
    }

    /// <summary>
    /// パフォーマンス考慮事項を表すMarkdownをレンダリングします。
    /// </summary>
    private SourceFile RenderPerformanceConsiderations(CodeRenderingContext ctx) {
        return new SourceFile {
            FileName = "パフォーマンス考慮事項.md",
            Contents = $$"""
                # パフォーマンスに関する考慮事項 ※このドキュメントは執筆中です。

                大量データ処理時などの最適化方法に関するガイドラインです。

                ## データベースパフォーマンス

                ### インデックス設計

                自動生成されたコードでは以下のインデックスが作成されます：

                - 主キーインデックス
                - 外部キーインデックス

                アプリケーション固有の検索パターンに基づいて、追加のインデックスを検討してください。

                ### クエリ最適化

                - 大量データを扱う場合は、ページネーションを利用してください
                - 検索条件を指定する際は、インデックスが効くようにしてください
                - 必要なカラムのみを選択するようにしてください

                ## フロントエンドパフォーマンス

                ### 仮想スクロール

                大量のデータを表示する場合は、仮想スクロールを使用することを検討してください：

                ```tsx
                import { VirtualScroll } from '@/components/common/VirtualScroll';

                const MyComponent = () => {
                  const largeDataset = [ /* 大量のデータ */ ];

                  return (
                    <VirtualScroll
                      data={largeDataset}
                      height={500}
                      rowHeight={40}
                      renderRow={(item) => <div>{item.name}</div>}
                    />
                  );
                };
                ```

                ### 遅延読み込み

                大きなコンポーネントは遅延読み込みを検討してください：

                ```tsx
                import { lazy, Suspense } from 'react';

                const HeavyComponent = lazy(() => import('./HeavyComponent'));

                const MyComponent = () => {
                  return (
                    <Suspense fallback={<div>Loading...</div>}>
                      <HeavyComponent />
                    </Suspense>
                  );
                };
                ```
                """
        };
    }

    /// <summary>
    /// カスタマイズポイントガイドを表すMarkdownをレンダリングします。
    /// </summary>
    private SourceFile RenderCustomizationPoints(CodeRenderingContext ctx) {
        return new SourceFile {
            FileName = "カスタマイズポイントガイド.md",
            Contents = $$"""
                # カスタマイズポイントガイド ※このドキュメントは執筆中です。

                自動生成されたコードをどこでどのようにカスタマイズできるかを示したドキュメントです。
                特に`OnBeforeCreate`などのフックポイントの使い方を説明します。

                ## サーバーサイドカスタマイズ

                ### アプリケーションサービスのカスタマイズ

                アプリケーションサービスには以下のようなフックポイントが用意されています：

                ```csharp
                // OverridedApplicationService.cs
                // 新規作成前に呼ばれるフック
                protected override void OnBeforeCreate(親集約Entity entity, ValidationErrorCollection errors)
                {
                    // カスタム検証ロジックを実装
                    if (entity.価格 < 0)
                    {
                        errors.Add(nameof(entity.価格), "価格は0以上である必要があります");
                    }

                    // ベースクラスの処理も呼び出す
                    base.OnBeforeCreate(entity, errors);
                }

                // 新規作成後に呼ばれるフック
                protected override async Task OnAfterCreateAsync(親集約Entity entity)
                {
                    // 他のシステムへの通知など
                    await _notificationService.NotifyAsync("新規作成", entity);

                    // ベースクラスの処理も呼び出す
                    await base.OnAfterCreateAsync(entity);
                }
                ```

                ### リポジトリのカスタマイズ

                カスタムクエリを追加する方法：

                ```csharp
                // 親集約Repository.cs
                public async Task<List<親集約Entity>> FindByCustomCriteriaAsync(string criteria)
                {
                    return await _dbContext.親集約
                        .Where(e => e.Name.Contains(criteria))
                        .ToListAsync();
                }
                ```

                ## フロントエンドカスタマイズ

                ### コンポーネントのカスタマイズ

                自動生成されたReactコンポーネントは以下のようにカスタマイズできます：

                ```tsx
                // カスタム親集約フォーム
                import { 親集約Form } from '@/components/forms/親集約Form';

                const Custom親集約Form = (props) => {
                  return (
                    <div className="custom-wrapper">
                      <h2>カスタムフォーム</h2>
                      <親集約Form {...props} />
                      <div className="additional-controls">
                        {/* カスタム要素 */}
                      </div>
                    </div>
                  );
                };
                ```

                ### カスタムフック

                データ取得ロジックのカスタマイズ：

                ```tsx
                // カスタムフック
                import { use親集約 } from '@/hooks/use親集約';

                const useCustom親集約 = (id) => {
                  const { data, loading, error, mutate } = use親集約(id);

                  // カスタム処理
                  const customUpdate = async (newData) => {
                    // 前処理
                    await mutate(newData);
                    // 後処理
                  };

                  return { data, loading, error, customUpdate };
                };
                ```
                """
        };
    }

    /// <summary>
    /// トラブルシューティングガイドを表すMarkdownをレンダリングします。
    /// </summary>
    private SourceFile RenderTroubleshootingGuide(CodeRenderingContext ctx) {
        return new SourceFile {
            FileName = "トラブルシューティングガイド.md",
            Contents = $$"""
                # トラブルシューティングガイド ※このドキュメントは執筆中です。

                一般的な問題と解決策を示したドキュメントです。

                ## 一般的な問題

                ### バリデーションエラー

                **問題**: バリデーションエラーが発生する

                **解決策**:
                - 必須項目が入力されていることを確認
                - 文字数制限を確認
                - 日付形式が正しいことを確認
                - 数値項目に数値以外が入力されていないか確認

                ### データベース接続エラー

                **問題**: データベース接続エラーが発生する

                **解決策**:
                - 接続文字列の設定を確認
                - データベースサーバーが実行中か確認
                - ファイアウォール設定を確認
                - ユーザー権限を確認

                ### 楽観的排他制御エラー

                **問題**: 楽観的排他制御エラーが発生する

                **解決策**:
                - データを再読み込みしてから更新を試みる
                - 競合解決ロジックを実装する

                ## エラーコード一覧

                | エラーコード | 説明 | 解決策 |
                |------------|-----|-------|
                | DB-001 | データベース接続エラー | 接続設定を確認 |
                | VAL-001 | バリデーションエラー | 入力データを確認 |
                | CONC-001 | 排他制御エラー | データを再読み込み |
                | AUTH-001 | 認証エラー | 認証情報を確認 |

                ## ログの見方

                アプリケーションは以下の場所にログを出力します：

                ```
                logs/application.log
                ```

                ログの形式は以下の通りです：

                ```
                [YYYY-MM-DD HH:MM:SS] [レベル] [コンポーネント] - メッセージ
                ```

                重要なエラーは `ERROR` レベルで出力されます。
                """
        };
    }

    /// <summary>
    /// 用語集を表すMarkdownをレンダリングします。
    /// </summary>
    private SourceFile RenderGlossary(CodeRenderingContext ctx) {
        return new SourceFile {
            FileName = "用語集.md",
            Contents = $$"""
                # 用語集 ※このドキュメントは執筆中です。

                プロジェクト固有の用語とその定義を説明するドキュメントです。

                ## 一般用語

                | 用語 | 定義 |
                |-----|-----|
                | 集約 | 関連するエンティティの集まり。トランザクションの単位となる |
                | 集約ルート | 集約の外部からアクセスできる唯一のエンティティ |
                | 値オブジェクト | 識別子を持たず、属性値のみで一意に識別される不変のオブジェクト |
                | エンティティ | 識別子を持ち、ライフサイクルを通じて同一性が保たれるオブジェクト |
                | DTO | Data Transfer Object。層間のデータ転送に使用されるオブジェクト |
                | CQRS | Command Query Responsibility Segregation。コマンド（変更）とクエリ（参照）の責務を分離する設計パターン |

                ## プロジェクト固有の用語

                {{_rootAggregates.SelectTextTemplate(root => $$"""
                ### {{root.DisplayName}} 関連

                | 用語 | 定義 |
                |-----|-----|
                | {{root.DisplayName}} | [ここに定義を記述] |
                """)}}

                ## 技術用語

                | 用語 | 定義 |
                |-----|-----|
                | EF Core | Entity Framework Core。.NETのORM |
                | REST API | REpresentational State Transfer API。リソース指向のWeb API |
                | ORM | Object-Relational Mapping。オブジェクトとリレーショナルデータベースの間のマッピング技術 |
                | JWT | JSON Web Token。セキュアな情報伝達のための開放標準 |
                """
        };
    }

    private static string ToFileName(RootAggregate rootAggregate) {
        return $"{rootAggregate.DisplayName.ToFileNameSafe()}.md";
    }
}
