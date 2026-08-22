import { readdir, readFile, realpath } from "node:fs/promises"
import path from "node:path"
import matter from "gray-matter"
import type { ChangePlanDetail, ChangePlanSummary } from "../shared/devtool-api.ts"

/** ファイル名として安全な計画ID。ドット・スラッシュを含まないため親ディレクトリへ抜けられない。 */
const PLAN_ID_PATTERN = /^[A-Za-z0-9_-]+$/

/**
 * 変更計画（YAML frontmatter付きMarkdown）を読み取る。登録処理はこのクラスの責務ではない。
 */
export class ChangePlan {
  readonly #plansDir: string

  constructor(plansDir: string) {
    this.#plansDir = plansDir
  }

  /** 変更計画の一覧を、作成日時の新しい順（無ければファイル名順）で返す */
  async list(): Promise<ChangePlanSummary[]> {
    const fileNames = await readdir(this.#plansDir).catch(() => [])
    const ids = fileNames.filter(name => name.endsWith(".md")).map(name => name.slice(0, -3))

    const summaries: ChangePlanSummary[] = []
    for (const id of ids) {
      const detail = await this.read(id)
      if (detail) summaries.push(detail)
    }
    return summaries.sort((a, b) => (b.createdAt ?? "").localeCompare(a.createdAt ?? "") || a.id.localeCompare(b.id))
  }

  /** 指定IDの変更計画を本文込みで返す。存在しない・IDが不正な場合は null。 */
  async read(id: string): Promise<ChangePlanDetail | null> {
    if (!PLAN_ID_PATTERN.test(id)) return null

    const filePath = path.join(this.#plansDir, `${id}.md`)
    if (!(await this.#isInsidePlansDir(filePath))) return null

    const raw = await readFile(filePath, "utf8").catch(() => null)
    if (raw === null) return null

    const parsed = matter(raw)
    const title = typeof parsed.data.title === "string" ? parsed.data.title : id
    const status = typeof parsed.data.status === "string" ? parsed.data.status : "draft"
    const createdAt = parsed.data.createdAt instanceof Date
      ? parsed.data.createdAt.toISOString()
      : typeof parsed.data.createdAt === "string" ? parsed.data.createdAt : null

    return { id, title, status, createdAt, body: parsed.content.trim() }
  }

  /** シンボリックリンク経由での親ディレクトリ脱出を防ぐため、解決後のパスが計画ディレクトリ配下であることを確認する */
  async #isInsidePlansDir(filePath: string): Promise<boolean> {
    const resolvedPlansDir = await realpath(this.#plansDir).catch(() => null)
    if (resolvedPlansDir === null) return false
    const resolvedFilePath = await realpath(filePath).catch(() => null)
    if (resolvedFilePath === null) return false
    return resolvedFilePath.startsWith(resolvedPlansDir + path.sep)
  }
}
