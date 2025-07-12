---
# https://vitepress.dev/reference/default-theme-home-page
layout: home

hero:
  name: "Nijo Application Builder"
  text: "スキーマ駆動型アプリケーション生成フレームワーク"
  tagline: XMLベースのスキーマ定義からアプリケーションコードを自動生成し、高品質なフルスクラッチ開発を支援します
  actions:
    - theme: brand
      text: チュートリアル
      link: /tutorials/
    - theme: alt
      text: ハウツーガイド
      link: /how-to-guides/
    - theme: alt
      text: リファレンス
      link: /reference/
    - theme: alt
      text: 設計思想
      link: /explanation/

features:
  - title: 📚 Tutorials
    details: 段階的な学習で基本から応用まで。初心者向けの5分で作る住所録アプリから、複雑なビジネスロジックまで。
    link: /tutorials/
  - title: 🛠️ How-to Guides
    details: 特定のタスクを達成する実践的な方法。スキーマ定義、カスタムバリデーション、デプロイメントなど。
    link: /how-to-guides/
  - title: 📖 Reference
    details: 技術仕様と詳細情報。API、データ型、コマンドライン、プロジェクト構造の完全なリファレンス。
    link: /reference/
  - title: 💡 Explanation
    details: 概念と設計思想を理解する。なぜNijoが必要なのか、アーキテクチャの設計思想を深く理解。
    link: /explanation/
---

## 🚀 Getting Started

どこから始めればよいかわからない場合は：

1. **初心者**: [📚 チュートリアル](/tutorials/)から「はじめての方へ」を実行
2. **既存プロジェクト改善**: [🛠️ ハウツーガイド](/how-to-guides/)で具体的な課題を解決
3. **技術詳細**: [📖 リファレンス](/reference/)で仕様を確認
4. **設計思想理解**: [💡 説明](/explanation/)でフレームワークの背景を学習

## 概要

NijoApplicationBuilderは、XMLベースのスキーマ定義からアプリケーションコードの一部を自動生成するフレームワークです。主として、特定の業務フローをサポートするためのアプリケーションのフルスクラッチ開発を補助することを主眼としています。

### 主な機能

- **スキーマ駆動開発**: 単一のXMLスキーマから複数の技術スタックに対応したコードを自動生成
- **完全なアプリケーションテンプレート**: React.js + ASP.NET Core + SQLite の動作するアプリケーション
- **柔軟なカスタマイズ**: テンプレートの自由な変更とビジネスロジックの追加
- **統合された開発フロー**: スキーマ定義 → 自動生成 → カスタマイズのシンプルなサイクル

### 生成されるコンポーネント

- RDBMSテーブル定義
- C#エンティティクラス
- TypeScript型定義
- CRUD操作API
- React UIコンポーネント
- バリデーション処理
- 検索・フィルタリング機能

## 開発手順概要

1. **初期設定**: `nijo.exe new` でプロジェクト作成
2. **スキーマ定義**: `nijo.xml` でデータ構造を定義
3. **コード生成**: `nijo.exe generate` で自動生成実行
4. **カスタマイズ**: 生成されたコードを基に機能拡張

