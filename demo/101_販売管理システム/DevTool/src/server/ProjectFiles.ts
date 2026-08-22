import { readFile, readdir, realpath, stat } from "node:fs/promises"
import path from "node:path"

/** 一覧・閲覧の対象から除外するエントリ名。ビルド成果物・依存パッケージなど、AIエージェントが読んでも意味がないもの。 */
const IGNORED_ENTRY_NAMES = new Set([".git", ".nijo", "node_modules", "bin", "obj", "dist"])

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

  /** シンボリックリンク経由での親ディレクトリ脱出を防ぐため、解決後のパスがプロジェクトルート配下であることを確認する */
  async #isInsideRoot(absolutePath: string): Promise<boolean> {
    const resolvedRoot = await realpath(this.#root).catch(() => null)
    if (resolvedRoot === null) return false
    const resolvedPath = await realpath(absolutePath).catch(() => null)
    if (resolvedPath === null) return false
    return resolvedPath === resolvedRoot || resolvedPath.startsWith(resolvedRoot + path.sep)
  }
}
