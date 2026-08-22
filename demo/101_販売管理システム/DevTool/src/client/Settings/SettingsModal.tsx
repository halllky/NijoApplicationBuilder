import React from "react"
import { XMarkIcon } from "@heroicons/react/24/outline"
import type { usePreviewState } from "../Preview"
import { ProcessList } from "./ProcessList"
import { AgentSettings } from "./AgentSettings"

/**
 * 設定画面。
 *
 * 閉じる操作（シェードクリック・閉じるボタン）は onClose を呼ぶだけで、
 * 実際に閉じるかどうかの判断はこのコンポーネントを呼ぶ側が行う。
 */
export const SettingsModal = ({ open, onClose, preview }: {
  open: boolean
  onClose: () => void
  preview: ReturnType<typeof usePreviewState>
}) => {
  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">

      {/* シェード */}
      <div className="absolute inset-0 bg-black/25" onClick={onClose} />

      {/* パネル */}
      <div className="relative bg-white rounded shadow-lg w-[720px] max-w-[90vw] max-h-[85vh] flex flex-col">

        {/* ヘッダ */}
        <div className="flex items-center justify-between px-4 py-2 border-b border-gray-200">
          <h2 className="text-base font-bold">設定</h2>
          <button type="button" onClick={onClose} className="p-1 text-gray-500 hover:text-gray-800">
            <XMarkIcon className="w-5 h-5" />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-6">

          {/* プロセス一覧・操作・ログ */}
          <ProcessList preview={preview} />

          {/* エージェント設定（APIキー・モデル） */}
          <AgentSettings />
        </div>
      </div>
    </div>
  )
}
