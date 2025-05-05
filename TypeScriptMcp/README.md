# TypeScript MCP

TypeScript版 MCP (Model Context Protocol) ツールは、AIエージェントがTypeScriptのプロジェクトに関するタスクの遂行の精度を上げるためのものです。

このツールは、.NET版MCPをTypeScriptに移植したものです。

## 機能

- `find_definition`: TypeScriptファイル内の特定位置にあるシンボルの定義を検索します
- `find_references`: TypeScriptファイル内の特定位置にあるシンボルの参照を検索します
- `suggest_abstract_members`: TypeScriptファイル内のクラスが実装すべきインターフェースメンバーを提案します

## セットアップ

1. 必要なパッケージをインストールします:

```bash
npm install
```

2. `appsettings.json`ファイルを設定します:

```json
{
  "TypeScriptMcp": {
    "ProjectPath": "<TypeScriptプロジェクトの絶対パス>",
    "WorkDirectory": "work"
  }
}
```

## 使い方

### 開発モードで実行

```bash
npm run dev
```

### ビルドして実行

```bash
npm run build
npm start
```

## APIの使用例

```bash
# シンボルの定義を検索
curl -X POST http://localhost:3000/api/mcp -H "Content-Type: application/json" -d '{"tool":"find_definition","params":{"sourceFilePath":"C:\\path\\to\\file.ts","line":"10","character":"15"}}'

# シンボルの参照を検索
curl -X POST http://localhost:3000/api/mcp -H "Content-Type: application/json" -d '{"tool":"find_references","params":{"sourceFilePath":"C:\\path\\to\\file.ts","line":"10","character":"15"}}'

# インターフェースメンバーを提案
curl -X POST http://localhost:3000/api/mcp -H "Content-Type: application/json" -d '{"tool":"suggest_abstract_members","params":{"sourceFilePath":"C:\\path\\to\\file.ts","line":"10","character":"15"}}'
```
