import React from "react"
import { useChat } from "@ai-sdk/react"
import { DefaultChatTransport, getToolName, isToolUIPart, type DynamicToolUIPart, type ToolUIPart, type UITools } from "ai"
import type { ChatSessionDto } from "../../shared/devtool-api"

/**
 * 1つのチャットセッションとのやりとりを表示する画面。
 * 会話履歴はサーバー側が正であり、表示時にそこから読み込んで復元する。
 * 高さ・幅は呼び出し側のレイアウトに従って伸縮する（自身では画面上の位置や大きさを決めない）。
 */
export const ChatPane: React.FC<{
  sessionId: string
  /** 表示直後に自動で1回だけ送る発言。未指定の場合は何も送らず、ユーザーの入力を待つ。 */
  initialMessage?: string
  /** チャットがAPIキー未設定エラーを受け取ったときに、設定画面へ切り替えるために呼ぶ */
  onApiKeyMissing: () => void
}> = ({ sessionId, initialMessage, onApiKeyMissing }) => {

  //#region 状態

  // 送信先はセッションごとに異なるため、セッションが変わったときだけ transport を作り直す
  const transport = React.useMemo(() => new DefaultChatTransport({
    api: `/devtool-api/sessions/${encodeURIComponent(sessionId)}/chat`,
  }), [sessionId])
  const { messages, setMessages, sendMessage, status, error } = useChat({ id: sessionId, transport })

  const [input, setInput] = React.useState('')
  const [isLoadingHistory, setIsLoadingHistory] = React.useState(true)
  const isBusy = status === 'submitted' || status === 'streaming'

  // 初回発言の送信に使う値。履歴同期エフェクトの再実行契機にはしたくないので ref で最新値を持つ
  const initialSendRef = React.useRef({ initialMessage, sendMessage })
  initialSendRef.current = { initialMessage, sendMessage }
  // 送信済みかどうか。StrictMode によるエフェクトの二重実行で同じ発言が2回送られるのを防ぐ
  const initialMessageSentRef = React.useRef(false)

  // 表示時にサーバー側に永続化された会話と同期し、その直後に初回の発言を送る
  React.useEffect(() => {
    let canceled = false
    fetch(`/devtool-api/sessions/${encodeURIComponent(sessionId)}`)
      .then(res => res.ok ? res.json() as Promise<ChatSessionDto> : null)
      .then(session => {
        if (canceled || !session) return
        setMessages(session.messages)

        const { initialMessage, sendMessage } = initialSendRef.current
        if (initialMessage && !initialMessageSentRef.current) {
          initialMessageSentRef.current = true
          sendMessage({ text: initialMessage })
        }
      })
      .finally(() => { if (!canceled) setIsLoadingHistory(false) })
    return () => { canceled = true }
  }, [sessionId, setMessages])

  //#endregion 状態

  //#region イベント

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

  //#endregion イベント

  return (
    <div className="flex-1 flex flex-col min-w-0 min-h-0">

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

  // input-streaming / input-available / approval-* のいずれも「実行中」として一括りに表示する。
  // research_* は数十秒かかることがあるため、何を調べさせているか（input）が分かるようにしておく。
  const input: unknown = part.input
  const questionText = input && typeof input === 'object' && 'question' in input && typeof input.question === 'string'
    ? input.question
    : null

  return (
    <div className="text-xs text-gray-500 border border-gray-200 rounded px-2 py-1 my-1 bg-white animate-pulse">
      {name} を実行中…
      {questionText && <div className="mt-1 text-gray-400 whitespace-pre-wrap">{questionText}</div>}
    </div>
  )
}
