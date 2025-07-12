# 5分で作る住所録アプリ

このチュートリアルでは、NijoApplicationBuilderを使って5分で動作する住所録アプリケーションを作成します。

## 前提条件

- Windows 10以上
- .NET 8.0以上
- Node.js 18以上

## ステップ1：新しいプロジェクトの作成

新しいフォルダを作成し、そこで新しいNijoプロジェクトを作成します。

```cmd
# 新しいフォルダを作成
mkdir my-address-book
cd my-address-book

# Nijoプロジェクトを作成
nijo.exe new
```

このコマンドにより、以下のファイルとフォルダが作成されます：

- `nijo.xml` - スキーマ定義ファイル
- `Nijo.ApplicationTemplate.Ver1/` - アプリケーションテンプレート
- その他の設定ファイル

## ステップ2：スキーマ定義の作成

`nijo.xml` ファイルを開き、以下の内容に書き換えます：

```xml
<?xml version="1.0" encoding="utf-8"?>
<nijo version="1.0">

  <!-- 住所録のデータ構造を定義 -->
  <DataModel name="Contact" displayName="連絡先">
    <Member name="Name" displayName="氏名" type="SingleLineText" required="true" />
    <Member name="Email" displayName="メールアドレス" type="SingleLineText" />
    <Member name="Phone" displayName="電話番号" type="SingleLineText" />
    <Member name="Address" displayName="住所" type="MultiLineText" />
    <Member name="Birthday" displayName="誕生日" type="Date" />
    <Member name="Notes" displayName="メモ" type="MultiLineText" />
  </DataModel>

</nijo>
```

### スキーマ定義の説明

- `DataModel`: データベースのテーブルに相当する単位
- `name`: プログラム内部で使用される名前
- `displayName`: 画面上に表示される名前
- `Member`: テーブルの列に相当する単位
- `type`: データの種類（`SingleLineText`は単行テキスト、`MultiLineText`は複数行テキスト）
- `required`: 必須入力かどうか

## ステップ3：コード生成の実行

以下のコマンドを実行してコードを生成します：

```cmd
nijo.exe generate
```

成功すると、以下のようなメッセージが表示されます：

```
スキーマ定義を解析しました。
コード生成を開始します...
コード生成が完了しました。
```

## ステップ4：アプリケーションの実行

### バックエンドの起動

```cmd
cd Nijo.ApplicationTemplate.Ver1
dotnet run --project WebApi
```

### フロントエンドの起動

別のターミナルを開いて：

```cmd
cd Nijo.ApplicationTemplate.Ver1/react
npm start
```

## ステップ5：アプリケーションの確認

ブラウザで `http://localhost:3000` にアクセスすると、住所録アプリケーションが表示されます。

### 基本的な操作

1. **連絡先の追加**
   - 「新規作成」ボタンをクリック
   - 氏名（必須）、メールアドレス、電話番号などを入力
   - 「保存」ボタンをクリック

2. **連絡先の一覧表示**
   - 作成した連絡先が一覧で表示される
   - 検索機能で特定の連絡先を絞り込み可能

3. **連絡先の編集**
   - 一覧から編集したい連絡先をクリック
   - 情報を修正して「保存」ボタンをクリック

4. **連絡先の削除**
   - 編集画面から「削除」ボタンをクリック

## 生成されたコードの構造

コード生成により、以下のコンポーネントが自動的に作成されています：

### データベース関連
- SQLiteデータベース
- `Contact` テーブル
- Entity Framework Coreのモデル

### バックエンド（C#）
- `Contact` エンティティクラス
- CRUD操作のAPI
- バリデーション処理
- 検索・フィルタリング機能

### フロントエンド（React + TypeScript）
- `Contact` 型定義
- 一覧表示コンポーネント
- 新規作成・編集フォーム
- 検索機能

## 学習のポイント

### 1. 単一真実の源
`nijo.xml` に定義した構造が、データベース、バックエンド、フロントエンドすべてに反映されています。

### 2. 自動生成される機能
- 必須入力チェック
- ページング
- ソート機能
- 基本的な検索機能

### 3. カスタマイズ可能
生成されたコードを基に、独自のビジネスロジックを追加できます。

## 次のステップ

住所録アプリケーションが動作することを確認できたら、以下のチュートリアルに進みましょう：

- [基本的なデータ型を理解する](./basic-data-types.md)
- [リレーションシップを使いこなす](./relationships.md)

## トラブルシューティング

### よくある問題と解決方法

**問題:** コード生成でエラーが発生する
**解決方法:** `nijo.xml` の文法を確認。XMLの構文エラーがないか確認してください。

**問題:** フロントエンドが起動しない
**解決方法:** Node.jsのバージョンを確認。18以上が必要です。

**問題:** バックエンドが起動しない
**解決方法:** .NET SDKのバージョンを確認。8.0以上が必要です。

詳細なトラブルシューティングは[リファレンス](../reference/troubleshooting.md)を参照してください。
