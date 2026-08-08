#!/bin/bash

# 本番リリース用ビルド処理(Linux向け)。
# RELEASE_BUILD.bat のLinux移植版。非対話で実行できるようにしてあるため、
# 共有デモサイト(nijo serve --demo-mode)からの自動フルリビルドにも使う。
#
# オプション:
#   --skip-generate       ソースコード自動生成(nijo generate)をスキップする。
#                          呼び出し元が既に自プロセス内で自動生成を実行済みの場合に使う
#                          (共有デモサイトはnijo自身のプロセスなのでこちらを使う)。
#
# 環境変数:
#   NIJO_CLI_PATH         nijo コマンドのパス(--skip-generate 指定時は不要)
#   VITE_DEMO_BASE        (任意)clientのルーター basename。共有デモサイトからは "/demo/" を渡す
#   VITE_API_BASE_URL     (任意)clientのAPI接続先。共有デモサイトからは "/demo-api/" を渡す

SKIP_GENERATE=0
for arg in "$@"; do
    if [ "$arg" = "--skip-generate" ]; then
        SKIP_GENERATE=1
    fi
done

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR/.."
# publish出力先は固定パスにしておく(TFMが変わっても迷わないように、pubxmlの既定と別に明示指定する)。
# Demo101ProcessManager 等、実行側もこの固定パスを前提にする。
ASP_NET_CORE_PUBLISH_DIR="$PROJECT_ROOT/WebApi/bin/Release/publish"
NODE_JS_DIST_DIR="$PROJECT_ROOT/client/dist"
WWWROOT_DIR="$PROJECT_ROOT/WebApi/wwwroot"

# ------------------------------------
# コミットしていない変更があれば警告(非対話。中断はしない)

if [ -n "$(git -C "$PROJECT_ROOT" status --porcelain 2>/dev/null)" ]; then
    echo "警告: コミットしていない変更があります。ビルドを続行します。"
fi

# ------------------------------------
# Check required tools

if ! command -v dotnet &> /dev/null; then
    echo "'dotnet' がインストールされていません。公式サイトからインストールしてください。"
    exit 1
fi

if ! command -v npm &> /dev/null; then
    echo "'npm' がインストールされていません。公式サイトからインストールしてください。"
    exit 1
fi

if [ "$SKIP_GENERATE" -eq 0 ] && ! command -v "$NIJO_CLI_PATH" &> /dev/null; then
    echo "'nijo' がインストールされていないかパスが通っていません。公式サイトからインストールしてください。"
    exit 1
fi

# ------------------------------------
# ソースコード自動生成処理のかけなおし

if [ "$SKIP_GENERATE" -eq 1 ]; then
    echo "--skip-generate が指定されたため、ソースコード自動生成をスキップします。"
else
    pushd "$PROJECT_ROOT" > /dev/null
    "$NIJO_CLI_PATH" generate
    if [ $? -ne 0 ]; then
        echo "ソースコード自動生成処理でエラーが発生しました。ビルドを中断します。"
        popd > /dev/null
        exit 1
    fi
    popd > /dev/null
fi

# ------------------------------------
# ASP.NET Core のビルド

pushd "$PROJECT_ROOT/WebApi" > /dev/null
# publish出力先の掃除は publish プロファイル側の DeleteExistingFiles=true (本番用ビルド.pubxml)が
# 行うため、ここで事前に rm -rf する必要はない(むしろ消さないほうが不変ファイルの再コピーを
# 省略でき速い)。
PUBLISH_ARGS=(/p:PublishProfile=本番用ビルド --output "$ASP_NET_CORE_PUBLISH_DIR")
if [ "$SKIP_GENERATE" -eq 1 ]; then
    # --skip-generate 指定時(=共有デモサイトからの自動リビルド)は、依存パッケージの追加が
    # AIチャットのツール権限で禁止されている(ClaudeAgentService.cs参照)ため、
    # 直前のビルドで復元済みのパッケージ構成が変わっていない前提が常に成り立つ。
    # そのため復元をスキップして高速化する。
    PUBLISH_ARGS+=(--no-restore)
fi
dotnet publish "${PUBLISH_ARGS[@]}"
if [ $? -ne 0 ]; then
    echo "ASP.NET Core のビルドでエラーが発生しました。ビルドを中断します。"
    popd > /dev/null
    exit 1
fi
popd > /dev/null

# 実行時設定ファイルは環境構築時にリリース先環境に用意してあるものを使用するためモジュールに含めない
rm -f "$ASP_NET_CORE_PUBLISH_DIR/appsettings.json" "$ASP_NET_CORE_PUBLISH_DIR/appsettings.Development.json"

# ------------------------------------
# Node.js のビルド
# tsc(型チェック)とvite build(バンドル)は出力として独立だが、あえて並列実行はしていない。
# vite内部のesbuild/rollupは既にワーカースレッドで並列化されており、CPUに余裕のない環境
# (本番はfly.ioのshared-cpu-2x)ではtscを同時に走らせるとCPUを奪い合ってかえって遅くなりうるため。

pushd "$PROJECT_ROOT/client" > /dev/null
npm run build
if [ $? -ne 0 ]; then
    echo "Node.js のビルドでエラーが発生しました。ビルドを中断します。"
    popd > /dev/null
    exit 1
fi
popd > /dev/null

# ------------------------------------
# SPAをWebApiのwwwrootへ配置する。
# (実行時、demo101はWebApiプロセス単体でSPAとAPIの両方を配信する。
#  contentRootをWebApiディレクトリにして起動する前提)

rm -rf "$WWWROOT_DIR"
mkdir -p "$WWWROOT_DIR"
cp -r "$NODE_JS_DIST_DIR"/* "$WWWROOT_DIR"/

echo "リリースビルドが完了しました。"
echo "publish出力: $ASP_NET_CORE_PUBLISH_DIR"
echo "wwwroot: $WWWROOT_DIR"

exit 0
