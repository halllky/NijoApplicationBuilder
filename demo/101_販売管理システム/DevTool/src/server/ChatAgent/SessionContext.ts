import { readFile } from "node:fs/promises"
import path from "node:path"
import { XMLParser } from "fast-xml-parser"

/**
 * プロンプトに載せる可能性がある背景情報を全部詰めたもの。
 */
export type SessionContext = {
  /** 現在時刻 */
  currentTimeUtc: string
  /** 編集対象のアプリケーションの名前。nijo.xmlから採る。 */
  applicationName: string
  /** プロジェクトのフォルダ構成の概要。README.mdの「プロジェクト全体の構造」節から採る。主エージェント・ResearchAgent双方がどこを調べるべきかの手がかりに使う。 */
  projectStructureOverview: string
}

/** nijo.xml が読めない・ApplicationName属性が無い場合のフォールバック表示 */
const UNKNOWN_APPLICATION_NAME = "(nijo.xmlから取得できませんでした)"

/** README.md が読めない・該当節が無い場合のフォールバック表示 */
const UNKNOWN_PROJECT_STRUCTURE = "(README.mdから取得できませんでした)"

/** README.md 中でフォルダ構成の概要が書かれている節の見出し */
const PROJECT_STRUCTURE_HEADING = "## プロジェクト全体の構造"

/**
 * {@link SessionContext} を構築して返す。
 * nijo.xml・README.md は編集作業の途中で内容が変わりうるため、呼び出しの都度読み直す（キャッシュしない）。
 *
 * @param demo101Root 編集対象プロジェクトのルートディレクトリ
 */
export async function buildSessionContext(demo101Root: string): Promise<SessionContext> {
  return {
    currentTimeUtc: new Date().toISOString(),
    applicationName: await readApplicationName(demo101Root),
    projectStructureOverview: await readProjectStructureOverview(demo101Root),
  }
}

/** nijo.xml のルート要素（NijoAppScaffold）の ApplicationName 属性を読む */
async function readApplicationName(demo101Root: string): Promise<string> {
  const xmlPath = path.join(demo101Root, "nijo.xml")
  const raw = await readFile(xmlPath, "utf8").catch(() => null)
  if (raw === null) return UNKNOWN_APPLICATION_NAME

  const parser = new XMLParser({ ignoreAttributes: false, attributeNamePrefix: "@_" })
  const parsed: unknown = parser.parse(raw)
  const applicationName = (parsed as { NijoAppScaffold?: { "@_ApplicationName"?: unknown } })
    .NijoAppScaffold?.["@_ApplicationName"]

  return typeof applicationName === "string" && applicationName.length > 0
    ? applicationName
    : UNKNOWN_APPLICATION_NAME
}

/** README.md の「プロジェクト全体の構造」節（見出し自体は含まない、次の見出しの手前まで）を読む */
async function readProjectStructureOverview(demo101Root: string): Promise<string> {
  const readmePath = path.join(demo101Root, "README.md")
  const raw = await readFile(readmePath, "utf8").catch(() => null)
  if (raw === null) return UNKNOWN_PROJECT_STRUCTURE

  const headingIndex = raw.indexOf(PROJECT_STRUCTURE_HEADING)
  if (headingIndex === -1) return UNKNOWN_PROJECT_STRUCTURE

  const afterHeading = raw.slice(headingIndex + PROJECT_STRUCTURE_HEADING.length)
  const nextHeadingIndex = afterHeading.search(/\n## /)
  const section = nextHeadingIndex === -1 ? afterHeading : afterHeading.slice(0, nextHeadingIndex)

  return section.trim() || UNKNOWN_PROJECT_STRUCTURE
}
