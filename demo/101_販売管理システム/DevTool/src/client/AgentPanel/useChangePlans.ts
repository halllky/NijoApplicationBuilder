import React from "react"
import type { ChangePlanSummary } from "../../shared/devtool-api"

/**
 * 変更計画の一覧を取得する。プランの登録はこのフックの責務ではなく、
 * サーバー側で .nijo/plans に書き込まれたものを読み取るだけの読み取り専用フック。
 */
export function useChangePlans(open: boolean) {
  const [plans, setPlans] = React.useState<ChangePlanSummary[]>([])
  const [isLoading, setIsLoading] = React.useState(false)

  const reload = React.useCallback(async () => {
    setIsLoading(true)
    try {
      const res = await fetch('/devtool-api/plans')
      if (res.ok) setPlans(await res.json())
    } finally {
      setIsLoading(false)
    }
  }, [])

  // パネルが開かれるたびにサーバー側の最新の一覧と同期する
  React.useEffect(() => {
    if (open) reload()
  }, [open, reload])

  return { plans, isLoading, reload }
}
