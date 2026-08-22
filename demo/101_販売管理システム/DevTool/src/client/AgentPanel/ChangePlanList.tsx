import React from "react"
import type { ChangePlanDetail, ChangePlanSummary } from "../../shared/devtool-api"

/**
 * 変更計画の一覧。行をクリックすると本文を展開して表示する。
 * 一覧の取得元は .nijo/plans 配下の Markdown ファイルで、登録・編集はこの画面では行わない。
 */
export const ChangePlanList: React.FC<{ plans: ChangePlanSummary[], isLoading: boolean }> = ({ plans, isLoading }) => {
  const [expandedId, setExpandedId] = React.useState<string>()

  return (
    <div className="w-[280px] shrink-0 border-r border-gray-200 overflow-y-auto p-3 flex flex-col gap-2">
      <h3 className="text-sm font-bold">変更計画</h3>

      {isLoading && plans.length === 0 && (
        <p className="text-xs text-gray-500">読み込み中...</p>
      )}
      {!isLoading && plans.length === 0 && (
        <p className="text-xs text-gray-500">変更計画はまだありません。</p>
      )}

      {plans.map(plan => (
        <PlanRow
          key={plan.id}
          plan={plan}
          expanded={expandedId === plan.id}
          onToggle={() => setExpandedId(current => current === plan.id ? undefined : plan.id)}
        />
      ))}
    </div>
  )
}

/** 1件の変更計画。クリックで展開し、展開時のみ本文をサーバーから取得する。 */
const PlanRow: React.FC<{ plan: ChangePlanSummary, expanded: boolean, onToggle: () => void }> = ({ plan, expanded, onToggle }) => {
  const [detail, setDetail] = React.useState<ChangePlanDetail>()

  // 展開されたときにだけ本文を取得する（一覧表示の時点では本文を持たないため）
  React.useEffect(() => {
    if (!expanded || detail) return
    fetch(`/devtool-api/plans/${encodeURIComponent(plan.id)}`)
      .then(res => res.ok ? res.json() : undefined)
      .then(data => { if (data) setDetail(data) })
  }, [expanded, detail, plan.id])

  return (
    <div className="border border-gray-200 rounded">
      <button
        type="button"
        onClick={onToggle}
        className="w-full text-left px-2 py-1.5 flex flex-col gap-0.5 hover:bg-gray-50"
      >
        <span className="text-sm font-medium truncate">{plan.title}</span>
        <span className="text-xs text-gray-500">{plan.status}{plan.createdAt ? ` ・ ${plan.createdAt}` : ''}</span>
      </button>
      {expanded && (
        <div className="px-2 pb-2 text-xs text-gray-700 whitespace-pre-wrap border-t border-gray-100 pt-2">
          {detail ? detail.body : '読み込み中...'}
        </div>
      )}
    </div>
  )
}
