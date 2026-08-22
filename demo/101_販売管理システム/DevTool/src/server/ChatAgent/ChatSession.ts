import { randomUUID } from "node:crypto"
import { mkdir, readdir, readFile, rm, writeFile } from "node:fs/promises"
import path from "node:path"
import type { ChatSessionDto, ChatSessionSummary } from "../../shared/devtool-api.ts"

/**
 * ファイル名として安全なセッションID。yyyyMMddHHmmss と UUID を "_" でつないだ形。
 * ドット・スラッシュを含み得ないため、この検査を通ったIDは親ディレクトリへ抜けられない。
 */
const SESSION_ID_PATTERN = /^\d{14}_[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/

/** 発言がまだ無いセッションの見出し */
const UNTITLED = "新しいチャット"

/** 見出しに使うユーザー発言の最大文字数。これを超える分は省略記号に置き換える。 */
const TITLE_MAX_LENGTH = 40

/**
 * エージェントとの会話セッションを管理するクラス。
 * ブラウザリロードやサーバー再起動などをまたいで残したい永続化された情報を扱う。
 *
 * .nijo フォルダ配下に1セッション1つのJSONファイルを置く。
 * 複数のセッションが同時に進行することを前提とし、セッション間で共有される状態は持たない。
 * 永続化の詳細はクラス内に隠蔽する。
 * サーバーで1つであり、ユーザーごとに分けたりはしない。
 */
export class ChatSession {

  readonly #sessionsDir: string

  constructor(devToolRoot: string) {
    this.#sessionsDir = path.join(devToolRoot, ".nijo", "sessions")
  }

  /** セッションの要約一覧を、作成日時の新しい順で返す */
  async list(): Promise<ChatSessionSummary[]> {
    const fileNames = await readdir(this.#sessionsDir).catch(() => [])
    const ids = fileNames.filter(name => name.endsWith(".json")).map(name => name.slice(0, -5))

    const summaries: ChatSessionSummary[] = []
    for (const id of ids) {
      const dto = await this.read(id)
      if (dto) summaries.push({ id: dto.id, title: dto.title, createdAt: dto.createdAt, changePlanSummary: dto.changePlanSummary })
    }
    // IDの先頭が採番時刻なので、ID降順がそのまま作成日時の新しい順になる
    return summaries.sort((a, b) => b.id.localeCompare(a.id))
  }

  /** 指定IDのセッションを返す。IDが不正・ファイルが無い・壊れている場合は null（例外は投げない）。 */
  async read(id: string): Promise<ChatSessionDto | null> {
    if (!SESSION_ID_PATTERN.test(id)) return null

    const raw = await readFile(this.#filePath(id), "utf8").catch(() => null)
    if (raw === null) return null

    try {
      return ChatSession.#toDto(id, JSON.parse(raw) as StoredSession)
    } catch {
      return null
    }
  }

  /** 空のセッションを採番して作成し、その内容を返す */
  async create(): Promise<ChatSessionDto> {
    const id = `${ChatSession.#timestamp(new Date())}_${randomUUID()}`
    const dto = ChatSession.#toDto(id, { messages: [], changePlan: null, concurrencyVersion: new Date(0).toISOString() })
    await this.save(dto)
    return dto
  }

  /** セッションを上書き保存する。concurrencyVersion は保存の都度このメソッドが採番する。 */
  async save(dto: ChatSessionDto): Promise<void> {
    // id・createdAt・title は読み出し時に導出される項目なのでファイルには書かない
    const toWrite: StoredSession = {
      messages: dto.messages,
      changePlan: dto.changePlan,
      concurrencyVersion: new Date().toISOString(),
    }
    await mkdir(this.#sessionsDir, { recursive: true })
    await writeFile(this.#filePath(dto.id), JSON.stringify(toWrite, null, 2), "utf8")
  }

  /** セッションを削除する。IDが不正・ファイルが無い場合は何もしない。 */
  async delete(id: string): Promise<void> {
    if (!SESSION_ID_PATTERN.test(id)) return
    await rm(this.#filePath(id), { force: true })
  }

  #filePath(id: string): string {
    return path.join(this.#sessionsDir, `${id}.json`)
  }

  /** ファイルに書かれている内容に、IDから導出できる項目を補って返す */
  static #toDto(id: string, stored: StoredSession): ChatSessionDto {
    const changePlan = stored.changePlan ?? null
    return {
      id,
      title: ChatSession.#buildTitle(stored.messages ?? []),
      createdAt: ChatSession.#parseTimestamp(id),
      changePlanSummary: changePlan && { title: changePlan.title, status: changePlan.status },
      messages: stored.messages ?? [],
      changePlan,
      concurrencyVersion: stored.concurrencyVersion,
    }
  }

  /** 最初のユーザー発言の1行目を見出しにする。発言がまだ無い場合は既定の文言。 */
  static #buildTitle(messages: ChatSessionDto["messages"]): string {
    const firstUserText = messages
      .find(message => message.role === "user")
      ?.parts.find(part => part.type === "text")
      ?.text
    const firstLine = firstUserText?.split("\n").find(line => line.trim() !== "")?.trim()
    if (!firstLine) return UNTITLED
    return firstLine.length > TITLE_MAX_LENGTH ? `${firstLine.slice(0, TITLE_MAX_LENGTH)}…` : firstLine
  }

  static #timestamp(date: Date): string {
    const pad = (value: number, length = 2) => String(value).padStart(length, "0")
    return `${date.getFullYear()}${pad(date.getMonth() + 1)}${pad(date.getDate())}${pad(date.getHours())}${pad(date.getMinutes())}${pad(date.getSeconds())}`
  }

  /** IDの先頭14桁（採番時刻）を ISO8601 に戻す */
  static #parseTimestamp(id: string): string {
    const year = Number(id.slice(0, 4))
    const month = Number(id.slice(4, 6))
    const day = Number(id.slice(6, 8))
    const hour = Number(id.slice(8, 10))
    const minute = Number(id.slice(10, 12))
    const second = Number(id.slice(12, 14))
    return new Date(year, month - 1, day, hour, minute, second).toISOString()
  }
}

/** JSONファイルに実際に書かれている内容。この形はクラスの外へ出さない。 */
type StoredSession = {
  messages: ChatSessionDto["messages"]
  changePlan: ChatSessionDto["changePlan"]
  concurrencyVersion: string
}
