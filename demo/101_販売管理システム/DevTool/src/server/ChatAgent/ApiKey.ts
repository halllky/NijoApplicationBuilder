import type { ApiKeyStorage } from "../../shared/devtool-api.ts"
import { serverLog } from "../ServerLog.ts"

const KEYCHAIN_SERVICE_NAME = "nijo-devtool"
const KEYCHAIN_ACCOUNT_NAME = "openrouter-api-key"

type Keychain = typeof import("@github/keytar")

/**
 * OpenRouter APIキーをOSキーチェーンに保管・取得する。
 * キーチェーンが使えない環境では環境変数 OPENROUTER_API_KEY にフォールバックする。
 * キーはこのクラスの外（ブラウザ側）に一切渡さない。
 */
export class ApiKey {

  static readonly ENV_OPENROUTER = "OPENROUTER_API_KEY"

  #keychainResolved = false
  #keychainCache: Keychain | null = null

  async read(): Promise<string | null> {
    const keychain = await this.#keychain()
    if (keychain) {
      const stored = await keychain.getPassword(KEYCHAIN_SERVICE_NAME, KEYCHAIN_ACCOUNT_NAME)
      if (stored) return stored
    }
    return process.env[ApiKey.ENV_OPENROUTER] ?? null
  }

  /** キーチェーンへ保存する。キーチェーンが使えない環境では失敗を示す false を返す。 */
  async write(apiKey: string): Promise<boolean> {
    const keychain = await this.#keychain()
    if (!keychain) return false
    await keychain.setPassword(KEYCHAIN_SERVICE_NAME, KEYCHAIN_ACCOUNT_NAME, apiKey)
    return true
  }

  /** キーチェーンから削除する。キーチェーンが使えない環境では false を返す。 */
  async delete(): Promise<boolean> {
    const keychain = await this.#keychain()
    if (!keychain) return false
    return keychain.deletePassword(KEYCHAIN_SERVICE_NAME, KEYCHAIN_ACCOUNT_NAME)
  }

  async storage(): Promise<ApiKeyStorage> {
    const keychain = await this.#keychain()
    if (keychain) return "keychain"
    return process.env[ApiKey.ENV_OPENROUTER] ? "environmentVariable" : "unavailable"
  }

  /**
   * OSのキーチェーンへのアクセス手段を返す。ネイティブモジュールであるため、
   * 未インストールや読み込み失敗（ヘッドレス環境でOSのキーチェーン機構が無い等）がありうる。
   * その場合は null を返し、呼び出し側は環境変数へフォールバックする。
   */
  async #keychain(): Promise<Keychain | null> {
    if (this.#keychainResolved) return this.#keychainCache
    this.#keychainResolved = true
    try {
      // @github/keytar は CommonJS モジュールで、Node の ESM 相互運用層は
      // named export の静的検出に失敗する（setPassword 等が抜け落ちる）ため、
      // 常に module.exports 全体が入る default 側から取得する。
      const mod = (await import("@github/keytar")) as unknown as { default: Keychain }
      this.#keychainCache = mod.default
    } catch (error) {
      this.#keychainCache = null
      // #keychainResolved により以降は再試行しない＝この警告も起動後1回だけ出る
      serverLog.warn("apikey.keychain.unavailable", { error })
    }
    return this.#keychainCache
  }
}
