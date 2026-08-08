export type DemoLockInfo = {
  ownerClientId: string
  reason: string
  acquiredAtUtc: string
}

export type DemoChatMessage = {
  /** system: サーバーからの通知(ビルド修復の進捗など), error: サーバー側で発生したエラーの通知 */
  role: "user" | "assistant" | "system" | "error"
  content: string
  createdAtUtc: string
}

export type DemoAppStatus = "stopped" | "starting" | "running" | "error"

/** AIチャット処理の状態。running: claude実行中, building: 変更の反映ビルド中 */
export type ChatStatus = "idle" | "running" | "building"

export type DemoStatusResponse = {
  demoMode: boolean
  lockInfo: DemoLockInfo | null
  demoUrl: string
  demoAppStatus: DemoAppStatus
  chatStatus: ChatStatus
  lastActivityUtc: string
  chatHistory: DemoChatMessage[]
}
