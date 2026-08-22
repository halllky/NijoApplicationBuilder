import React from "react"
import { XMarkIcon } from "@heroicons/react/24/outline"
import { useChangePlans } from "./useChangePlans"
import { ChangePlanList } from "./ChangePlanList"
import { ChatPane } from "./ChatPane"

/**
 * 要件ヒアリングのチャットと、これまでに積まれた変更計画の一覧を並べて表示するモーダル。
 * 変更計画は将来的にコーディングエージェントの入力になる予定だが、
 * この画面では一覧の表示のみを行い、登録・実装開始の操作はまだ持たない。
 * 閉じる操作（シェードクリック・閉じるボタン）は onClose を呼ぶだけで、
 * 実際に閉じるかどうかの判断はこのコンポーネントを呼ぶ側が行う。
 */
export const AgentPanel = ({ open, onClose, onOpenSettings }: {
  open: boolean
  onClose: () => void
  /** チャットがAPIキー未設定エラーを受け取ったときに、設定画面へ切り替えるために呼ぶ */
  onOpenSettings: () => void
}) => {
  // 変更計画の一覧取得
  const { plans, isLoading } = useChangePlans(open)

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">

      {/* シェード */}
      <div className="absolute inset-0 bg-black/25" onClick={onClose} />

      {/* パネル */}
      <div className="relative bg-white rounded shadow-lg w-[960px] max-w-[95vw] h-[640px] max-h-[85vh] flex flex-col">

        {/* ヘッダ */}
        <div className="flex items-center justify-between px-4 py-2 border-b border-gray-200">
          <h2 className="text-base font-bold">チャット・変更計画</h2>
          <button type="button" onClick={onClose} className="p-1 text-gray-500 hover:text-gray-800">
            <XMarkIcon className="w-5 h-5" />
          </button>
        </div>

        {/* 本体（左：変更計画一覧、右：チャット） */}
        <div className="flex-1 flex min-h-0">
          <ChangePlanList plans={plans} isLoading={isLoading} />
          <ChatPane onApiKeyMissing={onOpenSettings} />
        </div>
      </div>
    </div>
  )
}
