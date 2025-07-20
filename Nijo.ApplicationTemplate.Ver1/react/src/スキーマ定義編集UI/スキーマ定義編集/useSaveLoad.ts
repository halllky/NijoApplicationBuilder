import React from "react"
import useEvent from "react-use-event-hook"
import { SERVER_DOMAIN } from "../../routes"
import { SchemaDefinitionGlobalState } from "./types"

/**
 * サーバーに問い合わせて nijo.xml の読み込み、保存を行う。
 */
export const useSaveLoad = () => {
  const [schema, setSchema] = React.useState<SchemaDefinitionGlobalState>()
  const [loadError, setLoadError] = React.useState<string>()

  // 画面初期表示時、サーバーからスキーマ情報を読み込む
  const reloadSchema = useEvent(async () => {
    setLoadError(undefined)
    setSchema(undefined)
    try {
      const schemaResponse = await fetch(`${SERVER_DOMAIN}/load`)

      if (!schemaResponse.ok) {
        const body = await schemaResponse.text();
        throw new Error(`Failed to load schema: ${schemaResponse.status} ${body}`);
      }

      const schemaData: SchemaDefinitionGlobalState = await schemaResponse.json()
      setSchema(schemaData)
    } catch (error) {
      console.error(error)
      setLoadError(error instanceof Error ? error.message : `不明なエラー(${error})`)
    }
  })
  React.useEffect(() => {
    reloadSchema()
  }, [reloadSchema])

  // 保存処理
  const saveSchema = useEvent(async (valuesToSave: SchemaDefinitionGlobalState): Promise<{ ok: boolean, error?: string }> => {
    try {
      const response = await fetch(`${SERVER_DOMAIN}/save`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(valuesToSave),
      })
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
      return { ok: true }
    } catch (error) {
      console.error(error)
      return { ok: false, error: error instanceof Error ? error.message : `不明なエラー(${error})` }
    }
  })

  return {
    schema,
    loadError,
    reloadSchema,
    saveSchema,
  }
}