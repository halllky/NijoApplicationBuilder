import * as React from "react"
import * as ReactHookForm from "react-hook-form"
import * as ReactRouter from "react-router-dom"
import * as Layout from "../layout"
import * as Input from "../input"
import { NijoUiOutletContextType } from "./types"
import { useTypedDocumentContextProvider } from "./型つきドキュメント/TypedDocumentContext"
import { PanelGroup, Panel, PanelResizeHandle, ImperativePanelHandle } from "react-resizable-panels"
import { NijoUiSideMenu } from "./NijoUiSideMenu"

/**
 * nijo.xmlをUIで編集できる画面の試作。
 * 最終的には、独立した WebView2 アプリケーションとして分離する予定。
 */
export const NijoUi = () => {

  // 型つきドキュメントのコンテキスト
  const typedDoc = useTypedDocumentContextProvider()
  // サイドメニューのパネル
  const sideMenuPanelRef = React.useRef<ImperativePanelHandle>(null)

  // Outletコンテキストの値
  const outletContextValue: NijoUiOutletContextType = React.useMemo(() => ({
    typedDoc,
    sideMenuPanelRef,
  }), [typedDoc, sideMenuPanelRef])

  return (
    <PanelGroup direction="horizontal">
      <Panel ref={sideMenuPanelRef} defaultSize={30} collapsible minSize={8}>
        <NijoUiSideMenu outletContext={outletContextValue} />
      </Panel>
      <PanelResizeHandle className="w-1" />
      <Panel className="relative">
        <ReactRouter.Outlet context={outletContextValue} />
      </Panel>
    </PanelGroup>
  )
}
