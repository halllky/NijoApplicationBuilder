import * as React from "react"
import { useDemoMode } from "./DemoModeProvider"

/**
 * AIチャット + プロセスログを表示するフローティングパネル。
 * 画面右下のトグルボタンで開閉する。isDemoMode=false のときは何も表示しない。
 */
export const DemoChatPanel: React.FC = () => {
  const { isDemoMode, chatMessages, streamingText, processLogs, isLockedByOther, sendChat, cancelChat } = useDemoMode()
  const [open, setOpen] = React.useState(false)
  const [tab, setTab] = React.useState<"chat" | "logs">("chat")
  const [input, setInput] = React.useState("")
  const [sending, setSending] = React.useState(false)
  const [error, setError] = React.useState<string>()

  if (!isDemoMode) return null

  const handleSend = async () => {
    const message = input.trim()
    if (!message || sending || isLockedByOther) return
    setSending(true)
    setError(undefined)
    const result = await sendChat(message)
    if (result.ok) {
      setInput("")
    } else {
      setError(result.error)
    }
    setSending(false)
  }

  if (!open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="fixed bottom-4 right-4 z-40 rounded-full bg-indigo-600 text-white w-14 h-14 shadow-lg hover:bg-indigo-700"
      >
        AI
      </button>
    )
  }

  return (
    <div className="fixed bottom-4 right-4 z-40 w-96 h-[32rem] bg-white border border-gray-300 rounded shadow-xl flex flex-col">
      <div className="flex items-center justify-between px-3 py-2 border-b">
        <div className="flex gap-2 text-sm">
          <button
            type="button"
            className={tab === "chat" ? "font-semibold" : "text-gray-400"}
            onClick={() => setTab("chat")}
          >
            チャット
          </button>
          <button
            type="button"
            className={tab === "logs" ? "font-semibold" : "text-gray-400"}
            onClick={() => setTab("logs")}
          >
            ログ
          </button>
        </div>
        <button type="button" onClick={() => setOpen(false)} className="text-gray-400 hover:text-gray-700">×</button>
      </div>

      {tab === "chat" ? (
        <>
          <div className="flex-1 overflow-y-auto p-2 space-y-2 text-sm">
            {chatMessages.map((m, i) => (
              <div key={i} className={m.role === "user" ? "text-right" : "text-left"}>
                <span className={`inline-block px-2 py-1 rounded ${m.role === "user" ? "bg-indigo-100" : "bg-gray-100"}`}>
                  {m.content}
                </span>
              </div>
            ))}
            {streamingText && (
              <div className="text-left">
                <span className="inline-block px-2 py-1 rounded bg-gray-100 text-gray-500">{streamingText}</span>
              </div>
            )}
          </div>
          <div className="p-2 border-t">
            {isLockedByOther && <p className="text-xs text-amber-700 mb-1">他のユーザーまたはAIが編集中です</p>}
            {error && <p className="text-xs text-red-600 mb-1">{error}</p>}
            <div className="flex gap-1">
              <input
                type="text"
                value={input}
                onChange={e => setInput(e.target.value)}
                onKeyDown={e => { if (e.key === "Enter") handleSend() }}
                disabled={isLockedByOther || sending}
                maxLength={4000}
                placeholder="AIに指示を入力..."
                className="flex-1 border rounded px-2 py-1 text-sm disabled:bg-gray-100"
              />
              <button
                type="button"
                onClick={handleSend}
                disabled={isLockedByOther || sending}
                className="px-2 py-1 text-sm rounded bg-indigo-600 text-white disabled:opacity-50"
              >
                送信
              </button>
              <button
                type="button"
                onClick={cancelChat}
                className="px-2 py-1 text-sm rounded border"
              >
                中断
              </button>
            </div>
          </div>
        </>
      ) : (
        <div className="flex-1 overflow-y-auto p-2 font-mono text-xs space-y-0.5">
          {processLogs.map((l, i) => (
            <div key={i} className="whitespace-pre-wrap">
              <span className="text-gray-400">[{l.stream}]</span> {l.line}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
