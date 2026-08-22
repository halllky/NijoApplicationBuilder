import React from "react"
import type { OpenRouterModel } from "../../shared/devtool-api"
import { useDevToolSettings } from "./useDevToolSettings"
import { useOpenRouterModels } from "./useOpenRouterModels"

/**
 * OpenRouter APIキーの登録・削除と、チャット・コーディング両エージェントのモデル設定を行う。
 * APIキーはサーバー側のOSキーチェーンに保存され、この画面には登録済みかどうかのみが表示される。
 */
export const AgentSettings: React.FC = () => {
  const { settings, saveModels, saveApiKey, clearApiKey, isSaving, error } = useDevToolSettings()
  const { models, error: modelsError } = useOpenRouterModels()
  const [chatModel, setChatModel] = React.useState('')
  const [codingModel, setCodingModel] = React.useState('')
  const [apiKeyInput, setApiKeyInput] = React.useState('')

  // サーバーから読み込んだ設定を、編集用フォームの初期値として反映する
  React.useEffect(() => {
    if (settings) {
      setChatModel(settings.chatModel)
      setCodingModel(settings.codingModel)
    }
  }, [settings])

  const handleSaveModels = () => {
    saveModels({ chatModel, codingModel })
  }

  const handleSaveApiKey = () => {
    if (!apiKeyInput) return
    saveApiKey(apiKeyInput)
    setApiKeyInput('')
  }

  return (
    <div className="flex flex-col gap-3">
      <h3 className="text-sm font-bold">エージェント設定</h3>

      {/* APIキー */}
      <div className="flex flex-col gap-1">
        <label className="text-xs text-gray-600">OpenRouter APIキー</label>
        <div className="flex gap-2">
          <input
            type="password"
            value={apiKeyInput}
            onChange={e => setApiKeyInput(e.target.value)}
            placeholder={settings?.hasApiKey ? '登録済み（変更する場合のみ入力）' : 'sk-or-v1-...'}
            className="flex-1 border border-gray-300 rounded px-2 py-1 text-sm"
          />
          <button
            type="button"
            onClick={handleSaveApiKey}
            disabled={isSaving || !apiKeyInput}
            className="px-2 py-1 text-sm bg-gray-800 text-white rounded disabled:opacity-50"
          >
            保存
          </button>
          {settings?.hasApiKey && (
            <button
              type="button"
              onClick={() => clearApiKey()}
              disabled={isSaving}
              className="px-2 py-1 text-sm text-gray-600 hover:bg-gray-100 rounded disabled:opacity-50"
            >
              削除
            </button>
          )}
        </div>
        {settings?.apiKeyStorage === 'unavailable' && (
          <p className="text-xs text-rose-600">
            この環境ではOSキーチェーンが使えません。環境変数 OPENROUTER_API_KEY を設定してください。
          </p>
        )}
        {settings?.apiKeyStorage === 'environmentVariable' && (
          <p className="text-xs text-gray-500">環境変数 OPENROUTER_API_KEY を使用しています。</p>
        )}
      </div>

      {/* モデル設定 */}
      <ModelSelect
        label="チャットエージェント用モデル"
        value={chatModel}
        onChange={setChatModel}
        models={models}
        modelsError={modelsError}
      />
      <ModelSelect
        label="コーディングエージェント用モデル（未使用・設定のみ）"
        value={codingModel}
        onChange={setCodingModel}
        models={models}
        modelsError={modelsError}
      />
      <div>
        <button
          type="button"
          onClick={handleSaveModels}
          disabled={isSaving}
          className="px-2 py-1 text-sm bg-gray-800 text-white rounded disabled:opacity-50"
        >
          モデル設定を保存
        </button>
      </div>

      {error && <p className="text-xs text-rose-600">{error}</p>}
    </div>
  )
}

type ModelSelectProps = {
  label: string
  value: string
  onChange: (value: string) => void
  /** OpenRouterから取得した選択可能なモデル一覧。取得中は空配列。 */
  models: OpenRouterModel[]
  /** 一覧取得時のエラーメッセージ。取得に失敗しても現在値の選択自体は維持する。 */
  modelsError?: string
}

/** OpenRouterのモデルIDをドロップダウンで選択させる。一覧取得中・失敗中でも現在値の選択状態は失わない。 */
const ModelSelect: React.FC<ModelSelectProps> = ({ label, value, onChange, models, modelsError }) => {
  const isKnownValue = models.some(m => m.id === value)
  const isStillLoading = models.length === 0 && !modelsError

  // 保存済みの値が一覧に無い場合（取得失敗・モデル廃止など）に選択肢が消えて設定が壊れて見えないよう、
  // 現在値を選択肢の先頭に補って選択状態を維持する。取得中（まだ一覧が空なだけ）は補わない。
  const options = isKnownValue || !value || isStillLoading
    ? models
    : [{ id: value, name: value, contextLength: 0 }, ...models]

  return (
    <div className="flex flex-col gap-1">
      <label className="text-xs text-gray-600">{label}</label>
      <select
        value={value}
        onChange={e => onChange(e.target.value)}
        className="border border-gray-300 rounded px-2 py-1 text-sm"
      >
        {options.map(m => (
          <option key={m.id} value={m.id}>{m.name}</option>
        ))}
      </select>
      {modelsError && <p className="text-xs text-amber-600">{modelsError}</p>}
      {!modelsError && !isStillLoading && !isKnownValue && value && (
        <p className="text-xs text-amber-600">選択中のモデルが一覧に見つかりません。廃止された可能性があります。</p>
      )}
    </div>
  )
}
