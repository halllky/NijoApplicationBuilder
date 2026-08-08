import type { DemoStatusResponse } from "./types"

/**
 * 共有デモサイトにおいて、ブラウザタブ(セッション)を識別するためのID。
 * 認証ではなく、排他ロックの所有者判定・強制リロード配信時の自分自身の除外にのみ使う。
 */
const STORAGE_KEY = "nijo-demo-client-id"

export const DEMO_CLIENT_ID_HEADER = "X-Demo-Client-Id"

export const getDemoClientId = (): string => {
  let id = sessionStorage.getItem(STORAGE_KEY)
  if (!id) {
    id = crypto.randomUUID()
    sessionStorage.setItem(STORAGE_KEY, id)
  }
  return id
}

export const demoFetchHeaders = (): Record<string, string> => ({
  [DEMO_CLIENT_ID_HEADER]: getDemoClientId(),
})

/**
 * /api/demo/status への問い合わせ結果。共有デモサイトモードでなければnull。
 * ルーターのローダーとDemoModeProviderの両方から参照されるため、
 * プロセス中の初回呼び出し結果をキャッシュして問い合わせを1回にまとめる。
 */
let demoStatusCheck: Promise<DemoStatusResponse | null> | null = null

export const fetchDemoStatus = (serverDomain: string): Promise<DemoStatusResponse | null> => {
  if (!demoStatusCheck) {
    demoStatusCheck = fetch(`${serverDomain}/api/demo/status`, { headers: demoFetchHeaders() })
      .then(async response => {
        const contentType = response.headers.get("content-type") ?? ""
        if (!response.ok || !contentType.includes("application/json")) return null
        const status: DemoStatusResponse = await response.json()
        return status.demoMode ? status : null
      })
      .catch(() => null)
  }
  return demoStatusCheck
}
