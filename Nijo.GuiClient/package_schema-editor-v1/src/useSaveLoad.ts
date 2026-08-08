import { SERVER_DOMAIN } from "./main"
import { AppSchemaDefinitionGraphDataSet, ApplicationState } from "./types"
import { NIJOUI_CLIENT_ROUTE_PARAMS } from "./routing"
import { demoFetchHeaders } from "./DemoMode/clientId"

type LoadSchemaReturn =
  | { ok: true; schema: { applicationState: ApplicationState, schemaGraphViewState: AppSchemaDefinitionGraphDataSet | null } }
  | { ok: false; error?: string; }

/**
 * サーバーに問い合わせて nijo.xml の内容とスキーマ編集用の情報を読み込む
 */
export const loadSchema = async (projectDir: string | null, signal: AbortSignal): Promise<LoadSchemaReturn> => {
  try {
    const schemaResponse = await fetch(
      `${SERVER_DOMAIN}/api/load?${NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR}=${encodeURIComponent(projectDir ?? '')}`,
      { signal }
    )

    if (!schemaResponse.ok) {
      const body = await schemaResponse.text();
      throw new Error(`Failed to load schema: ${schemaResponse.status} ${body}`);
    }

    const responseData: { applicationState: ApplicationState, schemaGraphViewState: AppSchemaDefinitionGraphDataSet | null } = await schemaResponse.json()
    if (signal.aborted) return { ok: false }

    // schemaGraphViewStateをapplicationStateに含める
    const schema: ApplicationState = {
      ...responseData.applicationState,
      schemaGraphViewState: responseData.schemaGraphViewState,
    }

    return { ok: true, schema: { applicationState: schema, schemaGraphViewState: responseData.schemaGraphViewState } }

  } catch (error) {
    if (error instanceof Error && error.name === 'AbortError') {
      return { ok: false }
    }
    console.error(error)
    const msg = error instanceof Error ? error.message : `不明なエラー(${error})`
    return { ok: false, error: msg }
  }
}

/**
 * 画面上で編集した情報を送信し、サーバー側で nijo.xml の内容を更新する
 */
export const saveSchema = async (
  projectDir: string | null,
  applicationState: ApplicationState,
  schemaGraphViewState: AppSchemaDefinitionGraphDataSet | null,
  generateCode: boolean = false,
): Promise<{ ok: boolean, error?: string }> => {
  try {
    const response = await fetch(`${SERVER_DOMAIN}/api/save?${NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR}=${encodeURIComponent(projectDir ?? '')}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', ...demoFetchHeaders() },
      body: JSON.stringify({
        applicationState,
        schemaGraphViewState,
      }),
    })
    if (response.status === 423) {
      const body = await response.json().catch(() => null)
      return { ok: false, error: body?.message ?? '他のユーザーが編集中のため保存できません' }
    }
    if (!response.ok) {
      const bodyText = await response.text()
      try {
        const bodyJson = JSON.parse(bodyText) as string[]
        console.error(bodyJson)
        return { ok: false, error: `保存に失敗しました:\n${bodyJson.join('\n')}` }
      } catch {
        console.error(bodyText)
        return { ok: false, error: `保存に失敗しました (サーバーからの応答が不正です):\n${bodyText}` }
      }
    }

    // コード生成が有効な場合は実行
    if (generateCode) {
      try {
        const generateResponse = await fetch(`${SERVER_DOMAIN}/api/generate?${NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR}=${encodeURIComponent(projectDir ?? '')}`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', ...demoFetchHeaders() },
        })
        if (generateResponse.status === 423) {
          const body = await generateResponse.json().catch(() => null)
          return { ok: false, error: body?.message ?? '他のユーザーが編集中のためコード生成できません' }
        }
        if (!generateResponse.ok) {
          const bodyText = await generateResponse.text()
          console.error(bodyText)
          return { ok: false, error: `コード生成に失敗しました:\n${bodyText}` }
        }
      } catch (error) {
        console.error(error)
        return { ok: false, error: error instanceof Error ? `コード生成に失敗しました: ${error.message}` : `コード生成に失敗しました: 不明なエラー(${error})` }
      }
    }

    return { ok: true }
  } catch (error) {
    console.error(error)
    return { ok: false, error: error instanceof Error ? error.message : `不明なエラー(${error})` }
  }
}
