import React from "react"
import { Bars2Icon, PlayIcon, StopIcon, ArrowPathIcon, Cog6ToothIcon, ChatBubbleLeftRightIcon } from "@heroicons/react/24/outline"
import { SplitButton } from "../ui"
import { useDraggablePosition } from "./useDraggablePosition"
import type { usePreviewState } from "../Preview"
import { SettingsModal } from "../Settings"
import { AgentPanel } from "../AgentPanel"
import { PREVIEW_PROCESS_NAMES } from "../../shared/devtool-api"

/**
 * デバッグ実行プロセス群を操作するための、ドラッグで移動できるフローティングツールバー。
 * VSCodeのデバッグツールバーに相当する。
 * デバッグ実行プロセスの状態はAppが保持し、propsとして受け取る。
 */
export function DebugToolbar({ preview }: {
  preview: ReturnType<typeof usePreviewState>
}) {
  const { processes, logs, start, stop, restart, isBusy } = preview
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
        className="fixed z-40 flex items-center gap-1 bg-white border border-gray-300 rounded shadow-lg/40 px-1 py-1"
        style={{ left: position.x, top: position.y }}
      >
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

        {/* チャット（要件ヒアリング・変更計画一覧） */}
        <SplitButton
          icon={ChatBubbleLeftRightIcon}
          onClick={() => setOpenPanel('agent')}
        >
          チャット・変更計画一覧
        </SplitButton>

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

      <SettingsModal
        open={openPanel === 'settings'}
        onClose={() => setOpenPanel(null)}
        preview={preview}
      />

      <AgentPanel
        open={openPanel === 'agent'}
        onClose={() => setOpenPanel(null)}
        onOpenSettings={() => setOpenPanel('settings')}
      />
    </>
  )
}
