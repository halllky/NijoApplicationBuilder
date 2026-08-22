import { readFile, readdir, realpath, stat } from "node:fs/promises"
import { tool } from "ai"
import { z } from "zod"
import path from "node:path"
import { serverLog } from "./ServerLog.ts"

/** 一覧・検索の対象から除外するエントリ名。ビルド成果物・依存パッケージなど、AIエージェントが読んでも意味がないもの。 */
const IGNORED_ENTRY_NAMES = new Set([".git", ".nijo", "node_modules", "bin", "obj", "dist"])

/** 内容検索の対象から除外する拡張子。テキストとして意味を持たないバイナリ形式。 */
const BINARY_EXTENSIONS = new Set([
  ".sqlite3", ".db", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".pdf", ".zip",
  ".dll", ".exe", ".woff", ".woff2", ".ttf", ".eot", ".binlog",
])

/** 内容検索1件あたりの読み取り対象から外すファイルサイズの上限（バイト）。巨大ファイルの走査でエージェントが固まるのを防ぐ。 */
const SEARCH_MAX_FILE_SIZE = 2 * 1024 * 1024

/** search_files が1件の検索条件あたりに返すマッチファイル数の上限。これを超えたら打ち切り、`truncated: true` を付けて返す。 */
const SEARCH_MAX_FILES = 100

/** search_files が1件の検索条件あたりに返すマッチ行数の上限。 */
const SEARCH_MAX_HITS = 200

/** search_files が1回のツールコールで受け取れる検索条件の最大数。まとめて渡すほどステップ数を節約できる。 */
export const SEARCH_MAX_COUNT = 10

/** read_file が1回のツールコールで受け取れるファイルパスの最大数。 */
export const READ_MAX_FILES = 10

/** read_file が1ファイルあたりに返す文字数の上限。超過分は打ち切り、その旨を戻り値に含める。 */
const READ_MAX_CHARS = 60_000

/** 内容検索のマッチ1行分 */
export type GrepHit = {
  /** プロジェクトルートからの相対パス */
  path: string
  /** 1始まりの行番号 */
  line: number
  /** マッチした行の内容（前後の空白は除去） */
  text: string
}

/** search_files 1件分の検索条件。いずれも省略可だが、最低1つは指定すること（さもないと単なる全件列挙になる）。 */
export type FileSearchInput = {
  /** 検索を開始するディレクトリのプロジェクトルートからの相対パス。省略時はプロジェクト全体。 */
  path?: string
  /** ファイルパス（プロジェクトルートからの相対パス）に対する部分一致条件。大文字小文字を区別しない。 */
  namePattern?: string
  /** ファイル内容に対する部分一致条件（正規表現ではない）。大文字小文字を区別しない。 */
  query?: string
  /** 拡張子で絞り込みたい場合に指定する（例: ['.cs', '.xml']）。 */
  extensions?: string[]
}

/** search_files 1件分の検索結果。 */
export type FileSearchResult = {
  /** どの条件に対する結果かを対応づけられるよう、入力をそのまま返す */
  search: FileSearchInput
  /** namePattern・query・extensions のすべてに合致したファイルのパス一覧 */
  files: string[]
  /** query 指定時のみ。合致した行 */
  hits?: GrepHit[]
  /** files・hits のいずれかが上限に達して打ち切られた場合 true */
  truncated: boolean
  /** 指定されたディレクトリが見つからない場合などに設定する */
  error?: string
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

  /** 指定ファイルの内容をUTF-8として返す。ルート外・存在しない・ディレクトリの場合は null。 */
  async read(relativePath: string): Promise<string | null> {
    const absolutePath = path.join(this.#root, relativePath)
    if (!(await this.#isInsideRoot(absolutePath))) return null

    const stats = await stat(absolutePath).catch(() => null)
    if (stats === null || !stats.isFile()) return null

    return await readFile(absolutePath, "utf8").catch(() => null)
  }

  /**
   * 指定ディレクトリ配下を再帰的に走査し、渡された条件すべてに合致するファイルを1回で探す。
   * namePattern はファイルパス（相対パス全体）に対する部分一致、query はファイル内容に対する部分一致で、
   * どちらも識別子が日本語のプロジェクトのため正規表現ではなく単純な部分一致にしている。
   * query を指定した場合、ファイルは実際にマッチする行を含む場合のみ files に含まれる。
   * ルート外・存在しないディレクトリの場合は error を設定して返す。
   */
  async search(input: FileSearchInput): Promise<FileSearchResult> {
    const relativeDir = input.path ?? "."
    const absoluteDir = path.join(this.#root, relativeDir)
    const isValidDir = (await this.#isInsideRoot(absoluteDir))
      && (await stat(absoluteDir).catch(() => null))?.isDirectory()
    if (!isValidDir) {
      return { search: input, files: [], truncated: false, error: `ディレクトリが見つかりません: ${relativeDir}` }
    }

    const namePattern = input.namePattern?.toLowerCase()
    const queryNeedle = input.query?.toLowerCase()

    const files: string[] = []
    const hits: GrepHit[] = []
    let filesTruncated = false
    let hitsTruncated = false
    const shouldStop = (): boolean => filesTruncated && (queryNeedle === undefined || hitsTruncated)

    const addFile = (relativeEntryPath: string): void => {
      if (filesTruncated) return
      files.push(relativeEntryPath)
      if (files.length >= SEARCH_MAX_FILES) filesTruncated = true
    }

    const walk = async (absoluteCurrentDir: string): Promise<void> => {
      if (shouldStop()) return
      const entries = await readdir(absoluteCurrentDir, { withFileTypes: true }).catch(() => [])
      for (const entry of entries) {
        if (shouldStop()) return
        if (IGNORED_ENTRY_NAMES.has(entry.name)) continue

        const absoluteEntryPath = path.join(absoluteCurrentDir, entry.name)
        if (entry.isDirectory()) {
          await walk(absoluteEntryPath)
          continue
        }
        if (!entry.isFile()) continue
        if (input.extensions && !input.extensions.includes(path.extname(entry.name).toLowerCase())) continue

        const relativeEntryPath = path.relative(this.#root, absoluteEntryPath)
        if (namePattern && !relativeEntryPath.toLowerCase().includes(namePattern)) continue

        if (queryNeedle === undefined) {
          addFile(relativeEntryPath)
          continue
        }

        // 内容検索が必要な場合のみファイルを読む
        if (BINARY_EXTENSIONS.has(path.extname(entry.name).toLowerCase())) continue
        const stats = await stat(absoluteEntryPath).catch(() => null)
        if (stats === null || stats.size > SEARCH_MAX_FILE_SIZE) continue
        const content = await readFile(absoluteEntryPath, "utf8").catch(() => null)
        if (content === null) continue

        let matchedInFile = false
        const lines = content.split("\n")
        for (let i = 0; i < lines.length; i++) {
          if (!lines[i].toLowerCase().includes(queryNeedle)) continue
          matchedInFile = true
          if (!hitsTruncated) {
            hits.push({ path: relativeEntryPath, line: i + 1, text: lines[i].trim() })
            if (hits.length >= SEARCH_MAX_HITS) hitsTruncated = true
          }
        }
        if (matchedInFile) addFile(relativeEntryPath)
      }
    }
    await walk(absoluteDir)

    const truncated = filesTruncated || hitsTruncated
    if (truncated) serverLog.warn("files.search.truncated", { search: input, files: files.length, hits: hits.length })

    return { search: input, files, hits: queryNeedle === undefined ? undefined : hits, truncated }
  }

  /**
   * シンボリックリンク経由での親ディレクトリ脱出を防ぐため、解決後のパスがプロジェクトルート配下であることを確認する。
   * 解決に失敗する（＝単に存在しないパス）ことはAIがファイル名を推測して外れる際に日常的に起きるため警告にしない。
   * 実在するのにルート外を指している場合だけを脱出の兆候として warn する。
   */
  async #isInsideRoot(absolutePath: string): Promise<boolean> {
    const resolvedRoot = await realpath(this.#root).catch(() => null)
    if (resolvedRoot === null) return false
    const resolvedPath = await realpath(absolutePath).catch(() => null)
    if (resolvedPath === null) return false

    const inside = resolvedPath === resolvedRoot || resolvedPath.startsWith(resolvedRoot + path.sep)
    if (!inside) serverLog.warn("files.escape", { path: absolutePath, resolved: resolvedPath })
    return inside
  }

  /**
   * AIエージェント向けのファイル操作ツールを構築して返す。
   */
  buildAiTools(): { [toolName: string]: ReturnType<typeof tool<any, any, any>> } {
    return {
      search_files: tool({
        description: "プロジェクト内を再帰的に検索する。path・namePattern・query・extensionsの組み合わせで、ファイルパスの部分一致検索とファイル内容の部分一致検索の両方（または片方）を1回で行える。"
          + " query・namePatternはいずれも正規表現ではなく単純な部分一致（大文字小文字を区別しない）。"
          + " 調べたいことが複数ある場合は、呼び出しを繰り返さず searches に複数条件をまとめて渡すこと。"
          + " 結果が多すぎる場合は条件ごとに打ち切られる（truncated）ため、必要なら path・namePattern・extensions で絞り込むこと。",
        inputSchema: z.object({
          searches: z.array(z.object({
            path: z.string().optional().describe("検索を開始するディレクトリのプロジェクトルートからの相対パス。省略時はプロジェクト全体を対象にする。"),
            namePattern: z.string().optional().describe("ファイルパスに対する部分一致条件。ファイル名の一部が分かっているときに使う。"),
            query: z.string().optional().describe("ファイル内容に対する部分一致条件。正規表現は使えない。"),
            extensions: z.array(z.string()).optional().describe("拡張子で絞り込みたい場合に指定する（例: ['.cs', '.xml']）。"),
          })).min(1).max(SEARCH_MAX_COUNT).describe("検索条件の一覧。調べたいことが複数あるときは、1回の呼び出しにまとめて渡すこと。"),
        }),
        execute: async ({ searches }) => await Promise.all(searches.map(search => this.search(search))),
      }),
      read_file: tool({
        description: "プロジェクト内の指定ファイル（複数可）の内容をテキストとして返す。読みたいファイルが複数ある場合は、呼び出しを繰り返さず paths にまとめて渡すこと。",
        inputSchema: z.object({
          paths: z.array(z.string()).min(1).max(READ_MAX_FILES).describe("読みたいファイルのプロジェクトルートからの相対パスの一覧。"),
        }),
        execute: async ({ paths }) => await Promise.all(paths.map(async relativePath => {
          const content = await this.read(relativePath)
          if (content === null) return { path: relativePath, error: `ファイルが見つかりません: ${relativePath}` }
          if (content.length <= READ_MAX_CHARS) return { path: relativePath, content }
          return {
            path: relativePath,
            content: content.slice(0, READ_MAX_CHARS),
            truncated: true,
          }
        })),
      }),
    }
  }
}
