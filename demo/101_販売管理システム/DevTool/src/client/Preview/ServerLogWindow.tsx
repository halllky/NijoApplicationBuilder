import React from "react"
import { Bars2Icon, XMarkIcon } from "@heroicons/react/24/outline"
import { LogPane, useDraggablePosition } from "../ui"

/** 表示開始位置。設定モーダル・チャットウィンドウとは別レイヤーの読み物なので、決め打ちで右下寄りに出す。 */
const INITIAL_POSITION = { x: 96, y: 96 }

/**
 * DevToolサーバー自身のログを表示する、画面上に浮かべるウィンドウ。
 * 設定モーダル（z-50）の中に置くとチャットのストリーミング中は開けなくなり、
 * サブエージェントの入出力・ツール呼び出しの妥当性を最も見たい瞬間に見られなくなるため、
 * チャットウィンドウ（{@link FloatingSession}）と同じ z-40 のフローティングウィンドウとして独立させている。
 * 閉じる操作は onClose を呼ぶだけで、実際に閉じるかどうかの判断はこのコンポーネントを呼ぶ側が行う。
 */
export const ServerLogWindow: React.FC<{ serverLog: string, onClose: () => void }> = ({ serverLog, onClose }) => {
  // ウィンドウのドラッグ移動（画面内に収まるようクランプするため自身の要素を参照する）
  const containerRef = React.useRef<HTMLDivElement>(null)
  const { position, handlePointerDown } = useDraggablePosition(INITIAL_POSITION, containerRef)

  return (
    <div
      ref={containerRef}
      // resize はブラウザ標準のリサイズグリップ。overflow が visible のままだと効かないため overflow-hidden と併用する
      className="fixed z-40 flex flex-col bg-white border border-gray-300 rounded shadow-lg/40 overflow-hidden resize w-[560px] h-[360px] min-w-[320px] min-h-[160px]"
      style={{ left: position.x, top: position.y }}
    >
      {/* ヘッダ（見出し・ドラッグハンドル・閉じるボタン） */}
      <div className="flex items-center gap-1 px-1 py-1 border-b border-gray-200 bg-gray-50">
        <span className="px-1 text-sm font-bold text-gray-700 shrink-0">DevToolサーバー</span>

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

      {/* ログ本体 */}
      <LogPane text={serverLog} className="flex-1" />
    </div>
  )
}
