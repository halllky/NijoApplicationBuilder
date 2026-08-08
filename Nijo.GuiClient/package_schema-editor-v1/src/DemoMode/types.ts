export type DemoLockInfo = {
  ownerClientId: string
  reason: string
  acquiredAtUtc: string
}

export type DemoChatMessage = {
  role: "user" | "assistant"
  content: string
  createdAtUtc: string
}

export type DemoAppStatus = "stopped" | "starting" | "running" | "error"

export type DemoStatusResponse = {
  demoMode: boolean
  lockInfo: DemoLockInfo | null
  demoUrl: string
  demoAppStatus: DemoAppStatus
  lastActivityUtc: string
  chatHistory: DemoChatMessage[]
}
