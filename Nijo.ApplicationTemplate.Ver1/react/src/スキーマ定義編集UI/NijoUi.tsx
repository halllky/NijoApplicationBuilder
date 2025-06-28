import * as React from "react"
import * as ReactHookForm from "react-hook-form"
import * as ReactRouter from "react-router-dom"
import * as Layout from "../layout"
import * as Input from "../input"
import useEvent from "react-use-event-hook"
import { NijoUiOutletContextType, SchemaDefinitionGlobalState } from "./types"
import { AttrDefsProvider } from "./スキーマ定義編集/AttrDefContext"
import { SERVER_DOMAIN } from "../routes"
import { useValidationContextProvider, ValidationContext } from "./スキーマ定義編集/ValidationContext"
import { useTypedDocumentContextProvider } from "./型つきドキュメント/TypedDocumentContext"

/**
 * nijo.xmlをUIで編集できる画面の試作。
 * 最終的には、独立した WebView2 アプリケーションとして分離する予定。
 */
export const NijoUi = () => {

  // 画面初期表示時、サーバーからスキーマ情報を読み込む
  const [schema, setSchema] = React.useState<SchemaDefinitionGlobalState>()
  const [loadError, setLoadError] = React.useState<string>()
  const load = useEvent(async () => {
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
    load()
  }, [load])

  // 保存処理
  const handleSave = useEvent(async (valuesToSave: SchemaDefinitionGlobalState) => {
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
          window.alert(`保存に失敗しました:\n${bodyJson.join('\n')}`)
        } catch {
          console.error(bodyText)
          window.alert(`保存に失敗しました (サーバーからの応答が不正です):\n${bodyText}`)
        }
        return
      }
      window.alert('保存に成功しました')
    } catch (error) {
      console.error(error)
      window.alert(`保存に失敗しました: ${error instanceof Error ? error.message : `不明なエラー(${error})`}`)
    }
  })

  // 読み込み中
  if (schema === undefined && loadError === undefined) {
    return <Layout.NowLoading />
  }

  // 読み込み完了
  if (schema !== undefined) {
    // NijoUiSideMenu に渡す SchemaDefinitionGlobalState 部分を抽出
    const schemaDefinitionPart: SchemaDefinitionGlobalState = {
      xmlElementTrees: schema.xmlElementTrees,
      attributeDefs: schema.attributeDefs,
      valueMemberTypes: schema.valueMemberTypes,
    };
    return (
      <AfterLoaded
        defaultValues={schemaDefinitionPart} // SchemaDefinitionGlobalStateを渡す
        onSave={handleSave}                   // handleSaveはSchemaDefinitionGlobalStateを期待
      />
    )
  }

  // 上記以外は読み込みエラーとみなす
  return (
    <div>
      読み込みでエラーが発生しました: {loadError}
    </div>
  )
}

/** 画面初期表示時の読み込み完了後 */
const AfterLoaded = ({ defaultValues, onSave }: {
  defaultValues: SchemaDefinitionGlobalState
  onSave: (applicationState: SchemaDefinitionGlobalState) => void
}) => {

  const form = ReactHookForm.useForm<SchemaDefinitionGlobalState>({ defaultValues })
  const validationContext = useValidationContextProvider(form.getValues)

  // 型つきドキュメントのコンテキスト
  const typedDoc = useTypedDocumentContextProvider()

  // 保存処理
  const executeSave = React.useCallback(() => {
    onSave(form.getValues())
  }, [form, onSave])

  // Outletコンテキストの値
  const outletContextValue: NijoUiOutletContextType = React.useMemo(() => ({
    executeSave,
    formMethods: form,
    validationContext,
    typedDoc,
  }), [form, validationContext, typedDoc, executeSave])

  return (
    <AttrDefsProvider control={form.control}>
      <ValidationContext.Provider value={validationContext}>
        <ReactRouter.Outlet context={outletContextValue} />
      </ValidationContext.Provider>
    </AttrDefsProvider>
  )
}
