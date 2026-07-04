#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
NIJO_ROOT="$SCRIPT_DIR/../"
APP_TEMPLATE_DIR="$NIJO_ROOT/Nijo.NewProjectTemplate"
APP_TEMPLATE_ZIP="$NIJO_ROOT/temp_release/empty-template.zip"
DEMO101_TEMPLATE_ZIP="$NIJO_ROOT/temp_release/demo101-template.zip"

# GitHub配置用のzipまで作成するかどうかをコマンドライン引数から受け取る
ARCHIVE_RELEASE_ZIP="$1"

if [ "$ARCHIVE_RELEASE_ZIP" == "0" ]; then
  goto_EXISTS_VERSION_TAG=true
else
  goto_EXISTS_VERSION_TAG=false
fi

if [ "$goto_EXISTS_VERSION_TAG" == "false" ]; then
  # gitでコミットされていない変更が1個以上ある場合は確認
  if [ -n "$(git status --porcelain)" ]; then
    read -p "コミットされていない変更があります。リリースを続行しますか？ (y/n): " yn
    case $yn in
      [Yy]* ) ;;
      * ) echo "リリースを中断します。"; exit 1;;
    esac
  fi

  # リリースするバージョンの番号を入力する
  read -p "Version?: " RELEASE_VERSION
  if [ -z "$RELEASE_VERSION" ]; then
    read -p "バージョンが指定されていません。リリースを続行しますか？ (y/n): " yn
    case $yn in
      [Yy]* ) ;;
      * ) echo "リリースを中断します。"; exit 1;;
    esac
  fi

  # gitで現在のリビジョンに指定されているタグの一覧を取得し確認
  CURRENT_TAGS=$(git describe --tags --exact-match 2>/dev/null)
  if [[ ! "$CURRENT_TAGS" =~ "ver-$RELEASE_VERSION" ]]; then
    read -p "現在のリビジョンに ver-$RELEASE_VERSION タグが打たれていません。 リリースを続行しますか？ (y/n): " yn
    case $yn in
      [Yy]* ) ;;
      * ) echo "リリースを中断します。"; exit 1;;
    esac
  fi
fi

# --- デプロイ前のテスト ---
# GitHub配置用リリース（＝デプロイ）のときのみ、node と dotnet のテストを実行し、
# すべて成功した場合のみデプロイを続行する。失敗した場合はここで中断する。
if [ "$ARCHIVE_RELEASE_ZIP" != "0" ]; then
  echo "デプロイ前のテストを実行します（Node.js / .NET）。"

  echo "[1/2] Node.js (vitest) のテストを実行します。"
  pushd "$NIJO_ROOT" > /dev/null
  npm run test:ci
  NODE_TEST_RESULT=$?
  popd > /dev/null
  if [ $NODE_TEST_RESULT -ne 0 ]; then
    echo "Node.js のテストに失敗したため、デプロイを中断します。"
    exit 1
  fi

  echo "[2/2] .NET (Nijo.IntegrationTest) のテストを実行します。"
  dotnet test "$NIJO_ROOT/Nijo.IntegrationTest/Nijo.IntegrationTest.csproj"
  if [ $? -ne 0 ]; then
    echo ".NET のテストに失敗したため、デプロイを中断します。"
    exit 1
  fi

  echo "すべてのテストが成功しました。デプロイを続行します。"
fi

# EXISTS_VERSION_TAG
echo "アプリケーションテンプレートを圧縮します: $APP_TEMPLATE_ZIP"
mkdir -p "$NIJO_ROOT/temp_release"
rm -f "$APP_TEMPLATE_ZIP"

# git archive
git archive HEAD:Nijo.NewProjectTemplate --format=zip -o "$APP_TEMPLATE_ZIP"
if [ ! -f "$APP_TEMPLATE_ZIP" ]; then
  echo "アプリケーションテンプレートの圧縮に失敗しました。"
  exit 1
fi

echo "デモ101テンプレートを圧縮します: $DEMO101_TEMPLATE_ZIP"
bash "$SCRIPT_DIR/デモ101テンプレート作成.sh"
if [ ! -f "$DEMO101_TEMPLATE_ZIP" ]; then
  echo "デモ101テンプレートの圧縮に失敗しました。"
  exit 1
fi

echo "フロントエンドのビルドを開始します。"
pushd "$NIJO_ROOT/Nijo.GuiClient/package_schema-editor-v1" > /dev/null
npm run build
if [ $? -ne 0 ]; then
  echo "ビルドに失敗しました。"
  popd > /dev/null
  exit 1
fi
popd > /dev/null

# デモ編集用（内部向け）の場合は、現在のOSで動くフレームワーク依存ビルドを
# bin/Release/net10.0/publish フォルダに1箇所だけ発行して終了する。
# 「Nijo > デモ > スキーマ定義編集」タスクがこの publish フォルダの nijo を実行する。
if [ "$ARCHIVE_RELEASE_ZIP" == "0" ]; then
  echo "デモ編集用のビルドを開始します。"
  dotnet publish "$NIJO_ROOT/Nijo/Nijo.csproj" -c Release -o "$NIJO_ROOT/Nijo/bin/Release/net10.0/publish"
  if [ $? -ne 0 ]; then
    echo "ビルドに失敗しました。"
    exit 1
  fi
  exit 0
fi

# GitHub配置用：win/osx をそれぞれRID別に発行する
echo "ビルドを開始します。"
dotnet publish "$NIJO_ROOT/Nijo/Nijo.csproj" -p:PublishProfile=FOR_GITHUB_RELEASE_WINDOWS
if [ $? -ne 0 ]; then
  echo "ビルドに失敗しました。"
  exit 1
fi

dotnet publish "$NIJO_ROOT/Nijo/Nijo.csproj" -p:PublishProfile=FOR_GITHUB_RELEASE_OSX
if [ $? -ne 0 ]; then
  echo "ビルドに失敗しました。"
  exit 1
fi

# 圧縮
# zipコマンドを使用
pushd "$NIJO_ROOT/Nijo/bin/Release/net10.0/publish-win" > /dev/null
zip -r "$NIJO_ROOT/temp_release/release-$RELEASE_VERSION-win.zip" .
popd > /dev/null

pushd "$NIJO_ROOT/Nijo/bin/Release/net10.0/publish-osx" > /dev/null
zip -r "$NIJO_ROOT/temp_release/release-$RELEASE_VERSION-osx.zip" .
popd > /dev/null

# 掃除
rm -f "$APP_TEMPLATE_ZIP" "$DEMO101_TEMPLATE_ZIP"

echo "リリース $RELEASE_VERSION を作成しました。"
echo "GitHubのReleaseページにアップロードしてください。"

if command -v xdg-open > /dev/null 2>&1; then
  xdg-open "$NIJO_ROOT/temp_release" > /dev/null 2>&1 || true
elif command -v open > /dev/null 2>&1; then
  open "$NIJO_ROOT/temp_release" > /dev/null 2>&1 || true
else
  echo "成果物: $NIJO_ROOT/temp_release"
fi

exit 0
