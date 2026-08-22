import type { OpenRouterModel } from "../shared/devtool-api.ts"

/** OpenRouterのモデル一覧取得1回あたりのタイムアウト */
const FETCH_TIMEOUT_MS = 10_000

const MODELS_URL = "https://openrouter.ai/api/v1/models"

/** OpenRouter API が返す1モデル分のレコード（このモジュールが使う項目のみ） */
type OpenRouterModelsApiRecord = {
  id: string
  name: string
  context_length: number
  supported_parameters?: string[]
}

/**
 * OpenRouterで選択可能なモデルの一覧を取得する。
 * 認証不要のエンドポイントを使うため、APIキー登録前でも呼べる。
 * このアプリのエージェントはツール呼び出しを必須で使うため、無料かつツール対応のモデルのみに絞り込む。
 */
export class OpenRouterModels {
  #cache: OpenRouterModel[] | null = null

  /** 一覧を返す。取得に失敗した場合は例外を投げず、直近の成功結果（未取得なら空配列）を返す。 */
  async list(): Promise<{ models: OpenRouterModel[], error?: string }> {
    try {
      const res = await fetch(MODELS_URL, { signal: AbortSignal.timeout(FETCH_TIMEOUT_MS) })
      if (!res.ok) throw new Error(`OpenRouter API がエラーを返しました（HTTP ${res.status}）`)

      const body = await res.json() as { data: OpenRouterModelsApiRecord[] }
      this.#cache = body.data
        .filter(m => m.id.endsWith(":free") && (m.supported_parameters ?? []).includes("tools"))
        .map(m => ({ id: m.id, name: m.name, contextLength: m.context_length }))
        .sort((a, b) => a.name.localeCompare(b.name))

      return { models: this.#cache }
    } catch (e) {
      // 一覧はほぼ変動しないため、取得に失敗しても直近のキャッシュがあればそれを返す
      const message = e instanceof Error ? e.message : `不明なエラー(${e})`
      return { models: this.#cache ?? [], error: `モデル一覧の取得に失敗しました: ${message}` }
    }
  }
}
