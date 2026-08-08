#!/bin/bash
set -euo pipefail

exec dotnet /opt/nijo/nijo.dll serve /app/workspace \
  --demo-mode \
  --no-browser \
  --url "http://0.0.0.0:${PORT:-8080}"
