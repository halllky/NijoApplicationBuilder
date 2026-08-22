import React from "react"
import type { OpenRouterModel } from "../../shared/devtool-api"

/**
 * OpenRouterで選択可能なモデル一覧（無料かつツール呼び出し対応のもの）をサーバー経由で取得する。
 * 一覧はマウント時に一度だけ取得する。
 */
export function useOpenRouterModels() {
  const [models, setModels] = React.useState<OpenRouterModel[]>([])
  const [error, setError] = React.useState<string>()

  React.useEffect(() => {
    let cancelled = false
    fetch('/devtool-api/openrouter/models')
      .then(res => res.json())
      .then((body: { models: OpenRouterModel[], error?: string }) => {
        if (cancelled) return
        setModels(body.models)
        setError(body.error)
      })
      .catch(e => {
        if (cancelled) return
        setError(e instanceof Error ? e.message : `不明なエラー(${e})`)
      })
    // アンマウント後に届いた応答でstateを更新しないためのガード
    return () => { cancelled = true }
  }, [])

  return { models, error }
}
