import React from "react"
import type { ChatSessionDto, ChatSessionSummary } from "../../shared/devtool-api"

/**
 * チャットセッションの一覧の保持と、一覧を増減させる操作をまとめたフック。
 * 一覧はサーバー側（.nijo/sessions 配下）が正であり、作成・削除の後は必ず取得し直す。
 * 個々のセッションの会話の中身はこのフックの責務ではない。
 */
export function useChatSessions() {
  const [sessions, setSessions] = React.useState<ChatSessionSummary[]>([])
  const [isLoading, setIsLoading] = React.useState(false)

  const reload = React.useCallback(async () => {
    setIsLoading(true)
    try {
      const res = await fetch('/devtool-api/sessions')
      if (res.ok) setSessions(await res.json())
    } finally {
      setIsLoading(false)
    }
  }, [])

  /** 空のセッションを新規作成する。作成できなかった場合は undefined を返す。 */
  const create = React.useCallback(async (): Promise<ChatSessionDto | undefined> => {
    const res = await fetch('/devtool-api/sessions', { method: 'POST' })
    if (!res.ok) return undefined
    const created: ChatSessionDto = await res.json()
    await reload()
    return created
  }, [reload])

  const remove = React.useCallback(async (id: string) => {
    await fetch(`/devtool-api/sessions/${encodeURIComponent(id)}`, { method: 'DELETE' })
    await reload()
  }, [reload])

  // 初回表示時にサーバー側の一覧と同期する
  React.useEffect(() => {
    reload()
  }, [reload])

  return { sessions, isLoading, reload, create, remove }
}
