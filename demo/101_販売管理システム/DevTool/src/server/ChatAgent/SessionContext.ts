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
}

/** nijo.xml が読めない・ApplicationName属性が無い場合のフォールバック表示 */
const UNKNOWN_APPLICATION_NAME = "(nijo.xmlから取得できませんでした)"

/**
 * {@link SessionContext} を構築して返す。
 * nijo.xml は編集作業の途中で内容が変わりうるため、呼び出しの都度読み直す（キャッシュしない）。
 *
 * @param demo101Root 編集対象プロジェクトのルートディレクトリ
 */
export async function buildSessionContext(demo101Root: string): Promise<SessionContext> {
  return {
    currentTimeUtc: new Date().toISOString(),
    applicationName: await readApplicationName(demo101Root),
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
