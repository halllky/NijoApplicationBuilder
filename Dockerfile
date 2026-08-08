# 共有デモサイト用イメージ。
# Nijo.GuiClient (スキーマエディタSPA) と Nijo (nijo serve --demo-mode) をビルドし、
# デモ101(demo/101_販売管理システム)を実行環境として焼き込む。
#
# ビルド: docker build -t nijo-demo .
# 実行:   docker run -p 8080:8080 -e ANTHROPIC_API_KEY=... nijo-demo

# ---------------------------------------------------------------------------
# stage1: GuiClient(スキーマエディタSPA)を単一HTMLファイルにビルド
# ---------------------------------------------------------------------------
FROM node:22 AS gui-build
WORKDIR /src
COPY package.json package-lock.json ./
COPY Nijo.GuiClient ./Nijo.GuiClient
RUN npm ci && npm run build:schema-editor

# ---------------------------------------------------------------------------
# stage2: nijo (CLI + WebService) を publish
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS nijo-build
WORKDIR /src
COPY Nijo ./Nijo
COPY --from=gui-build /src/Nijo.GuiClient/package_schema-editor-v1/dist ./Nijo.GuiClient/package_schema-editor-v1/dist
RUN dotnet publish Nijo/Nijo.csproj -c Release -o /out

# ---------------------------------------------------------------------------
# stage3: 実行環境。dotnet watch(demo101 WebApi)とvite(demo101 client)を
# 常駐実行するため、ランタイムではなくSDKイメージを使う。
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends git curl procps ca-certificates \
    && curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && npm install -g @anthropic-ai/claude-code \
    && rm -rf /var/lib/apt/lists/*

RUN useradd --create-home --shell /bin/bash demo
COPY --from=nijo-build --chown=demo:demo /out /opt/nijo
COPY --chown=demo:demo "demo/101_販売管理システム" /app/workspace

WORKDIR /app/workspace

# ビルドキャッシュ(NuGet/npm)をイメージ層に焼き込み、
# 起動時・リセット後の再ビルド時間を短縮する。
RUN cd client && npm ci \
    && cd ../WebApi && dotnet build \
    && git config --global user.email "demo@example.com" \
    && git config --global user.name "demo" \
    && git config --global --add safe.directory /app/workspace \
    && git init \
    && git add -A \
    && git commit -m "pristine state (image build)"

COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

USER demo

ENTRYPOINT ["/entrypoint.sh"]

EXPOSE 8080
