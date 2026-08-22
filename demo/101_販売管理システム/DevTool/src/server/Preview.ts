import path from "node:path"
import { setTimeout as delay } from "node:timers/promises"
import treeKillCallback from "tree-kill"
import { execa, type ResultPromise } from "execa"
import type { LogIncrement, PreviewProcessName, PreviewProcessState } from "../shared/devtool-api.ts"
import { LogBuffer } from "./LogBuffer.ts"
import { serverLog } from "./ServerLog.ts"

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

/** デバッグ対象アプリへの到達確認1回あたりのタイムアウト */
const PROBE_TIMEOUT_MS = 1_000

/** 1ストリームあたり保持する最大文字数。超えたら古い方から捨てる */
const MAX_BUFFER_LENGTH = 256 * 1024

/**
 * 並列起動するプロセス1件の定義。
 * cwd は Preview の projectRoot からの相対パス。
 */
export type PreviewProcessDefinition = {
  name: PreviewProcessName
  cwd: string
  fileName: string
  args: string[]
  /** true なら起動の度に既存ログへ追記、false なら起動の度にクリアしてから書く */
  appendStdout: boolean
  appendStderr: boolean
}

type RunningProcess = {
  name: string
  subprocess: ResultPromise
  stdout: LogBuffer
  stderr: LogBuffer
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
  readonly #targetOrigin: string
  readonly #running = new Map<string, RunningProcess>()

  /**
   * Start / Stop / Restart は複数ステップの非同期手続きであり、await のたびに
   * 他の呼び出しに割り込まれうる。二重起動・停止中の横入り起動を防ぐため、
   * この3メソッド全体を直列化する。
   * GetState / ReadLog はこれに参加しない（Stop の最大10秒のブロッキングに
   * 状態表示・ログ表示のポーリングまで巻き込まないため）。
   */
  #gate: Promise<unknown> = Promise.resolve()

  constructor(projectRoot: string, definitions: readonly PreviewProcessDefinition[], targetOrigin: string) {
    this.#projectRoot = projectRoot
    this.#definitions = definitions
    this.#targetOrigin = targetOrigin
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
  readLog(processName: string, stream: "stdout" | "stderr", fromOffset: number): LogIncrement {
    const running = this.#running.get(processName)
    if (!running) return { text: "", offset: 0 }
    return running[stream].read(fromOffset)
  }

  /**
   * デバッグ対象アプリのオリジンにHTTPで到達できるかを調べ、応答ステータスを返す。
   * 未起動やタイムアウトなど、到達できない場合は null を返す。
   */
  async probeTargetStatus(): Promise<number | null> {
    try {
      const res = await fetch(this.#targetOrigin, { signal: AbortSignal.timeout(PROBE_TIMEOUT_MS), redirect: "manual" })
      // ボディを読まないまま放置するとコネクションが溜まり続けるため、明示的に破棄する
      await res.body?.cancel()
      return res.status
    } catch {
      return null
    }
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

    const stdout = existing?.stdout ?? new LogBuffer(MAX_BUFFER_LENGTH)
    const stderr = existing?.stderr ?? new LogBuffer(MAX_BUFFER_LENGTH)
    if (!definition.appendStdout) stdout.clear()
    if (!definition.appendStderr) stderr.clear()

    serverLog.info("preview.start", { process: definition.name, cwd: definition.cwd, command: definition.fileName })

    const subprocess = execa(definition.fileName, definition.args, {
      cwd: path.join(this.#projectRoot, definition.cwd),
      // 色変更のエスケープシーケンスがログに混ざって読みづらくなるのを防ぐ
      env: { NO_COLOR: "1", FORCE_COLOR: "0" },
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
    // execa は reject: false を指定しているため、起動失敗（実行ファイルが無い等）を含めどんな終わり方でも
    // このPromiseは常に resolve する（reject は起きない。catch は書いても呼ばれない）。
    subprocess.then(result => {
      running.hasExited = true
      running.exitCode = result.exitCode ?? null
      // シグナルによる終了は stop()/restart() 等の意図した停止。それ以外の failed（非0終了・起動失敗など）だけを異常として警告する。
      if (result.failed && !result.signal) {
        serverLog.warn("preview.exit", { process: definition.name, exitCode: result.exitCode ?? null })
      }
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
