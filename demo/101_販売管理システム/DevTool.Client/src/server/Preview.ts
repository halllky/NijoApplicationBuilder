import path from "node:path"
import { setTimeout as delay } from "node:timers/promises"
import treeKillCallback from "tree-kill"
import { execa, type ResultPromise } from "execa"
import type { PreviewLogIncrement, PreviewProcessState } from "../shared/devtool-api.ts"

/** tree-kill はコールバックAPIのみを提供するため、Promise化して使う */
function killTree(pid: number, signal: string): Promise<void> {
  return new Promise((resolve, reject) => {
    treeKillCallback(pid, signal, error => {
      if (error) reject(error)
      else resolve()
    })
  })
}

/** 停止要求からプロセスツリーの終了を待つ上限時間。超えたら強制終了へ切り替える */
const STOP_TIMEOUT_MS = 10_000

/** 1ストリームあたり保持する最大文字数。超えたら古い方から捨てる */
const MAX_BUFFER_LENGTH = 256 * 1024

/**
 * 並列起動するプロセス1件の定義。
 * cwd は Preview の projectRoot からの相対パス。
 */
export type PreviewProcessDefinition = {
  name: string
  cwd: string
  fileName: string
  args: string[]
  /** true なら起動の度に既存ログへ追記、false なら起動の度にクリアしてから書く */
  appendStdout: boolean
  appendStderr: boolean
}

/**
 * 1プロセス・1ストリーム分の出力を保持する。ログはファイルに書かず、この中にのみ保持する。
 * 返す offset は「これまでに受け取った文字数の累計」であり、
 * バッファの先頭を捨てても増加し続ける（呼び出し側が持つ既読位置と整合させるため）。
 */
class OutputBuffer {
  #text = ""
  #totalLength = 0
  #droppedLength = 0

  append(chunk: string): void {
    this.#text += chunk
    this.#totalLength += chunk.length
    if (this.#text.length > MAX_BUFFER_LENGTH) {
      const dropped = this.#text.length - MAX_BUFFER_LENGTH
      this.#text = this.#text.slice(dropped)
      this.#droppedLength += dropped
    }
  }

  /** 起動しなおしでログをクリアする。以降 offset は 0 から数えなおす */
  clear(): void {
    this.#text = ""
    this.#totalLength = 0
    this.#droppedLength = 0
  }

  /**
   * 指定オフセット以降の増分を返す。
   * オフセットが累計より大きい場合（クリアされた場合）は先頭から読み直す。
   */
  read(fromOffset: number): PreviewLogIncrement {
    const offset = fromOffset > this.#totalLength ? 0 : fromOffset
    const start = Math.max(offset, this.#droppedLength) - this.#droppedLength
    return { text: this.#text.slice(start), offset: this.#totalLength }
  }
}

type RunningProcess = {
  name: string
  subprocess: ResultPromise
  stdout: OutputBuffer
  stderr: OutputBuffer
  hasExited: boolean
  exitCode: number | null
}

/**
 * 開発中のアプリケーションを実際に起動して動かしている状態。
 * コンストラクタで受け取ったプロセス群を起動・停止する。
 */
export class Preview {
  readonly #projectRoot: string
  readonly #definitions: readonly PreviewProcessDefinition[]
  readonly #running = new Map<string, RunningProcess>()

  /**
   * Start / Stop / Restart は複数ステップの非同期手続きであり、await のたびに
   * 他の呼び出しに割り込まれうる。二重起動・停止中の横入り起動を防ぐため、
   * この3メソッド全体を直列化する。
   * GetState / ReadLog はこれに参加しない（Stop の最大10秒のブロッキングに
   * 状態表示・ログ表示のポーリングまで巻き込まないため）。
   */
  #gate: Promise<unknown> = Promise.resolve()

  constructor(projectRoot: string, definitions: readonly PreviewProcessDefinition[]) {
    this.#projectRoot = projectRoot
    this.#definitions = definitions
  }

  /**
   * プロセスを起動する。既に起動中のプロセスはスキップする。
   * processName を指定した場合はそのプロセスのみ、未指定なら全プロセスを対象とする。
   */
  async start(processName?: string): Promise<void> {
    await this.#exclusive(async () => {
      for (const definition of this.#targetDefinitions(processName)) {
        this.#startOne(definition)
      }
    })
  }

  /**
   * 起動中のプロセスをツリーごと停止する。
   * processName を指定した場合はそのプロセスのみ、未指定なら全プロセスを対象とする。
   */
  async stop(processName?: string): Promise<void> {
    await this.#exclusive(async () => {
      for (const definition of this.#targetDefinitions(processName)) {
        await this.#stopOne(definition.name)
      }
    })
  }

  /**
   * プロセスを停止してから起動しなおす。
   * processName を指定した場合はそのプロセスのみ、未指定なら全プロセスを対象とする。
   */
  async restart(processName?: string): Promise<void> {
    await this.#exclusive(async () => {
      for (const definition of this.#targetDefinitions(processName)) {
        await this.#stopOne(definition.name)
        this.#startOne(definition)
      }
    })
  }

  /** 起動中の各プロセスの稼働状態を返す。一度も起動したことのないプロセスは含まない。 */
  getState(): PreviewProcessState[] {
    return [...this.#running.values()].map(running => ({
      name: running.name,
      isRunning: !running.hasExited,
      processId: running.hasExited ? null : running.subprocess.pid ?? null,
      exitCode: running.hasExited ? running.exitCode : null,
    }))
  }

  /** 指定プロセスの指定ストリームの、指定オフセット以降の増分を読む */
  readLog(processName: string, stream: "stdout" | "stderr", fromOffset: number): PreviewLogIncrement {
    const running = this.#running.get(processName)
    if (!running) return { text: "", offset: 0 }
    return running[stream].read(fromOffset)
  }

  /** アプリケーション終了時（Ctrl+Cを含む）に稼働中のプロセスを確実に停止する */
  async stopAll(): Promise<void> {
    await this.stop()
  }

  #targetDefinitions(processName?: string): PreviewProcessDefinition[] {
    return processName === undefined
      ? [...this.#definitions]
      : this.#definitions.filter(d => d.name === processName)
  }

  #exclusive<T>(action: () => Promise<T>): Promise<T> {
    const result = this.#gate.then(action, action)
    this.#gate = result.then(() => undefined, () => undefined)
    return result
  }

  #startOne(definition: PreviewProcessDefinition): void {
    // 既存エントリが「稼働中」でなければ（自発終了直後で後始末が済んでいない場合を含む）起動する。
    const existing = this.#running.get(definition.name)
    if (existing && !existing.hasExited) return

    const stdout = existing?.stdout ?? new OutputBuffer()
    const stderr = existing?.stderr ?? new OutputBuffer()
    if (!definition.appendStdout) stdout.clear()
    if (!definition.appendStderr) stderr.clear()

    const subprocess = execa(definition.fileName, definition.args, {
      cwd: path.join(this.#projectRoot, definition.cwd),
      buffer: false,
      reject: false,
      cleanup: true,
      stdin: "ignore",
      windowsHide: true,
    })

    const running: RunningProcess = { name: definition.name, subprocess, stdout, stderr, hasExited: false, exitCode: null }
    subprocess.stdout?.setEncoding("utf8")
    subprocess.stdout?.on("data", (chunk: string) => stdout.append(chunk))
    subprocess.stderr?.setEncoding("utf8")
    subprocess.stderr?.on("data", (chunk: string) => stderr.append(chunk))
    subprocess
      .then(result => {
        running.hasExited = true
        running.exitCode = result.exitCode ?? null
      })
      .catch(() => {
        running.hasExited = true
      })

    this.#running.set(definition.name, running)
  }

  async #stopOne(name: string): Promise<void> {
    const running = this.#running.get(name)
    if (!running) return
    if (running.hasExited || running.subprocess.pid === undefined) {
      this.#running.delete(name)
      return
    }

    // Windows の taskkill は常に強制終了になるため、シグナル指定はPOSIXでのみ意味を持つ
    await killTree(running.subprocess.pid, "SIGTERM").catch(() => { })
    const timedOut = await Promise.race([
      running.subprocess.then(() => false).catch(() => false),
      delay(STOP_TIMEOUT_MS).then(() => true),
    ])
    if (timedOut && !running.hasExited) {
      await killTree(running.subprocess.pid, "SIGKILL").catch(() => { })
    }
    this.#running.delete(name)
  }
}
