import { APICallError, RetryError } from "ai"
import type { LogFields } from "../ServerLog.ts"

/** レートリミット到達時に利用者へ見せる文言。「待てば直る」ことと代替手段が分かるようにする。 */
export const RATE_LIMIT_MESSAGE = "AIサービスの利用回数の上限に達しました。しばらく時間をおいてからもう一度お試しください。設定画面から別のモデルに切り替えることもできます。"

/** レートリミット以外の回復不能なエラーに使う、従来からの文言。 */
export const INTERNAL_ERROR_MESSAGE = "サーバー内部でエラーが発生しました。"

/**
 * レートリミットの可能性がある文字列パターン（フォールバック判定用）。
 * OpenRouterの無料モデルは、ステータスコード429ではなく200 + エラーボディの形で
 * 上限到達を伝えてくることがあるため、statusCodeだけでは判定しきれない。
 */
const RATE_LIMIT_TEXT_PATTERN = /rate.?limit|too many requests/i

/**
 * 与えられたエラーが（自動リトライを使い切った末の）レートリミットかどうかを判定する。
 * AI SDK の既定の maxRetries（2回）は429を含むretryableなエラーを自動で再試行するため、
 * ここまで到達するエラーは RetryError に包まれていることが多い。まずそれを剥がしてから判定する。
 */
export function isRateLimitError(error: unknown): boolean {
  const unwrapped = unwrapRetryError(error)

  if (APICallError.isInstance(unwrapped)) {
    if (unwrapped.statusCode === 429) return true
    const bodyText = typeof unwrapped.responseBody === "string" ? unwrapped.responseBody : ""
    if (RATE_LIMIT_TEXT_PATTERN.test(bodyText) || RATE_LIMIT_TEXT_PATTERN.test(unwrapped.message)) return true
    return false
  }

  return unwrapped instanceof Error && RATE_LIMIT_TEXT_PATTERN.test(unwrapped.message)
}

/** エラーの種類に応じて利用者向けの文言を返す。 */
export function toUserFacingMessage(error: unknown): string {
  return isRateLimitError(error) ? RATE_LIMIT_MESSAGE : INTERNAL_ERROR_MESSAGE
}

/**
 * ログに残す用に、エラーからレートリミット判定の根拠となるフィールドを取り出す。
 * normalizeErrorField（ServerLog.ts）は Error を message/stack にしか展開せず、
 * APICallError が持つ statusCode 等はそのままでは失われるため、ここで別フィールドとして明示的に残す。
 */
export function describeLlmError(error: unknown): LogFields {
  const unwrapped = unwrapRetryError(error)
  const fields: LogFields = { isRateLimit: isRateLimitError(error) }
  if (APICallError.isInstance(unwrapped)) {
    fields.statusCode = unwrapped.statusCode
  }
  return fields
}

/** RetryError（maxRetries使い切り）であれば、最後に失敗した元エラーを取り出す。それ以外はそのまま返す。 */
function unwrapRetryError(error: unknown): unknown {
  return RetryError.isInstance(error) ? error.lastError : error
}
