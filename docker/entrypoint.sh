#!/bin/bash
set -euo pipefail

# WorkspaceRoot は demo101 フォルダ(モノレポ内の実パス)。
# node_modules はこの1つ上(/app/monorepo)に hoist されており、
# vite / dotnet watch はここから起動して依存を上位に遡って解決する。
exec dotnet /opt/nijo/nijo.dll serve "/app/monorepo/demo/101_販売管理システム" \
  --demo-mode \
  --no-browser \
  --url "http://0.0.0.0:${PORT:-8080}"
