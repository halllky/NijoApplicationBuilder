import { SERVER_DOMAIN } from "../main"
import { NIJOUI_CLIENT_ROUTE_PARAMS } from "../routing"
import { EditingProject, SchemaEditorRule, ValidationErrorMap } from "./types"

/** データを伴って成功しうる呼び出しの結果。 */
export type LoadResult<T> =
  | { ok: true; value: T }
  | { ok: false; error?: string }

/** 成否のみを返す呼び出しの結果。 */
export type ActionResult =
  | { ok: true }
  | { ok: false; error?: string }

/**
 * サーバーに問い合わせて nijo.xml の内容とスキーマ編集用の情報を読み込む。
 */
export const loadProject = async (projectDir: string | null, signal: AbortSignal): Promise<LoadResult<EditingProject>> => {
  try {
    const response = await fetch(
      `${SERVER_DOMAIN}/nijo-api/load?${NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR}=${encodeURIComponent(projectDir ?? '')}`,
      { signal },
    )
    if (!response.ok) {
      return { ok: false, error: await describeErrorResponse(response, '読み込みに失敗しました') }
    }
    const value: EditingProject = await response.json()
    return { ok: true, value }

  } catch (error) {
    return toErrorResult(error)
  }
}

/**
 * スキーマ編集画面が必要とする、プロジェクトに依存しない固定ルールを読み込む。
 */
export const loadSchemaRule = async (signal: AbortSignal): Promise<LoadResult<SchemaEditorRule>> => {
  try {
    const response = await fetch(`${SERVER_DOMAIN}/nijo-api/schema-rule`, { signal })
    if (!response.ok) {
      return { ok: false, error: await describeErrorResponse(response, '読み込みに失敗しました') }
    }
    const value: SchemaEditorRule = await response.json()
    return { ok: true, value }

  } catch (error) {
    return toErrorResult(error)
  }
}

/**
 * 画面上で編集した情報を送信し、サーバー側で nijo.xml の内容を更新する。
 */
export const saveProject = async (projectDir: string | null, project: EditingProject): Promise<ActionResult> => {
  try {
    const response = await fetch(`${SERVER_DOMAIN}/nijo-api/save?${NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR}=${encodeURIComponent(projectDir ?? '')}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(project),
    })
    if (!response.ok) {
      return { ok: false, error: await describeErrorResponse(response, '保存に失敗しました') }
    }
    return { ok: true }

  } catch (error) {
    return toErrorResult(error)
  }
}

/**
 * 編集中の内容をサーバーに送って検証する。
 * エラーが1件もない場合は空のマップを返す（検証自体が失敗した場合とは区別される）。
 */
export const validateProject = async (projectDir: string | null, project: EditingProject, signal: AbortSignal): Promise<LoadResult<ValidationErrorMap>> => {
  try {
    const response = await fetch(`${SERVER_DOMAIN}/nijo-api/validate?${NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR}=${encodeURIComponent(projectDir ?? '')}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(project),
      signal,
    })
    // ステータスコード202はエラーあり。200はエラーなし。
    if (response.status === 202) {
      const value: ValidationErrorMap = await response.json()
      return { ok: true, value }
    }
    if (!response.ok) {
      return { ok: false, error: await describeErrorResponse(response, '検証に失敗しました') }
    }
    return { ok: true, value: {} }

  } catch (error) {
    return toErrorResult(error)
  }
}

/**
 * nijo.xml の内容をもとにソースコードの自動生成を実行する。
 */
export const generateCode = async (projectDir: string | null): Promise<ActionResult> => {
  try {
    const response = await fetch(`${SERVER_DOMAIN}/nijo-api/generate?${NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR}=${encodeURIComponent(projectDir ?? '')}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
    })
    if (!response.ok) {
      return { ok: false, error: await describeErrorResponse(response, 'コード生成に失敗しました') }
    }
    return { ok: true }

  } catch (error) {
    return toErrorResult(error)
  }
}

/**
 * 現在編集中の内容をもとに、XML要素の種類（Type属性）の入力候補を取得する。
 */
export const fetchTypeCandidates = async (
  projectDir: string | null,
  project: EditingProject,
  signal: AbortSignal,
): Promise<LoadResult<{ value: string, text: string }[]>> => {
  try {
    // SERVER_DOMAIN は本番ビルドでは空文字（同一オリジンへの相対パス）になるため、
    // ベースURLを要求する `new URL()` ではなく文字列連結でURLを組み立てる。
    const response = await fetch(`${SERVER_DOMAIN}/nijo-api/types?${NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR}=${encodeURIComponent(projectDir ?? '')}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(project),
      signal,
    })
    if (!response.ok) {
      return { ok: false, error: await describeErrorResponse(response, '候補の取得に失敗しました') }
    }
    const value: { value: string, text: string }[] = await response.json()
    return { ok: true, value }

  } catch (error) {
    return toErrorResult(error)
  }
}

// ---------------------------------

/**
 * 正常系以外のレスポンスからエラーメッセージを組み立てる。
 */
const describeErrorResponse = async (response: Response, summary: string): Promise<string> => {
  const bodyText = await response.text()
  try {
    const bodyJson = JSON.parse(bodyText) as string[]
    return `${summary}:\n${bodyJson.join('\n')}`
  } catch {
    return `${summary} (${response.status}):\n${bodyText}`
  }
}

/**
 * fetch失敗時の例外を結果オブジェクトに変換する。
 * ユーザー操作等によるAbortはエラーとして扱わない。
 */
const toErrorResult = (error: unknown): { ok: false; error?: string } => {
  if (error instanceof Error && error.name === 'AbortError') {
    return { ok: false }
  }
  console.error(error)
  const message = error instanceof Error ? error.message : `不明なエラー(${error})`
  return { ok: false, error: message }
}
