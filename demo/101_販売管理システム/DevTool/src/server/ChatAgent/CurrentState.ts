import { mkdir, readFile, writeFile } from "node:fs/promises"
import path from "node:path"
import type { CurrentStateDto } from "../../shared/devtool-api.ts"

/**
 * エージェントの現在の状態を管理するクラス。
 * ブラウザリロードやサーバー再起動などをまたいで残したい永続化された情報を扱う。
 *
 * .nijo フォルダ配下にJSONファイルを置いてそのまま書き込む。
 * 永続化の詳細はクラス内に隠蔽する。
 * サーバーで1つであり、ユーザーごとに分けたりはしない。
 */
export class CurrentState {

  static readonly #FILE_NAME = "current-state.json"
  /** latestSessions に残しておく直近セッションの最大件数。これを超えた古いものから削除する。 */
  static readonly #LATEST_SESSIONS_MAX = 5

  readonly #filePath: string

  constructor(devToolRoot: string) {
    this.#filePath = path.join(devToolRoot, ".nijo", CurrentState.#FILE_NAME)
  }

  /** 保存済みの状態を返す。ファイルが無い・壊れている場合は空の状態を返す（例外は投げない）。 */
  async load(): Promise<CurrentStateDto> {
    const raw = await readFile(this.#filePath, "utf8").catch(() => null)
    if (raw === null) return CurrentState.#empty()

    try {
      return JSON.parse(raw) as CurrentStateDto
    } catch {
      return CurrentState.#empty()
    }
  }

  /** 状態を保存する。concurrencyVersion は保存の都度このメソッドが採番する。 */
  async save(dto: CurrentStateDto): Promise<void> {
    const toWrite: CurrentStateDto = { ...dto, concurrencyVersion: new Date().toISOString() }
    await mkdir(path.dirname(this.#filePath), { recursive: true })
    await writeFile(this.#filePath, JSON.stringify(toWrite, null, 2), "utf8")
  }

  /**
   * 会話を仕切り直す。現在のセッションを latestSessions の末尾に積み、
   * 直近 {@link CurrentState.#LATEST_SESSIONS_MAX} 件を超えた古いものから削除したうえで currentSession を空にする。
   * 現在のセッションが空の場合は何もしない（空の会話を latestSessions に積まないため）。
   */
  async startNewSession(): Promise<void> {
    const dto = await this.load()
    if (dto.currentSession.length === 0) return

    const latestSessions = [...(dto.latestSessions ?? []), dto.currentSession].slice(-CurrentState.#LATEST_SESSIONS_MAX)
    await this.save({ ...dto, currentSession: [], latestSessions })
  }

  static #empty(): CurrentStateDto {
    return { currentSession: [], concurrencyVersion: new Date(0).toISOString() }
  }
}
