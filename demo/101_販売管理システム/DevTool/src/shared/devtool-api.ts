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

/** ログの指定オフセット以降の増分。プレビュープロセスのログ・DevToolサーバー自身のログの両方で使う。 */
export type LogIncrement = {
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

//#endregion プレビュー

//#region DevToolの稼働状態

/**
 * DevToolの稼働状態取得のリクエストボディ。
 * プレビュープロセスのログとDevToolサーバー自身のログを、同じ1秒ポーリングでまとめて取得する。
 */
export type DevToolStateRequest = {
  offsets: Record<string, PreviewLogOffsets>
  /** DevToolサーバー自身のログの既読オフセット */
  serverLogOffset: number
}

/** /devtool-api/state のレスポンス */
export type DevToolStateResponse = {
  processes: (PreviewProcessState & { stdout: LogIncrement, stderr: LogIncrement })[]
  /** デバッグ対象アプリのオリジンへのHTTP応答ステータス。到達できない場合は null */
  targetStatus: number | null
  /** DevToolサーバー自身のログの増分 */
  serverLog: LogIncrement
}

//#endregion DevToolの稼働状態

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

/** OpenRouter で選択可能なモデル。ツール呼び出し対応の無料モデルのみが対象。 */
export type OpenRouterModel = {
  /** モデルID。エージェント設定（chatModel / codingModel）に保存される値そのもの。 */
  id: string
  /** 画面表示用の名称。 */
  name: string
  /** コンテキストウィンドウのトークン数。 */
  contextLength: number
}

/** /devtool-api/openrouter/models のレスポンス。取得に失敗した場合も200を返し、error にメッセージを載せる。 */
export type OpenRouterModelsResponse = {
  models: OpenRouterModel[]
  error?: string
}

//#endregion アプリ設定

//#region チャットセッション

/**
 * 変更計画。1つのセッションでの対話の成果物であり、1セッションにつき最大1つ。
 * まだ立てられていない間は null になる。
 */
export type ChangePlan = {
  /** 変更計画の見出し。 */
  title: string
  /** 変更計画の状態。'draft' など。 */
  status: string
  /** 本文（Markdown）。 */
  body: string
}

/**
 * セッション一覧表示用の要約。会話の本文・変更計画の本文は含まない。
 */
export type ChatSessionSummary = {
  /** yyyyMMddHHmmss_UUID 形式。 */
  id: string
  /** 一覧表示用の見出し。最初のユーザー発言から導出される。発言がまだ無い場合は既定の文言になる。 */
  title: string
  /** 作成日時（ISO8601）。 */
  createdAt: string
  /** 変更計画の見出しと状態。変更計画が未作成の場合は null。 */
  changePlanSummary: Pick<ChangePlan, 'title' | 'status'> | null
}

/**
 * セッション1件。会話の全メッセージと変更計画を含む。
 * ブラウザリロードやサーバー再起動などをまたいで残したい永続化された情報。
 * セッションは複数を並行して進行させることができる。
 */
export type ChatSessionDto = ChatSessionSummary & {
  /** この会話でのやりとり全て。 */
  messages: UIMessage[]
  /** この会話の成果物である変更計画。未作成の場合は null。 */
  changePlan: ChangePlan | null
  /**
   * 楽観排他制御用のバージョン。更新時刻UTC。
   * 更新が競合するとAIの応答が壊れるので念のため
   */
  concurrencyVersion: string
}

//#endregion チャットセッション
