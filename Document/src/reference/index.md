# 📖 Reference（リファレンス）

*情報指向 - 技術仕様と詳細情報*

このページでは、GUIツールの画面仕様、API、コマンド、データ型などの詳細情報を提供します。開発中に必要な情報を素早く見つけるためのリファレンスとして活用してください。

[GUIツール画面仕様](./GUIツール画面仕様.md)

---

## 没

## プロジェクト構造

### プロジェクト構造の詳細
生成されるファイルとフォルダの完全な構造

### コード生成エンジンの構造
Nijoコード生成エンジンの内部構造

### アプリケーションテンプレートの構造
標準テンプレートの構成と役割

## コマンドライン API

### nijo.exe new
新しいプロジェクトの作成

**構文:**
```cmd
nijo.exe new [オプション]
```

**オプション:**
- `--template <テンプレート名>` - 使用するテンプレート
- `--output <出力ディレクトリ>` - 出力先ディレクトリ

### nijo.exe generate
スキーマ定義からソースコードを生成

**構文:**
```cmd
nijo.exe generate [オプション]
```

**オプション:**
- `--schema <スキーマファイル>` - スキーマファイルのパス（既定値: nijo.xml）
- `--output <出力ディレクトリ>` - 出力先ディレクトリ
- `--verbose` - 詳細なログ出力

### nijo.exe validate
スキーマ定義の妥当性検証

**構文:**
```cmd
nijo.exe validate [オプション]
```

## スキーマ定義リファレンス

### XMLスキーマ仕様
nijo.xmlファイルの完全な仕様

### DataModel
データモデルの定義

```xml
<DataModel name="ModelName" displayName="表示名">
  <Member name="field1" type="SingleLineText" required="true" />
  <Member name="field2" type="Integer" />
</DataModel>
```

**属性:**
- `name` (必須): モデルの内部名
- `displayName` (必須): UI表示用の名前
- `description` (任意): 説明文

### QueryModel
クエリモデルの定義

```xml
<QueryModel name="SearchName" displayName="検索名">
  <From type="DataModel" ref="ModelName" />
  <Where>
    <Member name="field1" type="SingleLineText" />
  </Where>
  <Select>
    <Member name="field1" />
    <Member name="field2" />
  </Select>
</QueryModel>
```

### CommandModel
コマンドモデルの定義

```xml
<CommandModel name="ActionName" displayName="操作名">
  <Parameter name="param1" type="SingleLineText" required="true" />
  <Implementation>
    <!-- 実装の詳細 -->
  </Implementation>
</CommandModel>
```

## データ型

### 標準データ型
組み込みデータ型の完全なリスト

| 型名             | 説明           | C#型       | TypeScript型 | HTML入力タイプ   |
| ---------------- | -------------- | ---------- | ------------ | ---------------- |
| `SingleLineText` | 単行テキスト   | `string`   | `string`     | `text`           |
| `MultiLineText`  | 複数行テキスト | `string`   | `string`     | `textarea`       |
| `Integer`        | 整数           | `int`      | `number`     | `number`         |
| `Decimal`        | 小数点数       | `decimal`  | `number`     | `number`         |
| `Boolean`        | 真偽値         | `bool`     | `boolean`    | `checkbox`       |
| `Date`           | 日付           | `DateOnly` | `Date`       | `date`           |
| `DateTime`       | 日付時刻       | `DateTime` | `Date`       | `datetime-local` |
| `Time`           | 時刻           | `TimeOnly` | `string`     | `time`           |
| `Uuid`           | UUID           | `Guid`     | `string`     | `text`           |
| `Url`            | URL            | `string`   | `string`     | `url`            |
| `Email`          | メールアドレス | `string`   | `string`     | `email`          |

### カスタムデータ型
独自のデータ型の作成方法

### バリデーション属性
各データ型で使用可能なバリデーション属性

## API リファレンス

### 生成されるWeb API
自動生成されるRESTful APIの仕様

### CRUD操作
基本的なCRUD操作のエンドポイント

**GET** `/api/{model}`
- 一覧取得
- クエリパラメータでフィルタリング、ソート、ページング

**GET** `/api/{model}/{id}`
- 単一レコードの取得

**POST** `/api/{model}`
- 新規作成

**PUT** `/api/{model}/{id}`
- 更新

**DELETE** `/api/{model}/{id}`
- 削除

### 検索API
QueryModelから生成される検索API

### コマンドAPI
CommandModelから生成されるコマンドAPI

## 技術スタック

### サポートされている技術
対応している技術とバージョン

**バックエンド:**
- .NET 8.0以上
- ASP.NET Core
- Entity Framework Core

**フロントエンド:**
- Node.js 18以上
- React 18以上
- TypeScript 5.0以上

**データベース:**
- SQLite（デフォルト）
- PostgreSQL
- SQL Server
- MySQL
- その他Entity Framework Core対応DBMS

### 依存関係
各コンポーネントの依存関係

### 設定ファイル
appsettings.json、package.jsonなどの設定

## 生成されるコンポーネント

### C#コンポーネント
- エンティティクラス
- ApplicationService
- Repository
- DTO
- バリデーション

### TypeScriptコンポーネント
- 型定義
- APIクライアント
- Reactコンポーネント
- フォーム

### データベースコンポーネント
- テーブル定義
- インデックス
- 外部キー制約
- マイグレーション

## 拡張ポイント

### カスタマイズ可能な箇所
標準テンプレートでカスタマイズ可能な部分

### オーバーライド可能なメソッド
継承によるカスタマイズ

### 設定による制御
設定ファイルによる動作制御

## 制約事項

### 技術的制約
現在の技術的制約

### スキーマ定義の制約
スキーマ定義での制約

### パフォーマンス制約
パフォーマンス関連の制約

## 関連リンク

- [GitHub Repository](https://github.com/example/nijo)
- [Issue Tracker](https://github.com/example/nijo/issues)
- [Discussion Forum](https://github.com/example/nijo/discussions)

## 他のセクションとの関係

- **[📚 Tutorials](../tutorials/)** - 実際の使用例を学習
- **[🛠️ How-to Guides](../how-to-guides/)** - 実装方法を確認
- **[💡 Explanation](../explanation/)** - 背景理論を理解
