import { readFile, writeFile } from "node:fs/promises"
import { parse, stringify } from "envfile"
import path from "node:path"

const ENV_KEY_CHAT_MODEL = "DEVTOOL_CHAT_MODEL"
const ENV_KEY_CODING_MODEL = "DEVTOOL_CODING_MODEL"
const DEFAULT_CHAT_MODEL = "claude-opus-5"
const DEFAULT_CODING_MODEL = "claude-opus-5"

/** チャット・コーディング両エージェントのモデル設定を .env.local / .env に保管・取得する。 */
export class DotEnv {
  readonly #localPath: string
  readonly #path: string

  constructor(devToolRoot: string) {
    this.#localPath = path.join(devToolRoot, ".env.local")
    this.#path = path.join(devToolRoot, ".env")
  }

  async readModels(): Promise<{ chatModel: string, codingModel: string }> {
    const env = parse(await this.#read())
    return {
      chatModel: env[ENV_KEY_CHAT_MODEL] || DEFAULT_CHAT_MODEL,
      codingModel: env[ENV_KEY_CODING_MODEL] || DEFAULT_CODING_MODEL,
    }
  }

  /** モデル設定のみを .env.local に書き込む。ファイル内の他のキーは維持する。 */
  async writeModels(models: { chatModel: string, codingModel: string }): Promise<void> {
    const env = parse(await this.#read())
    env[ENV_KEY_CHAT_MODEL] = models.chatModel
    env[ENV_KEY_CODING_MODEL] = models.codingModel
    await writeFile(this.#localPath, stringify(env), "utf8")
  }

  /** .env.local があればそちらを優先し、なければ .env を読み取る。 */
  async #read(): Promise<string> {
    const local = await readFile(this.#localPath, "utf8").catch(() => null)
    if (local !== null) return local
    return await readFile(this.#path, "utf8").catch(() => "")
  }
}
