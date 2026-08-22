import React from "react"
import { Modal } from "../ui"
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

  return (
    <Modal open={open} onClose={onClose} title="チャット・変更計画" panelClassName="w-[960px] max-w-[95vw] h-[640px] max-h-[85vh]">

      {/* 本体（左：変更計画一覧、右：チャット） */}
      <div className="flex-1 flex min-h-0">
        <ChangePlanList plans={plans} isLoading={isLoading} />
        <ChatPane onApiKeyMissing={onOpenSettings} />
      </div>
    </Modal>
  )
}
