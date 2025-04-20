# McpサーバーforNijo

このプロジェクトは、Nijo.ApplicationTemplate.Ver1のデバッグ実行とリビルドを行うためのサーバーです。

## 機能

- **start**: Reactアプリケーション（npm run dev）とWebAPIアプリケーション（dotnet run）を起動します
- **rebuild**: WebAPIアプリケーションを再ビルドして再起動します
- **stop**: 起動中のすべてのプロセスを停止します

## 使用方法

### サーバーの起動

```bash
cd McpServer
dotnet run
```

### APIエンドポイント

- GET /api/process/status - 現在のプロセス状態を取得
- POST /api/process/start - アプリケーションを起動
- POST /api/process/rebuild - WebAPIを再ビルド
- POST /api/process/stop - すべてのプロセスを停止

## curlでの操作例

```bash
# ステータス確認
curl http://localhost:5001/api/process/status

# 起動
curl -X POST http://localhost:5001/api/process/start

# 再ビルド
curl -X POST http://localhost:5001/api/process/rebuild

# 停止
curl -X POST http://localhost:5001/api/process/stop
```

## 注意事項

- Reactアプリケーションはホットリロード対応のため、rebuildコマンドではWebAPIのみが再起動されます
- プロセスを強制終了する際に、すべての子プロセスも含めて終了します
