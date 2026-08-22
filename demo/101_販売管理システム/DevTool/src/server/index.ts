import { randomUUID } from "node:crypto"
import path from "node:path"
import { fileURLToPath } from "node:url"
import { serve } from "@hono/node-server"
import { Hono } from "hono"
import type { UIMessage, Warning } from "ai"
import { Preview, type PreviewProcessDefinition } from "./Preview.ts"
import { ApiKey, ChatTurnLogger, RootAgent, ChatSession, extractMessageText, isRateLimitError, toUserFacingMessage } from "./ChatAgent"
import { PREVIEW_TARGET_ORIGIN, type DevToolSettings, type DevToolStateRequest, type DevToolStateResponse, type OpenRouterModelsResponse } from "../shared/devtool-api.ts"
import { DotEnv } from "./DotEnv.ts"
import { OpenRouterModels } from "./OpenRouterModels.ts"
import { serverLog, type ServerLog } from "./ServerLog.ts"

const PORT = 5184

// このファイルの位置から解決する（実行時のカレントディレクトリに依存させないため）
const devToolRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..")
const demo101Root = path.dirname(devToolRoot)

const PREVIEW_PROCESS_DEFINITIONS: readonly PreviewProcessDefinition[] = [
  { name: "vite", cwd: "client", fileName: "npm", args: ["run", "dev"], appendStdout: false, appendStderr: true },
  { name: "dotnet", cwd: "WebApi", fileName: "dotnet", args: ["run", "--launch-profile", "http"], appendStdout: false, appendStderr: true },
]

const preview = new Preview(demo101Root, PREVIEW_PROCESS_DEFINITIONS, PREVIEW_TARGET_ORIGIN)
const chatAgentApiKey = new ApiKey()
const dotEnv = new DotEnv(devToolRoot)
const openRouterModels = new OpenRouterModels()
const chatSessions = new ChatSession(devToolRoot)
const rootAgent = new RootAgent(demo101Root, chatSessions)

// AI SDK / プロバイダーが出す警告（非対応設定など）を、既定の何もしない挙動から接続する。
// 3行で全プロバイダー呼び出しの警告を横断的に拾えるようになる。
globalThis.AI_SDK_LOG_WARNINGS = (options: { warnings: Warning[], provider?: string, model?: string }) => {
  serverLog.warn("llm.warning", { provider: options.provider, model: options.model, warnings: options.warnings })
}

const app = new Hono<{ Variables: { log: ServerLog } }>()

//#region 横断ログ

/** リクエストの所要時間がこれを超えたら遅延として警告に引き上げる */
const SLOW_HTTP_MS = 1_000

/** 1秒間隔でタブの数だけポーリングされるため、正常時（200・非遅延）は記録しない。さもないとこのログ自体が画面のログペインを埋め尽くす。 */
const QUIET_PATHS = new Set(["/devtool-api/state"])

// 1リクエストごとに相関ID付きのロガーを作り、後続のハンドラから c.get("log") で使えるようにする。
// 所要時間の計測・記録はここでしか行わない（順序をエントリーポイントに集約する）。
app.use("*", async (c, next) => {
  const requestId = randomUUID().slice(0, 8)
  c.set("log", serverLog.scoped({ requestId }))
  const startedAt = performance.now()

  await next()

  const ms = performance.now() - startedAt
  const slow = ms > SLOW_HTTP_MS
  if (QUIET_PATHS.has(c.req.path) && c.res.status < 400 && !slow) return

  const fields = { method: c.req.method, path: c.req.path, status: c.res.status, ms: Math.round(ms), slow: slow || undefined }
  if (slow || c.res.status >= 500) c.get("log").warn("http.request", fields)
  else c.get("log").info("http.request", fields)
})

app.onError((error, c) => {
  // 直前のミドルウェアで必ず c.set("log", ...) 済みだが、そこに至る前の失敗に備えてフォールバックする
  const log = c.get("log") ?? serverLog
  const rateLimited = isRateLimitError(error)
  log.error("http.error", { error, method: c.req.method, path: c.req.path, isRateLimit: rateLimited })
  return c.json({ error: toUserFacingMessage(error) }, rateLimited ? 429 : 500)
})

app.notFound(c => {
  const log = c.get("log") ?? serverLog
  log.warn("http.notfound", { method: c.req.method, path: c.req.path })
  return c.json({ error: "指定されたエンドポイントが見つかりません。" }, 404)
})

//#endregion 横断ログ

//#region デバッグ

// デバッグ実行プロセスの起動
app.post("/devtool-api/preview/start", async c => {
  await preview.start(c.req.query("process"))
  return c.body(null, 204)
})
// デバッグ実行プロセスの停止
app.post("/devtool-api/preview/stop", async c => {
  await preview.stop(c.req.query("process"))
  return c.body(null, 204)
})
// デバッグ実行プロセスの再起動
app.post("/devtool-api/preview/restart", async c => {
  await preview.restart(c.req.query("process"))
  return c.body(null, 204)
})

// DevToolの稼働状態: デバッグ実行プロセスの稼働状態・ログ増分と、DevToolサーバー自身のログ増分をまとめて返す
app.post("/devtool-api/state", async c => {
  const request: DevToolStateRequest = await c.req.json().catch(() => ({ offsets: {}, serverLogOffset: 0 }))
  const response: DevToolStateResponse = {
    processes: preview.getState().map(state => ({
      ...state,
      stdout: preview.readLog(state.name, "stdout", request.offsets[state.name]?.stdout ?? 0),
      stderr: preview.readLog(state.name, "stderr", request.offsets[state.name]?.stderr ?? 0),
    })),
    targetStatus: await preview.probeTargetStatus(),
    serverLog: serverLog.readTail(request.serverLogOffset ?? 0),
  }
  return c.json(response)
})

//#endregion デバッグ

//#region アプリケーション設定

/** 各ストレージから設定を読み込んでまとめて返す */
async function readSettings(): Promise<DevToolSettings> {
  const models = await dotEnv.readModels()
  const apiKeyStorage = await chatAgentApiKey.storage()
  const hasApiKey = (await chatAgentApiKey.read()) !== null
  return { ...models, apiKeyStorage, hasApiKey }
}

app.get("/devtool-api/settings", async c => {
  return c.json(await readSettings())
})
app.put("/devtool-api/settings/models", async c => {
  const body = await c.req.json<{ chatModel: string, codingModel: string }>()
  await dotEnv.writeModels(body)
  return c.json(await readSettings())
})
app.put("/devtool-api/settings/api-key", async c => {
  const body = await c.req.json<{ apiKey: string }>()
  const apiKey = body.apiKey.trim()
  // 半角ASCII文字以外（誤って別の文字列を貼り付けた場合など）を弾く。
  // ここで弾かないと、後段のAI SDKプロバイダーがHTTPヘッダー生成時に投げる分かりにくいエラーで落ちる。
  if (!/^[\x21-\x7e]+$/.test(apiKey)) {
    return c.json({ error: "APIキーの形式が不正です。sk-or-v1- から始まる半角英数字の文字列を入力してください。" }, 400)
  }
  const saved = await chatAgentApiKey.write(apiKey)
  if (!saved) return c.json({ error: `この環境ではOSキーチェーンが使えません。環境変数 ${ApiKey.ENV_OPENROUTER} を設定してください。` }, 501)
  return c.json(await readSettings())
})
app.delete("/devtool-api/settings/api-key", async c => {
  await chatAgentApiKey.delete()
  return c.json(await readSettings())
})

// OpenRouterで選択可能なモデルの一覧（設定画面のドロップダウン用）
app.get("/devtool-api/openrouter/models", async c => {
  const response: OpenRouterModelsResponse = await openRouterModels.list()
  return c.json(response)
})
//#endregion アプリケーション設定

//#region チャットセッション

// セッションの一覧（変更計画の本文・会話の本文は含まない要約）
app.get("/devtool-api/sessions", async c => {
  return c.json(await chatSessions.list())
})

// 新規セッションの作成。発言はまだ含まない空のセッションを返す
app.post("/devtool-api/sessions", async c => {
  return c.json(await chatSessions.create())
})

// セッション1件（会話の全メッセージと変更計画）
app.get("/devtool-api/sessions/:id", async c => {
  const session = await chatSessions.read(c.req.param("id"))
  if (!session) return c.json({ error: "指定されたチャットセッションが見つかりません。" }, 404)
  return c.json(session)
})

// セッションの削除
app.delete("/devtool-api/sessions/:id", async c => {
  await chatSessions.delete(c.req.param("id"))
  return c.body(null, 204)
})

// チャットメッセージ送信。 Vercel AI SDK の規約準拠のエンドポイント
app.post("/devtool-api/sessions/:id/chat", async c => {
  const apiKey = await chatAgentApiKey.read()
  if (!apiKey) return c.json({ error: "OpenRouter APIキーが未設定です。設定画面から登録してください。" }, 400)

  const { messages } = await c.req.json<{ messages: UIMessage[] }>()
  const { chatModel } = await readSettings()
  const sessionId = c.req.param("id")
  const newUserMessage = messages.at(-1)
  const log = c.get("log").scoped({ sessionId })
  const turnLogger = new ChatTurnLogger(log)

  return await rootAgent.respond(sessionId, newUserMessage, {
    apiKey,
    model: chatModel,
    turnLogger,
    log,
    abortSignal: c.req.raw.signal,
  })
})

//#endregion チャットセッション

const server = serve({
  fetch: app.fetch,
  port: PORT,
  // このサーバーは任意の子プロセスを起動し .env.local を読み書きしAPIキーを仲介するため、
  // ローカルホスト以外には公開しない
  hostname: "127.0.0.1",
}, () => {
  serverLog.info("server.start", {
    port: PORT,
    devToolRoot,
    demo101Root,
    logLevel: process.env.LOG_LEVEL ?? "info",
    fly: Boolean(process.env.FLY_APP_NAME),
  })
})

server.on("error", (error: NodeJS.ErrnoException) => {
  if (error.code === "EADDRINUSE") {
    serverLog.error("server.error", { error: `ポート${PORT}が既に使用されています。他のプロセスが残っていないか確認してください。` })
  } else {
    serverLog.error("server.error", { error })
  }
})

// 通常のハンドラを経由しない失敗（コールバック内の例外・未処理rejection）は、放置すると画面にも fly logs にも一切残らない。
process.on("uncaughtException", error => serverLog.error("process.uncaught", { error }))
process.on("unhandledRejection", reason => serverLog.error("process.unhandled", { error: reason instanceof Error ? reason : String(reason) }))

// tsx watch による再起動・Ctrl+C・親からの終了要求のいずれでも子プロセスを確実に停止する。
// server.close() は開いたままの接続（チャットのストリーミング応答）があると返ってこないため待たない。
const shutdown = async (): Promise<void> => {
  serverLog.info("server.stop", {})
  await preview.stopAll()
  await serverLog.flush() // 書き込み中のトレースファイルを取りこぼさないよう待つ
  server.close()
  process.exit(0)
}
process.once("SIGINT", shutdown)
process.once("SIGTERM", shutdown)
