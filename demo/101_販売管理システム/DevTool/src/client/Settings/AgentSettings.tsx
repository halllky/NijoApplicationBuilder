import React from "react"
import { useDevToolSettings } from "./useDevToolSettings"

/**
 * Anthropic APIキーの登録・削除と、チャット・コーディング両エージェントのモデル設定を行う。
 * APIキーはサーバー側のOSキーチェーンに保存され、この画面には登録済みかどうかのみが表示される。
 */
export const AgentSettings: React.FC = () => {
  const { settings, saveModels, saveApiKey, clearApiKey, isSaving, error } = useDevToolSettings()
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
        <label className="text-xs text-gray-600">Anthropic APIキー</label>
        <div className="flex gap-2">
          <input
            type="password"
            value={apiKeyInput}
            onChange={e => setApiKeyInput(e.target.value)}
            placeholder={settings?.hasApiKey ? '登録済み（変更する場合のみ入力）' : 'sk-ant-...'}
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
            この環境ではOSキーチェーンが使えません。環境変数 ANTHROPIC_API_KEY を設定してください。
          </p>
        )}
        {settings?.apiKeyStorage === 'environmentVariable' && (
          <p className="text-xs text-gray-500">環境変数 ANTHROPIC_API_KEY を使用しています。</p>
        )}
      </div>

      {/* モデル設定 */}
      <div className="flex flex-col gap-1">
        <label className="text-xs text-gray-600">チャットエージェント用モデル</label>
        <input
          value={chatModel}
          onChange={e => setChatModel(e.target.value)}
          className="border border-gray-300 rounded px-2 py-1 text-sm"
        />
      </div>
      <div className="flex flex-col gap-1">
        <label className="text-xs text-gray-600">コーディングエージェント用モデル（未使用・設定のみ）</label>
        <input
          value={codingModel}
          onChange={e => setCodingModel(e.target.value)}
          className="border border-gray-300 rounded px-2 py-1 text-sm"
        />
      </div>
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
