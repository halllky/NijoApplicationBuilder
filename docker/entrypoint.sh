#!/bin/bash
set -euo pipefail

# WorkspaceRoot は demo101 フォルダ(モノレポ内の実パス)。
# node_modules はこの1つ上(/app/monorepo)に hoist されており、
# デモ101は単一プロセス(publish済みWebApiがAPIとSPAを両方配信)で常駐する。
# スキーマ変更時に再実行される npm run build(client) もここから起動して
# 依存を上位に遡って解決する(Task/RELEASE_BUILD.sh参照)。
exec dotnet /opt/nijo/nijo.dll serve "/app/monorepo/demo/101_販売管理システム" \
  --demo-mode \
  --no-browser \
  --url "http://0.0.0.0:${PORT:-8080}"
