import React from "react"
import { Bars2Icon, XMarkIcon } from "@heroicons/react/24/outline"
import { useDraggablePosition } from "../ui"
import { ChatPane } from "./ChatPane"

/**
 * 1つのチャットセッションを、画面上に浮かせて表示するウィンドウ。
 * 複数を同時に開いて並行して会話を進められるよう、状態は全てセッション単位で閉じている。
 * 閉じる操作は onClose を呼ぶだけで、実際に閉じるかどうかの判断はこのコンポーネントを呼ぶ側が行う。
 */
export const FloatingSession: React.FC<{
  sessionId: string
  /** 表示直後に自動で1回だけ送る発言。未指定の場合は何も送らない。 */
  initialMessage?: string
  /** 表示開始位置。複数のウィンドウが完全に重ならないよう、呼び出し側がずらして渡す。 */
  initialPosition: { x: number, y: number }
  onClose: () => void
  /** チャットがAPIキー未設定エラーを受け取ったときに、設定画面へ切り替えるために呼ぶ */
  onOpenSettings: () => void
}> = ({ sessionId, initialMessage, initialPosition, onClose, onOpenSettings }) => {

  // ウィンドウのドラッグ移動（画面内に収まるようクランプするため自身の要素を参照する）
  const containerRef = React.useRef<HTMLDivElement>(null)
  const { position, handlePointerDown } = useDraggablePosition(initialPosition, containerRef)

  return (
    <div
      ref={containerRef}
      // resize はブラウザ標準のリサイズグリップ。overflow が visible のままだと効かないため overflow-hidden と併用する
      className="fixed z-40 flex flex-col bg-white border border-gray-300 rounded shadow-lg/40 overflow-hidden resize w-[420px] h-[520px] min-w-[280px] min-h-[240px]"
      style={{ left: position.x, top: position.y }}
    >
      {/* ヘッダ（エージェントの姿・ドラッグハンドル・閉じるボタン） */}
      <div className="flex items-center gap-1 px-1 py-1 border-b border-gray-200 bg-gray-50">

        {/* エージェントの姿 */}
        <img src="/agent-icon.png" alt="" className="w-6 h-6 shrink-0 select-none" draggable={false} />

        {/* ドラッグハンドル */}
        <div
          onPointerDown={handlePointerDown}
          className="flex-1 flex items-center gap-1 px-1 cursor-move text-gray-400 select-none min-w-0"
          title="ドラッグして移動"
        >
          <Bars2Icon className="w-4 h-4 shrink-0" />
        </div>

        {/* 閉じる */}
        <button
          type="button"
          onClick={onClose}
          className="p-1 text-gray-500 hover:text-gray-800 shrink-0"
          title="閉じる"
        >
          <XMarkIcon className="w-4 h-4" />
        </button>
      </div>

      {/* チャット本体 */}
      <ChatPane sessionId={sessionId} initialMessage={initialMessage} onApiKeyMissing={onOpenSettings} />
    </div>
  )
}
