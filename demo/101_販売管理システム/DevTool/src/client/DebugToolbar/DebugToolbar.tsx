import React from "react"
import { Bars2Icon, PlayIcon, StopIcon, ArrowPathIcon, Cog6ToothIcon, ChatBubbleLeftRightIcon, PaperAirplaneIcon, DocumentTextIcon } from "@heroicons/react/24/outline"
import { SplitButton, useDraggablePosition } from "../ui"
import { ServerLogWindow, type useDevToolState } from "../Preview"
import { SettingsModal } from "../Settings"
import { AgentPanel, FloatingSession, useChatSessions } from "../Agent"
import { PREVIEW_PROCESS_NAMES } from "../../shared/devtool-api"

/** フローティングウィンドウを開くたびに表示位置をずらす量。完全に重なって見失うのを防ぐ。 */
const FLOATING_OFFSET = 32

/**
 * デバッグ実行プロセス群の操作と、AIエージェントとの会話の開始を担う、
 * ドラッグで移動できるフローティングツールバー。
 * VSCodeのデバッグツールバーに相当する。
 * デバッグ実行プロセスの状態はAppが保持し、propsとして受け取る。
 */
export function DebugToolbar({ preview }: {
  preview: ReturnType<typeof useDevToolState>
}) {

  //#region 状態

  const { processes, start, stop, restart, isBusy } = preview
  // ツールバー自体のドラッグ移動（画面内に収まるようクランプするため自身の要素を参照する）
  const containerRef = React.useRef<HTMLDivElement>(null)
  const { position, handlePointerDown } = useDraggablePosition({ x: 16, y: 16 }, containerRef)
  // 開いているオーバーレイ（設定モーダル・エージェントパネル）。
  // 単一の状態で持つことで、2つのz-50モーダルが同時に開いて重なることを構造的に防ぐ。
  const [openPanel, setOpenPanel] = React.useState<'settings' | 'agent' | null>(null)
  // サーバーログ窓の開閉。チャットのストリーミング中でも見られるよう、上記の openPanel（z-50モーダルの排他用）とは別に持つ。
  const [showServerLog, setShowServerLog] = React.useState(false)

  // チャットセッションの一覧と増減
  const chatSessions = useChatSessions()
  // 画面上に浮かべているセッション。同じセッションは1つまでで、複数のセッションを同時に浮かべられる。
  const [floatings, setFloatings] = React.useState<{ id: string, initialMessage?: string }[]>([])
  const [newChatText, setNewChatText] = React.useState('')

  const processByName = React.useMemo(() => new Map(processes.map(p => [p.name, p])), [processes])
  const isAnyRunning = processes.some(p => p.isRunning)

  //#endregion 状態

  //#region イベント

  // 入力されたテキストで新しいセッションを起こし、その最初の発言としてフローティングウィンドウへ引き渡す
  const handleStartChat = async () => {
    const text = newChatText.trim()
    if (!text) return
    const created = await chatSessions.create()
    if (!created) return
    setNewChatText('')
    setFloatings(current => [...current, { id: created.id, initialMessage: text }])
  }

  const formRef = React.useRef<HTMLFormElement>(null)
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      formRef.current?.requestSubmit()
    }
  }

  // 既存セッションのフローティングUIを開く
  const handleOpenFloating = (sessionId: string) => {
    setOpenPanel(null)
    setFloatings(current => {
      return current.some(f => f.id === sessionId)
        ? current
        : [...current, { id: sessionId }]
    })
  }

  //#endregion イベント

  return (
    <>
      <div
        ref={containerRef}
        className="fixed z-40 flex flex-col gap-1 bg-white border border-gray-300 rounded shadow-lg/40 px-1 py-1"
        style={{ left: position.x, top: position.y }}
      >

        {/* 操作ボタンの列 */}
        <div className="flex items-center gap-1">

          {/* ドラッグハンドル */}
          <div
            onPointerDown={handlePointerDown}
            className="self-stretch flex items-center px-1 cursor-move text-gray-400 select-none"
            title="ドラッグして移動"
          >
            <Bars2Icon className="w-4 h-4" />
          </div>

          {/* 開始・終了 */}
          <SplitButton
            icon={isAnyRunning ? StopIcon : PlayIcon}
            onClick={() => (isAnyRunning ? stop() : start())}
            loading={isBusy}
            options={PREVIEW_PROCESS_NAMES.map(name => {
              const running = processByName.get(name)?.isRunning ?? false
              return {
                key: name,
                label: `${name} を${running ? '停止' : '開始'}`,
                onClick: () => (running ? stop(name) : start(name)),
              }
            })}
          >
            {isAnyRunning ? '終了' : '開始'}
          </SplitButton>

          {/* 再起動 */}
          <SplitButton
            icon={ArrowPathIcon}
            onClick={() => restart()}
            loading={isBusy}
            options={PREVIEW_PROCESS_NAMES.map(name => ({
              key: name,
              label: `${name} を再起動`,
              onClick: () => restart(name),
            }))}
          >
            再起動
          </SplitButton>

          {/* セッション一覧・変更計画 */}
          <SplitButton
            icon={ChatBubbleLeftRightIcon}
            onClick={() => setOpenPanel('agent')}
          >
            セッション一覧
          </SplitButton>

          {/* DevToolサーバーのログ */}
          <button
            type="button"
            onClick={() => setShowServerLog(current => !current)}
            className={`p-1 rounded cursor-pointer ${showServerLog ? 'text-sky-700 bg-sky-50' : 'text-gray-600 hover:bg-gray-100'}`}
            title="DevToolサーバーのログ"
          >
            <DocumentTextIcon className="w-5 h-5" />
          </button>

          {/* 設定 */}
          <button
            type="button"
            onClick={() => setOpenPanel('settings')}
            className="p-1 text-gray-600 hover:bg-gray-100 rounded cursor-pointer"
            title="設定"
          >
            <Cog6ToothIcon className="w-5 h-5" />
          </button>
        </div>

        {/* 新しい会話の開始 */}
        <form
          ref={formRef}
          onSubmit={e => { e.preventDefault(); handleStartChat() }}
          className="flex gap-1 items-end"
        >
          <textarea
            value={newChatText}
            onChange={e => setNewChatText(e.target.value)}
            onKeyDown={handleKeyDown}
            spellCheck="false"
            placeholder="AIに話しかける (Ctrl + Enter)"
            className="w-72 border border-gray-300 rounded px-2 py-1 text-sm max-h-40 resize-none field-sizing-content"
          />
          <button
            type="submit"
            disabled={!newChatText.trim()}
            className="p-1.5 text-gray-600 hover:bg-gray-100 rounded cursor-pointer disabled:opacity-50"
            title="この内容で新しい会話を始める"
          >
            <PaperAirplaneIcon className="w-5 h-5" />
          </button>
        </form>
      </div>

      <SettingsModal
        open={openPanel === 'settings'}
        onClose={() => setOpenPanel(null)}
        preview={preview}
      />

      <AgentPanel
        open={openPanel === 'agent'}
        sessions={chatSessions}
        onClose={() => setOpenPanel(null)}
        onOpenFloating={handleOpenFloating}
        onOpenSettings={() => setOpenPanel('settings')}
      />

      {/* DevToolサーバーのログ。設定モーダルの中に置くとチャットのストリーミング中は見られなくなるため、独立したフローティング窓にする */}
      {showServerLog && (
        <ServerLogWindow serverLog={preview.serverLog} onClose={() => setShowServerLog(false)} />
      )}

      {/* 進行中の会話。開いた順に表示位置をずらす（位置はマウント時にのみ効く） */}
      {floatings.map((floating, ix) => (
        <FloatingSession
          key={floating.id}
          sessionId={floating.id}
          initialMessage={floating.initialMessage}
          initialPosition={{ x: 80 + ix * FLOATING_OFFSET, y: 80 + ix * FLOATING_OFFSET }}
          onClose={() => setFloatings(current => current.filter(f => f.id !== floating.id))}
          onOpenSettings={() => setOpenPanel('settings')}
        />
      ))}
    </>
  )
}
