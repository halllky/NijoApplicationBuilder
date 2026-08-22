import React from "react"
import { useChat } from "@ai-sdk/react"
import { DefaultChatTransport } from "ai"

/**
 * 要件ヒアリングエージェントとのチャット画面。
 * 会話履歴はブラウザ内のReact stateのみで保持し、パネルを閉じる・再読み込みすると消える。
 */
export const ChatPane: React.FC<{ onApiKeyMissing: () => void }> = ({ onApiKeyMissing }) => {
  const { messages, sendMessage, status, error } = useChat({
    transport: new DefaultChatTransport({ api: '/devtool-api/chat' }),
  })
  const [input, setInput] = React.useState('')
  const isBusy = status === 'submitted' || status === 'streaming'

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const text = input.trim()
    if (!text || isBusy) return
    sendMessage({ text })
    setInput('')
  }

  return (
    <div className="flex-1 flex flex-col min-w-0">

      {/* 発言一覧 */}
      <div className="flex-1 overflow-y-auto p-3 flex flex-col gap-3">
        {messages.length === 0 && (
          <p className="text-sm text-gray-500">要件を話しかけてください。</p>
        )}
        {messages.map(message => (
          <div key={message.id} className={`flex ${message.role === 'user' ? 'justify-end' : 'justify-start'}`}>
            <div
              className={`max-w-[80%] rounded px-3 py-2 text-sm whitespace-pre-wrap ${message.role === 'user' ? 'bg-gray-800 text-white' : 'bg-gray-100 text-gray-900'
                }`}
            >
              {message.parts
                .filter(part => part.type === 'text')
                .map((part, i) => <span key={i}>{part.text}</span>)}
            </div>
          </div>
        ))}
      </div>

      {/* エラー表示。APIキー未設定の場合は設定画面への導線を出す */}
      {error && (
        <div className="px-3 py-2 text-xs text-rose-600 border-t border-gray-200 flex items-center justify-between gap-2">
          <span>{error.message}</span>
          <button type="button" onClick={onApiKeyMissing} className="underline shrink-0">
            設定を開く
          </button>
        </div>
      )}

      {/* 入力欄 */}
      <form onSubmit={handleSubmit} className="flex gap-2 p-3 border-t border-gray-200">
        <input
          value={input}
          onChange={e => setInput(e.target.value)}
          placeholder="メッセージを入力"
          className="flex-1 border border-gray-300 rounded px-2 py-1 text-sm"
        />
        <button
          type="submit"
          disabled={isBusy || !input.trim()}
          className="px-3 py-1 text-sm bg-gray-800 text-white rounded disabled:opacity-50"
        >
          送信
        </button>
      </form>
    </div>
  )
}
