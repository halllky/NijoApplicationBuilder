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
# stage3: 実行環境。
# デモ101はpublish済みの単一プロセス(WebApiがAPIとSPAの両方を配信)として常駐実行するため
# ランタイムイメージでも足りるが、AIチャットによるスキーマ変更時に
# RELEASE_BUILD.sh(nijo generate + dotnet publish + npm run build)を都度実行するため、
# 引き続きSDKイメージを使う。
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
COPY --chmod=755 docker/entrypoint.sh /entrypoint.sh

# nijo CLIをシェルコマンドとして呼び出せるようにする(RELEASE_BUILD.sh が
# $NIJO_CLI_PATH 経由で "nijo generate" を呼ぶため。Task/RELEASE_BUILD.sh 参照)。
RUN printf '#!/bin/bash\nexec dotnet /opt/nijo/nijo.dll "$@"\n' > /usr/local/bin/nijo \
    && chmod +x /usr/local/bin/nijo
ENV NIJO_CLI_PATH=/usr/local/bin/nijo

# ---------------------------------------------------------------------------
# demo101 はモノレポ(ルート package.json の npm workspaces)の一員であり、
# react/vite/各種プラグイン等の実依存はすべてルート package.json 側に宣言されている。
# さらに demo101 client は別ワークスペース @nijo/ui-components(Nijo.GuiClient/
# package_ui-components)をソースとして import している。
# そのため demo101 サブツリー単独で npm ci しても依存は一切入らない(vite: not found)。
#
# 対策: モノレポのルート(/app/monorepo)を用意し、そこで npm ci する。
#   - 実依存はルート node_modules に hoist される
#   - @nijo/ui-components はワークスペース symlink で解決される
#   - vite は /app/monorepo/demo/101_.../client で起動し、node解決が上位の
#     /app/monorepo/node_modules まで遡って依存を見つける
# (ルート package.json は他にも多数のワークスペースを列挙しているが、
#  存在しないワークスペースは npm ci が無視する ― stage1 と同じ挙動)
#
# /app/monorepo は npm ci が node_modules を作成する場所なので demo 所有にする。
# WORKDIR で暗黙生成させると root 所有になり、後段の npm ci が EACCES になる。
RUN mkdir -p /app/monorepo && chown demo:demo /app/monorepo
WORKDIR /app/monorepo
COPY --chown=demo:demo package.json package-lock.json ./
COPY --chown=demo:demo Nijo.GuiClient ./Nijo.GuiClient
COPY --chown=demo:demo "demo/101_販売管理システム" "./demo/101_販売管理システム"

# ⚠️ ビルドキャッシュの焼き込みは実行時ユーザー(demo)と同じユーザーで実行すること
# (USER demo より後に置くこと)。rootで実行すると node_modules / bin / obj / .git が
# root所有になり、実行時に vite が node_modules/.vite を書けずEACCESで即死する、
# dotnet watch がobjへ書けず失敗する、gitが「dubious ownership」で失敗する、等が起きる。
USER demo

# ルートで npm ci(実依存の hoist + @nijo/ui-components の symlink 解決)
RUN npm ci

# demo101を本番モード単一プロセス用にビルドする(nijo generate + dotnet publish +
# npm run build。Task/RELEASE_BUILD.sh参照)。publish成果物(WebApi/bin/Release/publish)と
# SPA(WebApi/wwwroot)も含めて次のpristineコミットに含めることで、
# 環境リセット時は git checkout だけで(再ビルド無しで)復元できるようにする
# (AIによるスキーマ変更時のみ、その場でRELEASE_BUILD.shが再実行される)。
# git repo は demo101 フォルダ自身(= WorkspaceRoot)。node_modules はその外側(モノレポ
# ルート)にあるため、リセット時の git checkout/clean で消えない。
WORKDIR "/app/monorepo/demo/101_販売管理システム"
RUN VITE_DEMO_BASE=/demo/ VITE_API_BASE_URL=/demo-api/ bash Task/RELEASE_BUILD.sh
RUN git config --global user.email "demo@example.com" \
    && git config --global user.name "demo" \
    && git init \
    && git add -A \
    && git commit -m "pristine state (image build)"

ENTRYPOINT ["/entrypoint.sh"]

EXPOSE 8080
