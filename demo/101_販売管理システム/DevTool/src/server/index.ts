import path from "node:path"
import { fileURLToPath } from "node:url"
import { serve } from "@hono/node-server"
import { Hono } from "hono"
import type { UIMessage } from "ai"
import { Preview, type PreviewProcessDefinition } from "./Preview.ts"
import { ChangePlan } from "./ChangePlan.ts"
import { ApiKey, ChatAgent, CurrentState } from "./ChatAgent"
import { PREVIEW_TARGET_ORIGIN, type DevToolSettings, type OpenRouterModelsResponse, type PreviewStateRequest, type PreviewStateResponse } from "../shared/devtool-api.ts"
import { DotEnv } from "./DotEnv.ts"
import { OpenRouterModels } from "./OpenRouterModels.ts"

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
const changePlans = new ChangePlan(devToolRoot)
const currentState = new CurrentState(devToolRoot)
const chatAgent = new ChatAgent(demo101Root, currentState)

const app = new Hono()

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

// デバッグ実行プロセスの稼働状態と、リクエストで指定されたオフセット以降のログ増分
app.post("/devtool-api/preview/state", async c => {
  const request: PreviewStateRequest = await c.req.json().catch(() => ({ offsets: {} }))
  const response: PreviewStateResponse = {
    processes: preview.getState().map(state => ({
      ...state,
      stdout: preview.readLog(state.name, "stdout", request.offsets[state.name]?.stdout ?? 0),
      stderr: preview.readLog(state.name, "stderr", request.offsets[state.name]?.stderr ?? 0),
    })),
    targetStatus: await preview.probeTargetStatus(),
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

//#region 変更計画

// 変更計画の一覧
app.get("/devtool-api/plans", async c => {
  return c.json(await changePlans.list())
})

// 変更計画1件の詳細
app.get("/devtool-api/plans/:id", async c => {
  const detail = await changePlans.read(c.req.param("id"))
  if (!detail) return c.json({ error: "指定された変更計画が見つかりません。" }, 404)
  return c.json(detail)
})

//#endregion 変更計画

//#region AIチャット

// チャット欄表示時の現在セッション状態同期
app.get("/devtool-api/current-session", async c => {
  const { currentSession } = await currentState.load()
  return c.json(currentSession)
})

// チャットメッセージ送信。 Vercel AI SDK の規約準拠のエンドポイント
app.post("/devtool-api/chat", async c => {
  const apiKey = await chatAgentApiKey.read()
  if (!apiKey) return c.json({ error: "OpenRouter APIキーが未設定です。設定画面から登録してください。" }, 400)

  const { messages } = await c.req.json<{ messages: UIMessage[] }>()
  const { chatModel } = await readSettings()
  return await chatAgent.respond(messages.at(-1), { apiKey, model: chatModel })
})

// 新規セッション開始
app.post("/devtool-api/new-chat", async c => {
  await currentState.startNewSession()
  return c.body(null, 204)
})

//#endregion AIチャット

const server = serve({
  fetch: app.fetch,
  port: PORT,
  // このサーバーは任意の子プロセスを起動し .env.local を読み書きしAPIキーを仲介するため、
  // ローカルホスト以外には公開しない
  hostname: "127.0.0.1",
})

server.on("error", (error: NodeJS.ErrnoException) => {
  if (error.code === "EADDRINUSE") {
    console.error(`ポート${PORT}が既に使用されています。他のプロセスが残っていないか確認してください。`)
  } else {
    console.error(error)
  }
})

// tsx watch による再起動・Ctrl+C・親からの終了要求のいずれでも子プロセスを確実に停止する。
// server.close() は開いたままの接続（チャットのストリーミング応答）があると返ってこないため待たない。
const shutdown = async (): Promise<void> => {
  await preview.stopAll()
  server.close()
  process.exit(0)
}
process.once("SIGINT", shutdown)
process.once("SIGTERM", shutdown)
