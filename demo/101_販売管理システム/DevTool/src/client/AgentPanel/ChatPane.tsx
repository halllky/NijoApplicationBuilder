import React from "react"
import { useChat } from "@ai-sdk/react"
import { DefaultChatTransport, getToolName, isToolUIPart, type DynamicToolUIPart, type ToolUIPart, type UITools } from "ai"

/**
 * 要件ヒアリングエージェントとのチャット画面。
 * 会話履歴はサーバー側（.nijo/current-state.json）が正であり、表示時にそこから読み込んで復元する。
 * 「新しいチャット」ボタンで現在の会話を締め、空の状態から話しかけ直せる。
 */
export const ChatPane: React.FC<{ onApiKeyMissing: () => void }> = ({ onApiKeyMissing }) => {
  const { messages, setMessages, sendMessage, status, error } = useChat({
    transport: new DefaultChatTransport({ api: '/devtool-api/chat' }),
  })
  const [input, setInput] = React.useState('')
  const [isLoadingHistory, setIsLoadingHistory] = React.useState(true)
  const isBusy = status === 'submitted' || status === 'streaming'

  // 表示時（マウント時）にサーバー側に永続化された会話履歴と同期する
  React.useEffect(() => {
    let canceled = false
    fetch('/devtool-api/current-session')
      .then(res => res.ok ? res.json() : null)
      .then(history => { if (!canceled && history) setMessages(history) })
      .finally(() => { if (!canceled) setIsLoadingHistory(false) })
    return () => { canceled = true }
  }, [setMessages])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const text = input.trim()
    if (!text || isBusy || isLoadingHistory) return
    sendMessage({ text })
    setInput('')
  }

  const formRef = React.useRef<HTMLFormElement>(null)
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      formRef.current?.requestSubmit()
    }
  }

  // 新しいチャット: 現在の会話をサーバー側で latestSessions に退避してから画面を空にする
  const handleNewChat = async () => {
    if (messages.length === 0 || isBusy) return
    const res = await fetch('/devtool-api/new-chat', { method: 'POST' })
    if (res.ok) setMessages([])
  }

  return (
    <div className="flex-1 flex flex-col min-w-0">

      {/* ヘッダ（新しいチャットボタン） */}
      <div className="flex justify-end px-3 py-2 border-b border-gray-200">
        <button
          type="button"
          onClick={handleNewChat}
          disabled={messages.length === 0 || isBusy || isLoadingHistory}
          className="px-2 py-1 text-xs border border-gray-300 rounded text-gray-700 disabled:opacity-50"
        >
          新しいチャット
        </button>
      </div>

      {/* 発言一覧 */}
      <div className="flex-1 overflow-y-auto p-3 flex flex-col gap-3">
        {isLoadingHistory && (
          <p className="text-sm text-gray-500">読み込み中…</p>
        )}
        {!isLoadingHistory && messages.length === 0 && (
          <p className="text-sm text-gray-500">要件を話しかけてください。</p>
        )}
        {messages.map((message, ix) => (
          <div key={ix} className={`flex ${message.role === 'user' ? 'justify-end' : 'justify-start'}`}>
            <div
              className={`max-w-[80%] rounded px-3 py-2 text-sm whitespace-pre-wrap ${message.role === 'user' ? 'bg-gray-800 text-white' : 'bg-gray-100 text-gray-900'
                }`}
            >
              {message.parts.map((part, i) => {
                if (part.type === 'text') return <span key={i}>{part.text}</span>
                if (isToolUIPart(part)) return <ToolCallBadge key={i} part={part} />
                return null
              })}
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
      <form
        ref={formRef}
        onSubmit={handleSubmit}
        className="flex gap-2 p-3 border-t border-gray-200 overflow-hidden"
      >
        <textarea
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={isLoadingHistory}
          spellCheck="false"
          placeholder="メッセージを入力"
          className="flex-1 border border-gray-300 rounded px-2 py-1 text-sm max-h-96 resize-none field-sizing-content"
        />
        <button
          type="submit"
          disabled={isBusy || isLoadingHistory || !input.trim()}
          className="px-3 py-1 text-sm bg-gray-800 text-white rounded disabled:opacity-50"
        >
          送信<br />
          <span className="text-xs">
            (Ctrl + Enter)
          </span>
        </button>
      </form>
    </div>
  )
}

/**
 * ツール呼び出し1回分を表す小さなバッジ。
 * 実行中／完了／失敗の状態に応じて見た目を変える。入力・出力の中身は details で折りたたむ。
 */
const ToolCallBadge: React.FC<{ part: ToolUIPart<UITools> | DynamicToolUIPart }> = ({ part }) => {
  const name = getToolName(part)

  if (part.state === 'output-error') {
    return (
      <div className="text-xs text-rose-600 border border-rose-200 rounded px-2 py-1 my-1 bg-rose-50">
        {name} の実行に失敗しました: {part.errorText}
      </div>
    )
  }

  if (part.state === 'output-available') {
    return (
      <details className="text-xs border border-gray-200 rounded px-2 py-1 my-1 bg-white">
        <summary className="cursor-pointer text-gray-600">{name} を実行しました</summary>
        <pre className="mt-1 whitespace-pre-wrap break-words text-gray-700">
          {JSON.stringify(part.output, null, 2)}
        </pre>
      </details>
    )
  }

  // input-streaming / input-available / approval-* のいずれも「実行中」として一括りに表示する
  return (
    <div className="text-xs text-gray-500 border border-gray-200 rounded px-2 py-1 my-1 bg-white animate-pulse">
      {name} を実行中…
    </div>
  )
}
