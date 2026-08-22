import { readFile, readdir, realpath, stat } from "node:fs/promises"
import path from "node:path"

/** 一覧・閲覧の対象から除外するエントリ名。ビルド成果物・依存パッケージなど、AIエージェントが読んでも意味がないもの。 */
const IGNORED_ENTRY_NAMES = new Set([".git", ".nijo", "node_modules", "bin", "obj", "dist"])

/** grep の対象から除外する拡張子。テキストとして意味を持たないバイナリ形式。 */
const BINARY_EXTENSIONS = new Set([
  ".sqlite3", ".db", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".pdf", ".zip",
  ".dll", ".exe", ".woff", ".woff2", ".ttf", ".eot", ".binlog",
])

/** grep 1件あたりの読み取り対象から外すファイルサイズの上限（バイト）。巨大ファイルの走査でエージェントが固まるのを防ぐ。 */
const GREP_MAX_FILE_SIZE = 2 * 1024 * 1024

/** grep が返すマッチ件数の上限。これを超えたら打ち切り、`truncated: true` を付けて返す。 */
const GREP_MAX_HITS = 200

/** grep のマッチ1行分 */
export type GrepHit = {
  /** プロジェクトルートからの相対パス */
  path: string
  /** 1始まりの行番号 */
  line: number
  /** マッチした行の内容（前後の空白は除去） */
  text: string
}

/** grep の結果。上限に達して打ち切った場合は truncated が true になる。 */
export type GrepResult = {
  hits: GrepHit[]
  truncated: boolean
}

/**
 * デモ101プロジェクト配下のファイルを安全に読み取るためのクラス。
 * シンボリックリンク経由でのプロジェクトルート外への脱出を防ぐ（`ChangePlan` の検証と同じ考え方）。
 * 書き込みはこのクラスの責務ではない（読み取り専用）。
 */
export class ProjectFiles {
  readonly #root: string

  constructor(demo101Root: string) {
    this.#root = demo101Root
  }

  /** 指定ディレクトリ直下のエントリ名一覧を返す。ディレクトリ名末尾には "/" を付ける。ルート外・存在しない場合は null。 */
  async list(relativeDir: string): Promise<string[] | null> {
    const absoluteDir = path.join(this.#root, relativeDir)
    if (!(await this.#isInsideRoot(absoluteDir))) return null

    const entries = await readdir(absoluteDir, { withFileTypes: true }).catch(() => null)
    if (entries === null) return null

    return entries
      .filter(entry => !IGNORED_ENTRY_NAMES.has(entry.name))
      .map(entry => entry.isDirectory() ? `${entry.name}/` : entry.name)
      .sort()
  }

  /** 指定ファイルの内容をUTF-8として返す。ルート外・存在しない・ディレクトリの場合は null。 */
  async read(relativePath: string): Promise<string | null> {
    const absolutePath = path.join(this.#root, relativePath)
    if (!(await this.#isInsideRoot(absolutePath))) return null

    const stats = await stat(absolutePath).catch(() => null)
    if (stats === null || !stats.isFile()) return null

    return await readFile(absolutePath, "utf8").catch(() => null)
  }

  /**
   * 指定ディレクトリ配下を再帰的に走査し、query を含む行を返す（大文字小文字を区別しない部分一致）。
   * 識別子が日本語のプロジェクトのため、正規表現ではなく単純な部分一致にしている。
   * ルート外・存在しないディレクトリの場合は null。
   *
   * @param relativeDir 検索を開始するディレクトリのプロジェクトルートからの相対パス。ルート全体を見る場合は '.' を指定する。
   * @param extensions 指定時、このリストの拡張子（例: ['.cs', '.xml']）のファイルのみを対象にする。
   */
  async grep(query: string, relativeDir: string, extensions?: string[]): Promise<GrepResult | null> {
    const absoluteDir = path.join(this.#root, relativeDir)
    if (!(await this.#isInsideRoot(absoluteDir))) return null
    if (!(await stat(absoluteDir).catch(() => null))?.isDirectory()) return null

    const hits: GrepHit[] = []
    const needle = query.toLowerCase()
    let truncated = false

    const walk = async (absoluteCurrentDir: string): Promise<void> => {
      if (truncated) return
      const entries = await readdir(absoluteCurrentDir, { withFileTypes: true }).catch(() => [])
      for (const entry of entries) {
        if (truncated) return
        if (IGNORED_ENTRY_NAMES.has(entry.name)) continue

        const absoluteEntryPath = path.join(absoluteCurrentDir, entry.name)
        if (entry.isDirectory()) {
          await walk(absoluteEntryPath)
          continue
        }
        if (!entry.isFile()) continue
        if (BINARY_EXTENSIONS.has(path.extname(entry.name).toLowerCase())) continue
        if (extensions && !extensions.includes(path.extname(entry.name).toLowerCase())) continue

        const stats = await stat(absoluteEntryPath).catch(() => null)
        if (stats === null || stats.size > GREP_MAX_FILE_SIZE) continue

        const content = await readFile(absoluteEntryPath, "utf8").catch(() => null)
        if (content === null) continue

        const relativeEntryPath = path.relative(this.#root, absoluteEntryPath)
        const lines = content.split("\n")
        for (let i = 0; i < lines.length; i++) {
          if (!lines[i].toLowerCase().includes(needle)) continue
          hits.push({ path: relativeEntryPath, line: i + 1, text: lines[i].trim() })
          if (hits.length >= GREP_MAX_HITS) {
            truncated = true
            break
          }
        }
      }
    }
    await walk(absoluteDir)

    return { hits, truncated }
  }

  /** シンボリックリンク経由での親ディレクトリ脱出を防ぐため、解決後のパスがプロジェクトルート配下であることを確認する */
  async #isInsideRoot(absolutePath: string): Promise<boolean> {
    const resolvedRoot = await realpath(this.#root).catch(() => null)
    if (resolvedRoot === null) return false
    const resolvedPath = await realpath(absolutePath).catch(() => null)
    if (resolvedPath === null) return false
    return resolvedPath === resolvedRoot || resolvedPath.startsWith(resolvedRoot + path.sep)
  }
}
