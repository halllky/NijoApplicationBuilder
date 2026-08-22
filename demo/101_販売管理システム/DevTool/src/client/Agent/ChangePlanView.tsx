import React from "react"
import type { ChangePlan } from "../../shared/devtool-api"

/**
 * 1つのセッションでの対話の成果物である変更計画の表示。
 * 変更計画は将来的にコーディングエージェントの入力になる予定だが、
 * この画面では表示のみを行い、登録・編集・実装開始の操作はまだ持たない。
 */
export const ChangePlanView: React.FC<{ changePlan: ChangePlan | null, isLoading: boolean }> = ({ changePlan, isLoading }) => {

  return (
    <div className="w-[320px] shrink-0 border-l border-gray-200 overflow-y-auto p-3 flex flex-col gap-2">
      <h3 className="text-sm font-bold">変更計画</h3>

      {isLoading && (
        <p className="text-xs text-gray-500">読み込み中...</p>
      )}
      {!isLoading && !changePlan && (
        <p className="text-xs text-gray-500">変更計画はまだありません。</p>
      )}

      {/* 変更計画の見出し・状態・本文 */}
      {changePlan && (
        <>
          <div className="flex flex-col gap-0.5">
            <span className="text-sm font-medium">{changePlan.title}</span>
            <span className="text-xs text-gray-500">{changePlan.status}</span>
          </div>
          <p className="text-xs text-gray-700 whitespace-pre-wrap">{changePlan.body}</p>
        </>
      )}
    </div>
  )
}
