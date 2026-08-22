import React from "react"
import { XMarkIcon } from "@heroicons/react/24/outline"
import { PreviewProcessState } from "./usePreviewState"
import { useDevToolSettings } from "./useDevToolSettings"

/**
 * デバッグ実行プロセス群の稼働状態・ログの表示と、エージェント関連の設定
 * （Anthropic APIキー、チャット・コーディング両エージェントのモデル）の編集を行うモーダル。
 * 閉じる操作（シェードクリック・閉じるボタン）は onClose を呼ぶだけで、
 * 実際に閉じるかどうかの判断はこのコンポーネントを呼ぶ側が行う。
 */
export const SettingsModal = ({ open, onClose, processes, logs }: {
  open: boolean
  onClose: () => void
  processes: PreviewProcessState[]
  logs: Record<string, { stdout: string, stderr: string }>
}) => {
  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">

      {/* シェード */}
      <div className="absolute inset-0 bg-black/25" onClick={onClose} />

      {/* パネル */}
      <div className="relative bg-white rounded shadow-lg w-[720px] max-w-[90vw] max-h-[85vh] flex flex-col">

        {/* ヘッダ */}
        <div className="flex items-center justify-between px-4 py-2 border-b border-gray-200">
          <h2 className="text-base font-bold">デバッグ実行プロセスの状態・設定</h2>
          <button type="button" onClick={onClose} className="p-1 text-gray-500 hover:text-gray-800">
            <XMarkIcon className="w-5 h-5" />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-6">

          {/* プロセス一覧・ログ */}
          <div className="flex flex-col gap-4">
            {processes.length === 0 && (
              <p className="text-sm text-gray-500">稼働中のプロセスはありません。</p>
            )}
            {processes.map(p => (
              <div key={p.name} className="flex flex-col gap-1">
                {/* プロセス名と稼働状態 */}
                <div className="flex items-center gap-2 text-sm">
                  <span className="font-bold">{p.name}</span>
                  <span className={p.isRunning ? 'text-emerald-600' : 'text-gray-500'}>
                    {p.isRunning ? `稼働中 (PID=${p.processId})` : `停止 (ExitCode=${p.exitCode ?? '?'})`}
                  </span>
                </div>
                {/* 標準出力・標準エラー出力 */}
                <LogPane text={logs[p.name]?.stdout ?? ''} />
                <LogPane text={logs[p.name]?.stderr ?? ''} isError />
              </div>
            ))}
          </div>

          {/* エージェント設定（APIキー・モデル） */}
          <AgentSettingsSection />
        </div>
      </div>
    </div>
  )
}

/** ログ1本分の表示欄。追記のたびに末尾へ自動スクロールする */
const LogPane: React.FC<{ text: string, isError?: boolean }> = ({ text, isError }) => {
  const ref = React.useRef<HTMLPreElement>(null)

  // DOM要素の末尾へのスクロール位置は描画結果に依存するため、DOM操作としてeffectで行う
  React.useEffect(() => {
    const el = ref.current
    if (el) el.scrollTop = el.scrollHeight
  }, [text])

  return (
    <pre
      ref={ref}
      className={`h-[120px] overflow-auto bg-gray-900 text-xs p-2 whitespace-pre-wrap rounded ${isError ? 'text-rose-400' : 'text-gray-100'}`}
    >
      {text}
    </pre>
  )
}

/**
 * Anthropic APIキーの登録・削除と、チャット・コーディング両エージェントのモデル設定を行う。
 * APIキーはサーバー側のOSキーチェーンに保存され、この画面には登録済みかどうかのみが表示される。
 */
const AgentSettingsSection: React.FC = () => {
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
