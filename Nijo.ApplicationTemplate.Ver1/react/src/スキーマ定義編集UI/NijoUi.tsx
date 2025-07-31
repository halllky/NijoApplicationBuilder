import * as React from "react"
import * as ReactHookForm from "react-hook-form"
import * as ReactRouter from "react-router-dom"
import * as Layout from "../layout"
import * as Input from "../input"
import { NijoUiOutletContextType } from "./types"
import { useTypedDocumentContextProvider } from "./型つきドキュメント/TypedDocumentContext"
import { Allotment, AllotmentHandle, LayoutPriority } from "allotment"
import { NijoUiSideMenu } from "./NijoUiSideMenu"

/**
 * nijo.xmlをUIで編集できる画面の試作。
 * 最終的には、独立した WebView2 アプリケーションとして分離する予定。
 */
export const NijoUi = () => {

  // 型つきドキュメントのコンテキスト
  const typedDoc = useTypedDocumentContextProvider()
  // サイドメニューのパネル
  const sideMenuPanelRef = React.useRef<AllotmentHandle>(null)
  // サイドメニューの可視状態
  const [sideMenuVisible, setSideMenuVisible] = React.useState(true)

  // Outletコンテキストの値
  const outletContextValue: NijoUiOutletContextType = React.useMemo(() => ({
    typedDoc,
    sideMenuPanel: {
      toggleCollapsed: () => {
        setSideMenuVisible(prev => !prev)
      },
    },
  }), [typedDoc])

  const handleVisibleChange = React.useCallback((paneIndex: number, visible: boolean) => {
    if (paneIndex === 0) {
      setSideMenuVisible(visible)
    }
  }, [])

  return (
    <Allotment
      ref={sideMenuPanelRef}
      separator={false} // 区切り線非表示
      proportionalLayout={false} // コンテナのサイズが変わったときに均等に伸縮しないようにする
      onVisibleChange={handleVisibleChange}
    >
      <Allotment.Pane
        priority={LayoutPriority.Low} // コンテナのサイズが変わったとき、この要素は伸縮しないようにする
        preferredSize={240}
        minSize={120}
        snap // 折り畳みできるようにする
        visible={sideMenuVisible} // 折り畳みできるようにする
      >
        <NijoUiSideMenu outletContext={outletContextValue} />
      </Allotment.Pane>
      <Allotment.Pane className="pl-1">
        <ReactRouter.Outlet context={outletContextValue} />
      </Allotment.Pane>
    </Allotment>
  )
}
