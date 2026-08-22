/**
 * src/client（ブラウザ）と src/server（Node）の双方から import される契約。
 * 型と純粋なリテラル定数のみを置く。node: の import・DOM 型・実行時の副作用は禁止。
 */

/** デバッグ実行プロセスの識別名。ツールバーの表示順もこれに従う。 */
export const PREVIEW_PROCESS_NAMES = ['vite', 'dotnet'] as const
export type PreviewProcessName = typeof PREVIEW_PROCESS_NAMES[number]

/** プロセスごとの標準出力・標準エラー出力それぞれの既読オフセット */
export type PreviewLogOffsets = {
  stdout: number
  stderr: number
}

/** デバッグ実行の状態取得のリクエストボディ */
export type PreviewStateRequest = {
  offsets: Record<string, PreviewLogOffsets>
}

/** ログの指定オフセット以降の増分 */
export type PreviewLogIncrement = {
  text: string
  offset: number
}

/** 稼働中の1プロセスの状態 */
export type PreviewProcessState = {
  name: string
  isRunning: boolean
  processId: number | null
  exitCode: number | null
}

/** /devtool-api/preview/state のレスポンス */
export type PreviewStateResponse = {
  processes: (PreviewProcessState & { stdout: PreviewLogIncrement, stderr: PreviewLogIncrement })[]
}

/** 変更プランの一覧表示用の要約 */
export type ChangePlanSummary = {
  id: string
  title: string
  status: string
  createdAt: string | null
}

/** 変更プランの詳細（本文込み） */
export type ChangePlanDetail = ChangePlanSummary & {
  body: string
}

/** APIキーの保管先。OSキーチェーンが使えない環境では 'environmentVariable' か 'unavailable' になる。 */
export type ApiKeyStorage = 'keychain' | 'environmentVariable' | 'unavailable'

/** /devtool-api/settings のレスポンス。APIキー本体は含まない。 */
export type DevToolSettings = {
  chatModel: string
  codingModel: string
  apiKeyStorage: ApiKeyStorage
  hasApiKey: boolean
}
