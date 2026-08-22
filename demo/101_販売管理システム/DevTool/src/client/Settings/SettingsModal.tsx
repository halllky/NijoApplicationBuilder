import React from "react"
import { Modal } from "../ui"
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
  return (
    <Modal open={open} onClose={onClose} title="設定" panelClassName="w-[720px] max-w-[90vw] max-h-[85vh]">
      <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-6">

        {/* プロセス一覧・操作・ログ */}
        <ProcessList preview={preview} />

        {/* エージェント設定（APIキー・モデル） */}
        <AgentSettings />
      </div>
    </Modal>
  )
}
