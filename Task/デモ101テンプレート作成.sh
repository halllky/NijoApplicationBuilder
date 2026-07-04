#!/bin/bash
set -e

# unzip はロケールがUTF-8でないと、日本語などマルチバイト文字を含むファイル名を正しく復元できない。
# （zipアーカイブ自体にはUTF-8のファイル名が正しく格納されているが、非UTF-8ロケールの環境では
# unzipがそれを別の文字コードとして解釈してしまい、ファイル名が文字化けする。）
if locale -a 2>/dev/null | grep -qi "^en_US\.utf-\?8$"; then
  export LANG=en_US.UTF-8
elif locale -a 2>/dev/null | grep -qi "^C\.utf-\?8$"; then
  export LANG=C.UTF-8
fi

# デモ101（/demo/101_販売管理システム）から、GUI・ログ出力・ログイン認証など
# 基本的な機能があらかじめそろった新規プロジェクトテンプレート（demo101-template.zip）を作成する。
#
# 処理内容:
#   1. git archive でデモ101のソース一式をzip化する
#   2. 解凍する
#   3. 販売管理業務固有の部分（nijo.xml の定義・Core/WebApi/client のソース）を削除する
#      （削除対象は Demo101TemplateBuilder 内にハードコードされている）
#   4. 削除後のフォルダをzip化する
#
# 出力される demo101-template.zip のリソース名は、
# Nijo/GeneratedProject.cs の Templates ディクショナリで定義されているものと対応している。

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
NIJO_ROOT="$SCRIPT_DIR/.."
DEMO_DIR="demo/101_販売管理システム"

OUT_DIR="$NIJO_ROOT/temp_release"
ARCHIVE_ZIP="$OUT_DIR/demo101-template-src.zip"
WORK_DIR="$OUT_DIR/demo101-template-work"
FINAL_ZIP="$OUT_DIR/demo101-template.zip"

echo "出力先を初期化します。"
mkdir -p "$OUT_DIR"
rm -f "$ARCHIVE_ZIP" "$FINAL_ZIP"
rm -rf "$WORK_DIR"

echo "デモ101のソース一式を圧縮します: $DEMO_DIR"
git -C "$NIJO_ROOT" archive HEAD:"$DEMO_DIR" --format=zip -o "$ARCHIVE_ZIP"
if [ ! -f "$ARCHIVE_ZIP" ]; then
  echo "デモ101の圧縮に失敗しました。"
  exit 1
fi

echo "解凍します: $WORK_DIR"
mkdir -p "$WORK_DIR"
unzip -q "$ARCHIVE_ZIP" -d "$WORK_DIR"

echo "販売管理業務固有の部分を削除します。"
dotnet run --project "$SCRIPT_DIR/Demo101TemplateBuilder/Demo101TemplateBuilder.csproj" -- "$WORK_DIR"
if [ $? -ne 0 ]; then
  echo "販売管理業務固有の部分の削除に失敗しました。"
  exit 1
fi

echo "テンプレートを圧縮します: $FINAL_ZIP"
(cd "$WORK_DIR" && zip -r -q "$FINAL_ZIP" .)
if [ ! -f "$FINAL_ZIP" ]; then
  echo "テンプレートの圧縮に失敗しました。"
  exit 1
fi

echo "後片付けをします。"
rm -f "$ARCHIVE_ZIP"
rm -rf "$WORK_DIR"

echo "demo101-template.zip を作成しました: $FINAL_ZIP"

exit 0
