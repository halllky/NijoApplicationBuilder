import { UIMessage } from "ai"

//#region プレビュー

/**
 * src/client（ブラウザ）と src/server（Node）の双方から import される契約。
 * 型と純粋なリテラル定数のみを置く。node: の import・DOM 型・実行時の副作用は禁止。
 */

/** デバッグ実行プロセスの識別名。ツールバーの表示順もこれに従う。 */
export const PREVIEW_PROCESS_NAMES = ['vite', 'dotnet'] as const
export type PreviewProcessName = typeof PREVIEW_PROCESS_NAMES[number]

/**
 * デバッグ対象アプリ（生成後アプリの client）の vite dev server のオリジン。
 * iframe の src、及びサーバー側の到達確認の両方がこの値を参照する唯一の出所。
 */
export const PREVIEW_TARGET_ORIGIN = 'http://localhost:5173/'

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
  /** デバッグ対象アプリのオリジンへのHTTP応答ステータス。到達できない場合は null */
  targetStatus: number | null
}

//#endregion プレビュー

//#region 変更プラン

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

//#region 変更プラン

//#region アプリ設定

/** APIキーの保管先。OSキーチェーンが使えない環境では 'environmentVariable' か 'unavailable' になる。 */
export type ApiKeyStorage = 'keychain' | 'environmentVariable' | 'unavailable'

/** /devtool-api/settings のレスポンス。APIキー本体は含まない。 */
export type DevToolSettings = {
  chatModel: string
  codingModel: string
  apiKeyStorage: ApiKeyStorage
  hasApiKey: boolean
}

//#endregion アプリ設定

//#region エージェント

/**
 * エージェントの現在の状態。
 * ブラウザリロードやサーバー再起動などをまたいで残したい永続化された情報。
 */
export type CurrentStateDto = {
  /**
   * いま行なっていた会話。
   * ブラウザリロードで消えてしまわないようにするために保持されている。
   * 会話の仕切り直しによって消える。
   */
  currentSession: UIMessage[]
  /**
   * 直近数回の会話の内容。
   * 古いものは会話仕切り直し時に削除される。
   */
  latestSessions?: UIMessage[][]
  /**
   * 楽観排他制御用のバージョン。更新時刻UTC。
   * 更新が競合するとAIの応答が壊れるので念のため
   */
  concurrencyVersion: string
}

//#endregion チャット
