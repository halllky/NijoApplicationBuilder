import React from "react"
import type { DevToolSettings } from "../../shared/devtool-api"

/**
 * DevTool の設定（チャット・コーディング両エージェントのモデル、OpenRouter APIキーの登録状態）を
 * 取得・更新する。APIキー本体はサーバーから返されず、登録済みかどうかのみを扱う。
 */
export function useDevToolSettings() {
  const [settings, setSettings] = React.useState<DevToolSettings>()
  const [isSaving, setIsSaving] = React.useState(false)
  const [error, setError] = React.useState<string>()

  const reload = React.useCallback(async () => {
    try {
      const res = await fetch('/devtool-api/settings')
      if (res.ok) setSettings(await res.json())
    } catch {
      // 読み込み失敗時は前回の値のまま据え置く
    }
  }, [])

  // サーバー側で保持している設定と起動時に同期する
  React.useEffect(() => {
    reload()
  }, [reload])

  const saveModels = React.useCallback(async (models: { chatModel: string, codingModel: string }) => {
    setIsSaving(true)
    setError(undefined)
    try {
      const res = await fetch('/devtool-api/settings/models', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(models),
      })
      if (res.ok) setSettings(await res.json())
      else setError((await res.json().catch(() => undefined))?.error ?? await res.text())
    } catch (e) {
      setError(e instanceof Error ? e.message : `不明なエラー(${e})`)
    } finally {
      setIsSaving(false)
    }
  }, [])

  const saveApiKey = React.useCallback(async (apiKey: string) => {
    setIsSaving(true)
    setError(undefined)
    try {
      const res = await fetch('/devtool-api/settings/api-key', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ apiKey }),
      })
      if (res.ok) setSettings(await res.json())
      else setError((await res.json().catch(() => undefined))?.error ?? await res.text())
    } catch (e) {
      setError(e instanceof Error ? e.message : `不明なエラー(${e})`)
    } finally {
      setIsSaving(false)
    }
  }, [])

  const clearApiKey = React.useCallback(async () => {
    setIsSaving(true)
    setError(undefined)
    try {
      const res = await fetch('/devtool-api/settings/api-key', { method: 'DELETE' })
      if (res.ok) setSettings(await res.json())
      else setError(await res.text())
    } catch (e) {
      setError(e instanceof Error ? e.message : `不明なエラー(${e})`)
    } finally {
      setIsSaving(false)
    }
  }, [])

  return { settings, saveModels, saveApiKey, clearApiKey, isSaving, error }
}
