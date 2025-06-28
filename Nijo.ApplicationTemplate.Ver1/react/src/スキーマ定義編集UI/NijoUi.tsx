import * as React from "react"
import * as ReactHookForm from "react-hook-form"
import * as ReactRouter from "react-router-dom"
import * as Layout from "../layout"
import * as Input from "../input"
import { NijoUiOutletContextType, SchemaDefinitionGlobalState } from "./types"
import { useTypedDocumentContextProvider } from "./型つきドキュメント/TypedDocumentContext"

/**
 * nijo.xmlをUIで編集できる画面の試作。
 * 最終的には、独立した WebView2 アプリケーションとして分離する予定。
 */
export const NijoUi = () => {

  // 型つきドキュメントのコンテキスト
  const typedDoc = useTypedDocumentContextProvider()

  // Outletコンテキストの値
  const outletContextValue: NijoUiOutletContextType = React.useMemo(() => ({
    typedDoc,
  }), [typedDoc])

  return (
    <ReactRouter.Outlet context={outletContextValue} />
  )
}
