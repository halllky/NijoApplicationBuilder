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
