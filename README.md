# Nijo App Scaffold

## ドキュメント

https://halllky.github.io/NijoAppScaffold/

Get Started もこちら

## デモ

準備中

## ライセンス

このプロジェクトは [LICENSE.txt](./LICENSE.txt) の条件の下で提供されています。

## 開発手順

### 環境構築

このプロジェクトは VSCode dev container, Docker を使って開発環境を構築しています。
Dockerイメージは .NET の公式イメージをベースに、追加で Node.js をインストールしたものを使用しています。
環境構築は以下の手順で実施してください。

1. VSCode を使えるようにする（公式からダウンロード）
2. VSCode で拡張機能 [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) をインストールする
3. Linux仮想環境を使えるようにする
   * Windows であれば WSL2 を有効化するなど
   * MacOS であれば colima をインストールするなど
4. Linux仮想環境上で Git を使えるようにする
5. Linux仮想環境上で Docker を使えるようにする
6. Linux仮想環境上でこのGitHubリポジトリをクローンする
7. VSCode で [ワークスペースファイル](./nijo.code-workspace) を開く
8. コマンドパレットを開き（Cmd + Shift + P）、"Dev Containers: Reopen in Container" を選択。初回はコンテナイメージのビルドが行われる（数分かかる場合あり）。完了すると、コンテナ内で開発環境が利用可能になる

### デバッグ、リリース

* 基本的にデモプロジェクトを使って動作確認を行なっています。
  デモプロジェクトのコード自動整形かけ直しやデバッグなどは VSCode の Run Task（ [ワークスペースファイル](./nijo.code-workspace)  の `tasks` セクションで定義された各タスク）から行なっています。
* リリースも Run Task から行ないます。

## デモについて（Nijo保守向け説明）

### 構成

全ユーザーが同じ環境を共有する、単一Fly Machine構成の共有デモサイトです。
ユーザーに公開されるのは [デモ101(`demo/101_販売管理システム`)](./demo/101_販売管理システム/) のみで、
GUI（フロント: `Nijo.GuiClient`、バックエンド: `Nijo/WebService`）からスキーマ編集・AIチャットでの編集・デモ101のデバッグ起動を行います。

```
Browser ──HTTPS──> Fly edge ──> nijo serve --demo-mode (:8080)  ※Machineは1台固定
                                 ├ /              → 埋め込みSPA(スキーマエディタ)
                                 ├ /api/*         → 既存API + 共有デモ用API
                                 ├ /api/demo/hub  → SignalR
                                 ├ /demo/{**}     → YARP → デモ101 WebApi (:5290, prefix除去、SPAも配信)
                                 └ /demo-api/{**} → YARP → デモ101 WebApi (:5290, prefix除去)
                                 子プロセス: publish済みデモ101 WebApi(単一プロセス) / claude -p
```

デモ101はpublish済みの単一プロセス(WebApiがAPIとSPAの両方を静的配信)として常駐実行します
（実装: [`Demo101ProcessManager.cs`](./Nijo/WebService/DemoMode/Demo101ProcessManager.cs)）。

- **排他ロック**: AIによる編集や保存・生成・リセットなど環境を変更する操作は同時に1つしか実行できません。
  実行中は他ユーザーの同種の操作が HTTP 423 で拒否されます（単一Machine・単一プロセス前提のメモリ内ロック）。
- **SignalR**: サーバーから全クライアントへ、ロック状態の変化・他ユーザーによる更新時の強制リロード・
  AIチャットやビルドプロセスの出力をリアルタイムに配信します。
- **AIチャット**: GUIのチャットパネルから入力した指示を、WebServiceが headless の `claude` CLI (`claude -p`)
  に渡して実行し、出力をストリーミング表示します。スキーマ(`nijo.xml`)が変更されていれば自動でコード生成し、
  他クライアントへ強制リロードを配信します。
- **デモ101の公開**: WebService内蔵のYARPリバースプロキシ経由で `/demo/*`・`/demo-api/*`
  (いずれもプレフィックス除去)を、publish済みデモ101 WebApi(単一プロセス)へ転送します。
  APIとSPA(ビルド済み静的ファイル)を同じプロセスが配信するため、クラスターは1つだけです
  （詳細は [`DemoReverseProxyConfig.cs`](./Nijo/WebService/DemoMode/DemoReverseProxyConfig.cs) 参照）。
- **成果物のビルド**: デモ101のビルド(`nijo generate` + `dotnet publish` + `npm run build`)は
  [`Task/RELEASE_BUILD.sh`](./demo/101_販売管理システム/Task/RELEASE_BUILD.sh) が行います。
  通常はpublish済み成果物が既にあれば使い回し、無い場合のみ自動でビルドしますが、
  `--rebuild` オプション指定時は起動時に必ずフルビルドしなおします
  （スキーマ変更後・環境リセット後などソースが変わった可能性がある場合用）。
- **アイドル検知リセット**: 最終操作から一定時間（既定30分、`DEMO_IDLE_RESET_MINUTES` で変更可）
  操作が無く、かつワークスペースがpristine状態から変化している場合、自動的に環境をリセットします。
  手動リセットボタンも用意されています。リセットは `git checkout -- . && git clean` で
  イメージビルド時にコミットしたpristine状態へ戻し、コード再生成・プロセス再起動を行います。

主要な実装ファイル:

* `Nijo/Program.cs` — `serve --demo-mode` フラグ
* `Nijo/WebService/NijoWebServiceBuilder.cs` — DI・SignalR・YARP・ロック付きエンドポイントの組み込み
* `Nijo/WebService/DemoMode/` — 共有デモサイト機能一式（ロック、Hub、プロセス管理、claude連携、リセット）
* `Nijo.GuiClient/package_schema-editor-v1/src/DemoMode/` — GUI側のプロバイダー・バナー・チャットパネル
* `Dockerfile` / `fly.toml` / `docker/entrypoint.sh` — デプロイ用構成

### ローカルでの動作確認

```bash
dotnet run --project Nijo -- serve "demo/101_販売管理システム" --demo-mode --rebuild --no-browser --url http://localhost:5001
```

`--rebuild` は任意（`--demo-mode` 併用時のみ有効）。指定すると起動時に既存のpublish済み成果物を
使い回さず、必ずフルビルドしなおしてから起動します。省略した場合は、成果物が既にあればそのまま
起動し、無ければ自動でビルドします。

ビルドは内部で [`RELEASE_BUILD.sh`](./demo/101_販売管理システム/Task/RELEASE_BUILD.sh) を実行します。
このスクリプトは `nijo generate` を `$NIJO_CLI_PATH` 経由で呼び出すため、ローカル実行時は事前に
`NIJO_CLI_PATH` 環境変数に `nijo` コマンド(またはそれを呼ぶラッパースクリプト)へのパスを
設定しておいてください。

上記コマンドで起動した際にアクセスできる主なURLは次の通りです。

* `http://localhost:5001/` — スキーマエディタ(GUI)
* `http://localhost:5001/demo/` — デモ101本体（起動には数十秒〜数分かかります）
* `http://localhost:5001/api/demo/status` — デモモードの状態確認用API

通常の(共有デモではない)ローカル開発では、これまで通り `--demo-mode` を付けずに `nijo serve` を使ってください。挙動は変わりません。

### Fly.io へのデプロイ

このリポジトリ直下に `Dockerfile` と `fly.toml` を用意しています。

```bash
# 初回のみ: fly.toml が既にあるので --no-deploy でアプリを登録
fly launch --no-deploy

# 必須シークレット(claude CLI が使用)
fly secrets set ANTHROPIC_API_KEY=sk-ant-xxxxx

# 任意(既定30分)
fly secrets set DEMO_IDLE_RESET_MINUTES=30

# デプロイ
fly deploy --ha=false
```

注意点:

* **`fly scale count 1` を維持してください。** 排他ロック・チャット履歴・子プロセス管理はすべて単一プロセスの
  メモリ内実装のため、Machineを複数台にすると排他制御が破綻します。
* イメージは `mcr.microsoft.com/dotnet/sdk:10.0` ベースです。AIチャットによるスキーマ変更時に
  都度 `RELEASE_BUILD.sh`(`nijo generate` + `dotnet publish` + `npm run build`)を実行するため、
  ランタイムイメージではなくSDKイメージを使用しています。
* イメージビルド時にデモ101の `RELEASE_BUILD.sh` を実行してpublish成果物(WebApi/bin/Release/publish・
  WebApi/wwwroot)を焼き込み、`git init && git commit` でpristine状態を固定しています。
  起動時・リセット時はこの状態を基準に戻すため、通常は再ビルド不要で即起動できます。
* ヘルスチェックは `GET /api/demo/status` です。`grace_period` を長め(90秒)に設定しています。
* 無認証で公開されるため、`claude` の実行はコンテナ内に留めた上で
  `--dangerously-skip-permissions` を使用しています。API利用量やアクセス制御（サイトパスワード等）が
  必要な場合は別途検討してください。
