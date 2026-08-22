import path from "node:path"
import { fileURLToPath } from "node:url"
import { serve } from "@hono/node-server"
import { Hono } from "hono"
import type { UIMessage } from "ai"
import { Preview, type PreviewProcessDefinition } from "./Preview.ts"
import { DevToolSettings } from "./DevToolSettings.ts"
import { ChangePlan } from "./ChangePlan.ts"
import { ChatAgent } from "./ChatAgent.ts"
import { PREVIEW_TARGET_ORIGIN, type PreviewStateRequest, type PreviewStateResponse } from "../shared/devtool-api.ts"

const PORT = 5184

// このファイルの位置から解決する（実行時のカレントディレクトリに依存させないため）
const clientRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..")
const projectRoot = path.dirname(clientRoot)

const PREVIEW_PROCESS_DEFINITIONS: readonly PreviewProcessDefinition[] = [
  { name: "vite", cwd: "client", fileName: "npm", args: ["run", "dev"], appendStdout: false, appendStderr: true },
  { name: "dotnet", cwd: "WebApi", fileName: "dotnet", args: ["run", "--launch-profile", "http"], appendStdout: false, appendStderr: true },
]

const preview = new Preview(projectRoot, PREVIEW_PROCESS_DEFINITIONS, PREVIEW_TARGET_ORIGIN)
const settings = new DevToolSettings(path.join(clientRoot, ".env.local"))
const changePlans = new ChangePlan(path.join(clientRoot, ".nijo", "plans"))
const chatAgent = new ChatAgent()

const app = new Hono()

// デバッグ実行プロセスの起動・停止・再起動。
// process クエリパラメータを指定すればそのプロセスのみ、未指定なら全プロセスを対象とする。
app.post("/devtool-api/preview/start", async c => {
  await preview.start(c.req.query("process"))
  return c.body(null, 204)
})
app.post("/devtool-api/preview/stop", async c => {
  await preview.stop(c.req.query("process"))
  return c.body(null, 204)
})
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

// 設定の取得・保存
app.get("/devtool-api/settings", async c => {
  return c.json(await settings.read())
})
app.put("/devtool-api/settings/models", async c => {
  const body = await c.req.json<{ chatModel: string, codingModel: string }>()
  await settings.writeModels(body)
  return c.json(await settings.read())
})
app.put("/devtool-api/settings/api-key", async c => {
  const body = await c.req.json<{ apiKey: string }>()
  const saved = await settings.writeApiKey(body.apiKey)
  if (!saved) return c.json({ error: "この環境ではOSキーチェーンが使えません。環境変数 ANTHROPIC_API_KEY を設定してください。" }, 501)
  return c.json(await settings.read())
})
app.delete("/devtool-api/settings/api-key", async c => {
  await settings.deleteApiKey()
  return c.json(await settings.read())
})

// 変更計画の一覧・詳細取得（読み取り専用。登録処理は別スコープ）
app.get("/devtool-api/plans", async c => {
  return c.json(await changePlans.list())
})
app.get("/devtool-api/plans/:id", async c => {
  const detail = await changePlans.read(c.req.param("id"))
  if (!detail) return c.json({ error: "指定された変更計画が見つかりません。" }, 404)
  return c.json(detail)
})

// 要件ヒアリングのチャット
app.post("/devtool-api/chat", async c => {
  const apiKey = await settings.readApiKey()
  if (!apiKey) return c.json({ error: "Anthropic APIキーが未設定です。設定画面から登録してください。" }, 400)

  const { messages } = await c.req.json<{ messages: UIMessage[] }>()
  const { chatModel } = await settings.read()
  return await chatAgent.respond(messages, { apiKey, model: chatModel })
})

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
