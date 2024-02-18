# HalApplicationBuilder 【開発中 under development】
論理データモデルから一般的なWebアプリケーションのひな形を自動生成するツール。

## 概要図 Overview
![概要図 overview](./README_files/README.drawio.svg)

### 1対多の明細データや多様な型の子要素など、複雑なデータ構造を実現可能。

### スクラッチ開発に近い拡張性。C#やSQLやHTMLを直接編集できる開発者向き。

---

## :cherry_blossom: Get Started
以下を使えるようにしておく

- dotnet
  - 公式サイトからダウンロードしてください。
- dotnet ef
  - `dotnet tool install --global dotnet-ef` でダウンロードしてください。
- npm
  - 公式サイトからダウンロードしてください。

このリポジトリをcloneし、ビルドして `nijo.exe` を作成する(開発中のためバイナリは提供できていません。ご了承ください)。
以降の手順は、このコマンドラインツールを使用して進めていきます。

新しいアプリケーションを作成する

```bash
nijo create YourApplicationName
```

ビルド
- `build.bat` を実行する

デバッグ
- Reactテンプレートプロジェクトのデバッグ
  - `Nijo.ApplicationTemplates` フォルダ内にあるReactテンプレートのルートで `npm run dev`
- 自動生成されたプロジェクトのデバッグ
  - `Nijo.IntegrationTest` プロジェクトのいずれかのデータパターンで自動生成
  - `debug.bat` を実行する
  - 以下の情報が表示されるはずなので、控えておく
    - CLIENT: ブラウザからの閲覧用。 `npm start` が実行されているURL
    - SERVER: WebAPIのURL。 `dotnet run` が実行されているURL
  - ブラウザを開いて `CLIENT` のURLにアクセスする
  - 画面のメインメニューから「設定」を開き、サーバーURL欄に上記で控えた `SERVER` のURLを入力して「更新」

---
## :cherry_blossom: Documentation
### `nijo.xml` の記述ルール
執筆中
