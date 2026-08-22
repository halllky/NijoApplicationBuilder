import { readFile, writeFile } from "node:fs/promises"
import { parse, stringify } from "envfile"
import type { ApiKeyStorage, DevToolSettings as DevToolSettingsValue } from "../shared/devtool-api.ts"

const KEYCHAIN_SERVICE_NAME = "nijo-devtool"
const KEYCHAIN_ACCOUNT_NAME = "anthropic-api-key"

const ENV_KEY_CHAT_MODEL = "DEVTOOL_CHAT_MODEL"
const ENV_KEY_CODING_MODEL = "DEVTOOL_CODING_MODEL"
const DEFAULT_CHAT_MODEL = "claude-opus-5"
const DEFAULT_CODING_MODEL = "claude-opus-5"

type Keychain = typeof import("@github/keytar")

/**
 * チャット・コーディング両エージェントのモデル設定（.env.local）と、
 * Anthropic APIキー（OSキーチェーン）を保管・取得する。
 * キーはこのクラスの外（ブラウザ側）に一切渡さない。
 */
export class DevToolSettings {
  readonly #envLocalPath: string
  #keychainResolved = false
  #keychainCache: Keychain | null = null

  constructor(envLocalPath: string) {
    this.#envLocalPath = envLocalPath
  }

  /** ブラウザに返してよい設定一式を返す */
  async read(): Promise<DevToolSettingsValue> {
    const models = await this.#readModels()
    const apiKeyStorage = await this.#apiKeyStorage()
    const apiKey = await this.readApiKey()
    return { ...models, apiKeyStorage, hasApiKey: apiKey !== null }
  }

  /** モデル設定のみを .env.local に書き込む。ファイル内の他のキーは維持する。 */
  async writeModels(models: { chatModel: string, codingModel: string }): Promise<void> {
    const env = parse(await this.#readEnvLocal())
    env[ENV_KEY_CHAT_MODEL] = models.chatModel
    env[ENV_KEY_CODING_MODEL] = models.codingModel
    await writeFile(this.#envLocalPath, stringify(env), "utf8")
  }

  /**
   * Anthropic APIキーを取得する。キーチェーンが使える環境ではそちらを優先し、
   * 使えない環境では環境変数 ANTHROPIC_API_KEY にフォールバックする。
   */
  async readApiKey(): Promise<string | null> {
    const keychain = await this.#keychain()
    if (keychain) {
      const stored = await keychain.getPassword(KEYCHAIN_SERVICE_NAME, KEYCHAIN_ACCOUNT_NAME)
      if (stored) return stored
    }
    return process.env.ANTHROPIC_API_KEY ?? null
  }

  /**
   * Anthropic APIキーをキーチェーンへ保存する。
   * キーチェーンが使えない環境では失敗を示す false を返す。
   */
  async writeApiKey(apiKey: string): Promise<boolean> {
    const keychain = await this.#keychain()
    if (!keychain) return false
    await keychain.setPassword(KEYCHAIN_SERVICE_NAME, KEYCHAIN_ACCOUNT_NAME, apiKey)
    return true
  }

  /** キーチェーンからAPIキーを削除する。キーチェーンが使えない環境では false を返す。 */
  async deleteApiKey(): Promise<boolean> {
    const keychain = await this.#keychain()
    if (!keychain) return false
    return keychain.deletePassword(KEYCHAIN_SERVICE_NAME, KEYCHAIN_ACCOUNT_NAME)
  }

  async #readModels(): Promise<{ chatModel: string, codingModel: string }> {
    const env = parse(await this.#readEnvLocal())
    return {
      chatModel: env[ENV_KEY_CHAT_MODEL] || DEFAULT_CHAT_MODEL,
      codingModel: env[ENV_KEY_CODING_MODEL] || DEFAULT_CODING_MODEL,
    }
  }

  async #readEnvLocal(): Promise<string> {
    return await readFile(this.#envLocalPath, "utf8").catch(() => "")
  }

  async #apiKeyStorage(): Promise<ApiKeyStorage> {
    const keychain = await this.#keychain()
    if (keychain) return "keychain"
    return process.env.ANTHROPIC_API_KEY ? "environmentVariable" : "unavailable"
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
      this.#keychainCache = await import("@github/keytar")
    } catch {
      this.#keychainCache = null
    }
    return this.#keychainCache
  }
}
