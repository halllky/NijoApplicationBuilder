import { appendFile, mkdir } from "node:fs/promises"
import path from "node:path"
import { fileURLToPath } from "node:url"
import type { LogIncrement } from "../shared/devtool-api.ts"
import { LogBuffer } from "./LogBuffer.ts"

// このファイルの位置から解決する（実行時のカレントディレクトリに依存させないため）
const devToolRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..")

/** fly.io 上で動いているかどうか。fly は machine 起動時に必ずこの環境変数を設定する。 */
const IS_FLY = Boolean(process.env.FLY_APP_NAME)

/** 全レコードに載せる基底フィールド。fly.io 上でのみ値が入る。 */
const BASE_FIELDS: LogFields = {
  ...(process.env.FLY_MACHINE_ID ? { machine: process.env.FLY_MACHINE_ID } : {}),
  ...(process.env.FLY_REGION ? { region: process.env.FLY_REGION } : {}),
}

type Level = "error" | "warn" | "info"

/** ログレベルの重大度順（小さいほど重大）。LOG_LEVEL による絞り込みに使う。 */
const LEVEL_ORDER: Record<Level, number> = { error: 0, warn: 1, info: 2 }

/** 環境変数 LOG_LEVEL。不正・未指定なら info。 */
const CONFIGURED_LEVEL: Level = ((): Level => {
  const value = process.env.LOG_LEVEL
  return value === "error" || value === "warn" || value === "info" ? value : "info"
})()

/** 1レコードあたりの文字列フィールドの切り詰め上限。conversation() はこれを適用しない（利用者に見える発話は全文残す）。 */
const FIELD_TRUNCATE_LENGTH = 2000

/** 値を伏せるフィールド名（大文字小文字を区別しない）。API キーなどを誤って渡しても画面・fly logs に出さないための最後の砦。 */
const MASKED_FIELD_NAMES = new Set(["apikey", "authorization", "key", "password", "secret", "token"])

/** 画面表示用にサーバーログを保持するリングバッファの上限文字数。 */
const RING_BUFFER_MAX_LENGTH = 256 * 1024

export type LogFields = Record<string, unknown>

/**
 * process.stdout / process.stderr への書き込みを横取りし、リングバッファに複製する。
 * ServerLog 自身の出力だけでなく、AI SDK や依存ライブラリが直接呼ぶ console.log 等・
 * 未捕捉例外のスタックトレースも含めて全て画面表示（{@link ServerLog.readTail}）の対象にするための仕掛け。
 * 標準出力そのものへの書き込みは常に元の write に委譲するため、実際の出力内容・順序は変わらない。
 */
function teeStream(stream: NodeJS.WriteStream, buffer: LogBuffer): void {
  const original = stream.write.bind(stream)
  const wrapped: typeof stream.write = (chunk: unknown, encodingOrCallback?: unknown, callback?: unknown): boolean => {
    try {
      buffer.append(chunkToText(chunk))
    } catch {
      // バッファへの反映に失敗しても標準出力自体は継続させる
    }
    // Node の write() は複数のオーバーロードを持ち型で正確に表せないため、実引数をそのまま委譲する
    // @ts-expect-error 実引数の組み合わせは呼び出し元（Node内部・console.* 等）が保証する
    return original(chunk, encodingOrCallback, callback)
  }
  stream.write = wrapped
}

function chunkToText(chunk: unknown): string {
  if (typeof chunk === "string") return chunk
  if (Buffer.isBuffer(chunk)) return chunk.toString("utf8")
  return String(chunk)
}

const ringBuffer = new LogBuffer(RING_BUFFER_MAX_LENGTH)
teeStream(process.stdout, ringBuffer)
teeStream(process.stderr, ringBuffer)

/** fields.error が Error インスタンスの場合、message と stack に展開する。それ以外はそのまま返す。 */
function normalizeErrorField(fields: LogFields): LogFields {
  const error = fields.error
  if (!(error instanceof Error)) return fields
  const { error: _dropped, ...rest } = fields
  return { ...rest, error: error.message, stack: error.stack }
}

function maskFields(fields: LogFields): LogFields {
  const result: LogFields = {}
  for (const [key, value] of Object.entries(fields)) {
    result[key] = MASKED_FIELD_NAMES.has(key.toLowerCase()) ? "***" : value
  }
  return result
}

function truncateFields(fields: LogFields): LogFields {
  const result: LogFields = {}
  for (const [key, value] of Object.entries(fields)) {
    result[key] = typeof value === "string" && value.length > FIELD_TRUNCATE_LENGTH
      ? `${value.slice(0, FIELD_TRUNCATE_LENGTH)}…(${value.length}文字)`
      : value
  }
  return result
}

function prepareFields(fields: LogFields, truncate: boolean): LogFields {
  const masked = maskFields(normalizeErrorField(fields))
  return truncate ? truncateFields(masked) : masked
}

/** stdout/stderr 向けの1行レコードを組み立てる。fly.io 上では JSON 1行、それ以外は logfmt 風の1行。 */
function buildRecord(level: Level, event: string, fields: LogFields): string {
  const time = new Date().toISOString()
  const allFields = { ...BASE_FIELDS, ...fields }
  if (IS_FLY) return JSON.stringify({ time, level, event, ...allFields })
  const rendered = formatLogfmtFields(allFields)
  return rendered ? `${time} ${level.padEnd(5)} ${event} ${rendered}` : `${time} ${level.padEnd(5)} ${event}`
}

/** トレース（AI内部の全文）用のレコード。ローカルファイル・fly.io(stdout) のどちらに書く場合も同じ形にする。 */
function buildTraceRecord(event: string, fields: LogFields): string {
  const time = new Date().toISOString()
  return JSON.stringify({ time, level: "trace", event, ...BASE_FIELDS, ...fields })
}

function formatLogfmtFields(fields: LogFields): string {
  return Object.entries(fields)
    .filter(([, value]) => value !== undefined)
    .map(([key, value]) => `${key}=${formatLogfmtValue(value)}`)
    .join(" ")
}

function formatLogfmtValue(value: unknown): string {
  if (typeof value === "string") return /[\s="]/.test(value) ? JSON.stringify(value) : value
  if (value === null) return "null"
  if (typeof value === "object") return JSON.stringify(value)
  return String(value)
}

/**
 * ローカル実行時のトレース書き込み先。.nijo/logs/<sessionId>.jsonl に1行ずつ追記する（.nijo は gitignore 済み）。
 * 同一ファイルへの appendFile はオープン・書き込み・クローズが分かれた非同期処理のため、
 * 同一セッションに複数ターンが並走すると行が混ざり得る。ファイルパスごとにチェーンして直列化する。
 */
const fileWriteChains = new Map<string, Promise<void>>()

function traceFilePath(sessionId: string): string {
  return path.join(devToolRoot, ".nijo", "logs", `${sessionId}.jsonl`)
}

function enqueueFileAppend(filePath: string, line: string): void {
  const previous = fileWriteChains.get(filePath) ?? Promise.resolve()
  const next = previous
    .then(async () => {
      await mkdir(path.dirname(filePath), { recursive: true })
      await appendFile(filePath, `${line}\n`, "utf8")
    })
    .catch(error => {
      // ここで serverLog.error() を呼ぶとトレース書き込み失敗の再帰になりうるため、標準エラーへ直接出す
      process.stderr.write(`[ServerLog] トレースの書き込みに失敗しました path=${filePath} error=${String(error)}\n`)
    })
  fileWriteChains.set(filePath, next)
}

/**
 * ログ出力の窓口。DevToolサーバー内のあらゆる場所から `import { serverLog } from "./ServerLog.ts"` で使う。
 *
 * - info/warn/error: 境界・節目・劣化・失敗の1行ログ。stdout(info) / stderr(warn,error) へ。LOG_LEVEL で絞り込める。
 * - conversation: 利用者に見える発話（ユーザー入力・AIの最終回答）。レベル制御・切り詰めをせず常に全文を stdout へ。
 * - trace: AIエージェント内部の全文（プロンプト・ツール引数・結果・サブエージェント問答）。
 *   stdout/stderr には出さず、ローカルでは .nijo/logs/<sessionId>.jsonl へ、fly.io では volume が無いため stdout へ出す。
 * - scoped: requestId・sessionId 等の相関フィールドを固定した子ロガーを作る。呼び出し側が明示的に引き回す
 *   （コンストラクタ注入にしない理由は、warn を出したい箇所が複数クラスに散らばっており、
 *   全クラスのコンストラクタを汚すのに見合わないため。設計規約「ログの置き場所は一律に定めない」に従う）。
 */
export class ServerLog {
  readonly #fields: LogFields

  constructor(fields: LogFields = {}) {
    this.#fields = fields
  }

  info(event: string, fields?: LogFields): void {
    this.#emit("info", event, fields)
  }

  warn(event: string, fields?: LogFields): void {
    this.#emit("warn", event, fields)
  }

  error(event: string, fields?: LogFields): void {
    this.#emit("error", event, fields)
  }

  conversation(event: string, fields: LogFields): void {
    const merged = prepareFields({ ...this.#fields, ...fields }, false)
    process.stdout.write(`${buildRecord("info", event, merged)}\n`)
  }

  trace(event: string, fields: LogFields): void {
    const merged = prepareFields({ ...this.#fields, ...fields }, false)
    const sessionId = typeof merged.sessionId === "string" ? merged.sessionId : "misc"
    const line = buildTraceRecord(event, merged)
    if (IS_FLY) {
      process.stdout.write(`${line}\n`)
    } else {
      enqueueFileAppend(traceFilePath(sessionId), line)
    }
  }

  scoped(fields: LogFields): ServerLog {
    return new ServerLog({ ...this.#fields, ...fields })
  }

  /** 画面表示用に、指定オフセット以降のサーバーログ増分を返す。 */
  readTail(fromOffset: number): LogIncrement {
    return ringBuffer.read(fromOffset)
  }

  /** 終了処理から呼ぶ。書き込み中のトレースファイルが完了するまで待つ。 */
  async flush(): Promise<void> {
    await Promise.allSettled([...fileWriteChains.values()])
  }

  #emit(level: Level, event: string, fields?: LogFields): void {
    if (LEVEL_ORDER[level] > LEVEL_ORDER[CONFIGURED_LEVEL]) return
    const merged = prepareFields({ ...this.#fields, ...fields }, true)
    const line = buildRecord(level, event, merged)
    const stream = level === "info" ? process.stdout : process.stderr
    stream.write(`${line}\n`)
  }
}

export const serverLog = new ServerLog()
