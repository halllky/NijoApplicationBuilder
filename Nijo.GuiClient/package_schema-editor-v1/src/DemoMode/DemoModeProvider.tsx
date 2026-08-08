import * as React from "react"
import * as signalR from "@microsoft/signalr"
import { SERVER_DOMAIN } from "../main"
import { demoFetchHeaders, fetchDemoStatus, getDemoClientId } from "./clientId"
import { DemoAppStatus, DemoChatMessage, DemoLockInfo } from "./types"

type DemoModeContextValue = {
  isDemoMode: boolean
  demoUrl: string
  lock: DemoLockInfo | null
  isLockedByOther: boolean
  isLockedByMe: boolean
  demoAppStatus: DemoAppStatus
  chatMessages: DemoChatMessage[]
  streamingText: string
  processLogs: { stream: string, line: string }[]
  reloadReason: string | null
  sendChat: (message: string) => Promise<{ ok: boolean, error?: string }>
  cancelChat: () => Promise<void>
  resetDemo: () => Promise<{ ok: boolean, error?: string }>
}

const DemoModeContext = React.createContext<DemoModeContextValue | null>(null)

const NOT_DEMO_MODE: DemoModeContextValue = {
  isDemoMode: false,
  demoUrl: "",
  lock: null,
  isLockedByOther: false,
  isLockedByMe: false,
  demoAppStatus: "stopped",
  chatMessages: [],
  streamingText: "",
  processLogs: [],
  reloadReason: null,
  sendChat: async () => ({ ok: false, error: "デモモードではありません" }),
  cancelChat: async () => { },
  resetDemo: async () => ({ ok: false, error: "デモモードではありません" }),
}

const MAX_PROCESS_LOGS = 500

/**
 * 共有デモサイトモードのときだけ有効になる機能(排他ロック・強制リロード・
 * AIチャット・デモ101稼働状態)を提供するプロバイダー。
 * 通常のローカル利用時は /api/demo/status が存在しない(404 or 非JSON)ため、
 * isDemoMode=false のまま何もしない。
 */
export const DemoModeProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isDemoMode, setIsDemoMode] = React.useState(false)
  const [demoUrl, setDemoUrl] = React.useState("")
  const [lock, setLock] = React.useState<DemoLockInfo | null>(null)
  const [demoAppStatus, setDemoAppStatus] = React.useState<DemoAppStatus>("stopped")
  const [chatMessages, setChatMessages] = React.useState<DemoChatMessage[]>([])
  const [streamingText, setStreamingText] = React.useState("")
  const [processLogs, setProcessLogs] = React.useState<{ stream: string, line: string }[]>([])
  const [reloadReason, setReloadReason] = React.useState<string | null>(null)

  const myClientId = React.useMemo(() => getDemoClientId(), [])

  // 初回マウント時にデモモードかどうかを判定する
  React.useEffect(() => {
    let cancelled = false

    const detect = async () => {
      const status = await fetchDemoStatus(SERVER_DOMAIN)
      if (cancelled || !status) return

      setIsDemoMode(true)
      setDemoUrl(status.demoUrl)
      setLock(status.lockInfo)
      setDemoAppStatus(status.demoAppStatus)
      setChatMessages(status.chatHistory)
    }
    detect()

    return () => { cancelled = true }
  }, [])

  // デモモードのときだけSignalR接続を張る
  React.useEffect(() => {
    if (!isDemoMode) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${SERVER_DOMAIN}/api/demo/hub?clientId=${encodeURIComponent(myClientId)}`, {
        // negotiate(HTTP) → WebSocket の2段階だと、ロードバランサ配下で
        // 2つのリクエストが別サーバーに振り分けられたときに
        // "connection ID is not present on the server" で接続失敗する。
        // 最初からWebSocket一本で接続してこの問題を回避する。
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .build()

    connection.on("LockStateChanged", (lockInfo: DemoLockInfo | null) => setLock(lockInfo))
    connection.on("DemoAppStatusChanged", (status: DemoAppStatus) => setDemoAppStatus(status))
    connection.on("ProcessOutput", (stream: string, line: string) => {
      setProcessLogs(prev => {
        const next = [...prev, { stream, line }]
        return next.length > MAX_PROCESS_LOGS ? next.slice(next.length - MAX_PROCESS_LOGS) : next
      })
    })
    connection.on("ChatMessageAppended", (message: DemoChatMessage) => {
      setChatMessages(prev => [...prev, message])
      if (message.role === "assistant") setStreamingText("")
    })
    connection.on("ChatStreamChunk", (chunk: string) => {
      setStreamingText(prev => prev + chunk)
    })
    connection.on("ForceReload", (reason: string) => {
      setReloadReason(reason)
      window.setTimeout(() => window.location.reload(), 1500)
    })

    connection.start().catch(err => console.error("SignalR接続に失敗しました", err))

    return () => {
      connection.stop()
    }
  }, [isDemoMode, myClientId])

  const sendChat = React.useCallback(async (message: string) => {
    const response = await fetch(`${SERVER_DOMAIN}/api/demo/chat`, {
      method: "POST",
      headers: { "Content-Type": "application/json", ...demoFetchHeaders() },
      body: JSON.stringify({ message }),
    })
    if (response.status === 423) {
      const body = await response.json().catch(() => null)
      return { ok: false, error: body?.message ?? "他のユーザーまたはAIが編集中です" }
    }
    if (!response.ok) {
      return { ok: false, error: `送信に失敗しました (${response.status})` }
    }
    return { ok: true }
  }, [])

  const cancelChat = React.useCallback(async () => {
    await fetch(`${SERVER_DOMAIN}/api/demo/chat/cancel`, {
      method: "POST",
      headers: demoFetchHeaders(),
    })
  }, [])

  const resetDemo = React.useCallback(async () => {
    const response = await fetch(`${SERVER_DOMAIN}/api/demo/reset`, {
      method: "POST",
      headers: demoFetchHeaders(),
    })
    if (response.status === 423) {
      const body = await response.json().catch(() => null)
      return { ok: false, error: body?.message ?? "他のユーザーまたはAIが編集中です" }
    }
    if (!response.ok) {
      return { ok: false, error: `リセットに失敗しました (${response.status})` }
    }
    return { ok: true }
  }, [])

  const value = React.useMemo<DemoModeContextValue>(() => ({
    isDemoMode,
    demoUrl,
    lock,
    isLockedByOther: lock != null && lock.ownerClientId !== myClientId,
    isLockedByMe: lock != null && lock.ownerClientId === myClientId,
    demoAppStatus,
    chatMessages,
    streamingText,
    processLogs,
    reloadReason,
    sendChat,
    cancelChat,
    resetDemo,
  }), [isDemoMode, demoUrl, lock, myClientId, demoAppStatus, chatMessages, streamingText, processLogs, reloadReason, sendChat, cancelChat, resetDemo])

  return (
    <DemoModeContext.Provider value={value}>
      {children}
    </DemoModeContext.Provider>
  )
}

export const useDemoMode = (): DemoModeContextValue => {
  const context = React.useContext(DemoModeContext)
  return context ?? NOT_DEMO_MODE
}
