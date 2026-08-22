import React from "react"
import { Bars2Icon, PlayIcon, StopIcon, ArrowPathIcon, Cog6ToothIcon } from "@heroicons/react/24/outline"
import { SplitButton } from "../SplitButton"
import { useDraggablePosition } from "./useDraggablePosition"
import { usePreviewState } from "./usePreviewState"
import { SettingsModal } from "./SettingsModal"

/**
 * デバッグ実行プロセス群を操作するための、ドラッグで移動できるフローティングツールバー。
 * VSCodeのデバッグツールバーに相当する。
 *
 * デバッグ対象のプロセス名（vite, dotnet）は DevTool.Server 側の定義に合わせた固定値であり、
 * DevTool.Server が動的なプロセス構成を返すようになったら、そちらから取得する形に改める。
 */
const PROCESS_NAMES = ['vite', 'dotnet'] as const

export const DebugToolbar = () => {
  // デバッグ実行プロセスの状態・ログ取得とその操作
  const { processes, logs, start, stop, restart, isBusy } = usePreviewState()
  // ツールバー自体のドラッグ移動
  const { position, handlePointerDown } = useDraggablePosition({ x: 16, y: 16 })
  // 設定モーダル（プロセス状態・ログ表示）の開閉
  const [settingsOpen, setSettingsOpen] = React.useState(false)

  const processByName = React.useMemo(() => new Map(processes.map(p => [p.name, p])), [processes])
  const isAnyRunning = processes.some(p => p.isRunning)

  return (
    <>
      <div
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
          options={PROCESS_NAMES.map(name => {
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
          options={PROCESS_NAMES.map(name => ({
            key: name,
            label: `${name} を再起動`,
            onClick: () => restart(name),
          }))}
        >
          再起動
        </SplitButton>

        {/* 設定（プロセス状態・ログの表示） */}
        <button
          type="button"
          onClick={() => setSettingsOpen(true)}
          className="p-1.5 text-gray-600 hover:bg-gray-100 rounded"
          title="デバッグ実行プロセスの状態"
        >
          <Cog6ToothIcon className="w-4 h-4" />
        </button>
      </div>

      <SettingsModal
        open={settingsOpen}
        onClose={() => setSettingsOpen(false)}
        processes={processes}
        logs={logs}
      />
    </>
  )
}
