import React from "react"
import { Bars2Icon, PlayIcon, StopIcon, ArrowPathIcon, Cog6ToothIcon, ChatBubbleLeftRightIcon } from "@heroicons/react/24/outline"
import { SplitButton } from "../SplitButton"
import { useDraggablePosition } from "./useDraggablePosition"
import { usePreviewState } from "./usePreviewState"
import { SettingsModal } from "./SettingsModal"
import { AgentPanel } from "../AgentPanel"
import { PREVIEW_PROCESS_NAMES } from "../../shared/devtool-api"

/**
 * デバッグ実行プロセス群を操作するための、ドラッグで移動できるフローティングツールバー。
 * VSCodeのデバッグツールバーに相当する。
 */
export function DebugToolbar() {
  // デバッグ実行プロセスの状態・ログ取得とその操作
  const { processes, logs, start, stop, restart, isBusy } = usePreviewState()
  // ツールバー自体のドラッグ移動（画面内に収まるようクランプするため自身の要素を参照する）
  const containerRef = React.useRef<HTMLDivElement>(null)
  const { position, handlePointerDown } = useDraggablePosition({ x: 16, y: 16 }, containerRef)
  // 開いているオーバーレイ（設定モーダル・エージェントパネル）。
  // 単一の状態で持つことで、2つのz-50モーダルが同時に開いて重なることを構造的に防ぐ。
  const [openPanel, setOpenPanel] = React.useState<'settings' | 'agent' | null>(null)

  const processByName = React.useMemo(() => new Map(processes.map(p => [p.name, p])), [processes])
  const isAnyRunning = processes.some(p => p.isRunning)

  return (
    <>
      <div
        ref={containerRef}
        className="fixed z-40 flex items-center gap-1 bg-white/95 border border-gray-300 rounded shadow-lg px-1 py-1"
        style={{ left: position.x, top: position.y }}
      >
        {/* ドラッグハンドル */}
        <div
          onPointerDown={handlePointerDown}
          className="px-1 cursor-move text-gray-400 select-none"
          title="ドラッグして移動"
        >
          <Bars2Icon className="w-4 h-4" />
        </div>

        {/* 開始・終了（全プロセス一括操作。ドロップダウンから個別にも操作できる） */}
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

        {/* 再起動（全プロセス一括操作。ドロップダウンから個別にも操作できる） */}
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

        {/* チャット（要件ヒアリング・変更計画一覧） */}
        <button
          type="button"
          onClick={() => setOpenPanel('agent')}
          className="p-1.5 text-gray-600 hover:bg-gray-100 rounded"
          title="チャット・変更計画一覧"
        >
          <ChatBubbleLeftRightIcon className="w-4 h-4" />
        </button>

        {/* 設定（プロセス状態・ログの表示、APIキー・モデル設定） */}
        <button
          type="button"
          onClick={() => setOpenPanel('settings')}
          className="p-1.5 text-gray-600 hover:bg-gray-100 rounded"
          title="デバッグ実行プロセスの状態・設定"
        >
          <Cog6ToothIcon className="w-4 h-4" />
        </button>
      </div>

      <SettingsModal
        open={openPanel === 'settings'}
        onClose={() => setOpenPanel(null)}
        processes={processes}
        logs={logs}
      />

      <AgentPanel
        open={openPanel === 'agent'}
        onClose={() => setOpenPanel(null)}
        onOpenSettings={() => setOpenPanel('settings')}
      />
    </>
  )
}
