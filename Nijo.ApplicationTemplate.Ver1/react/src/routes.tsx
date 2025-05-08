import React from "react"
import * as ReactRouter from "react-router-dom"
import { Home } from "./pages/Home"
import { DEBUG_ROOMS_ROUTES } from "./debug-rooms"
import getQueryModelRoutes from "./pages"
import ComponentExampleIndex, * as examples from "./examples"
import { WordExample } from './examples/WordExample'
import { NumberInputExample } from './examples/NumberInputExample'
import { HyperLinkExample } from './examples/HyperLinkExample'
import { IconButtonExample } from './examples/IconButtonExample'
import { DateInputExample } from './examples/DateInputExample'
import { EditableGridExample } from './examples/EditableGridExample'
import { VFormExample } from './examples/VFormExample'
import { PageFrameExample } from './examples/PageFrameExample'

/** RouteObject に sideMenuLabel を追加した型 */
export type RouteObjectWithSideMenuSetting = ReactRouter.RouteObject & {
  /** この値がundefinedでないものは、サイドメニューに表示される。 */
  sideMenuLabel?: string
  children?: RouteObjectWithSideMenuSetting[]
}

/** ルーティング定義を取得する。 */
export const getRouter = (): RouteObjectWithSideMenuSetting[] => {
  const pages: RouteObjectWithSideMenuSetting[] = []

  // トップページ
  pages.push({ index: true, element: <Home />, sideMenuLabel: "ホーム" })

  // デバッグルーム。開発環境でのみ表示する。
  if (import.meta.env.DEV) {
    pages.push(...DEBUG_ROOMS_ROUTES)
  }

  // サンプル画面。開発環境でのみ表示する。
  if (import.meta.env.DEV) {
    pages.push({
      path: "/examples/",
      element: <ReactRouter.Outlet />,
      sideMenuLabel: "【開発用】UI実装例",
      children: [
        { index: true, element: <ComponentExampleIndex /> },
        { path: "word", element: <WordExample /> },
        { path: "number-input", element: <NumberInputExample /> },
        { path: "hyperlink", element: <HyperLinkExample /> },
        { path: "icon-button", element: <IconButtonExample /> },
        { path: "date-input", element: <DateInputExample /> },
        { path: "editable-grid", element: <EditableGridExample /> },
        { path: "vform", element: <VFormExample /> },
        { path: "page-frame", element: <PageFrameExample /> },
      ]
    })
  }

  // QueryModelの各種画面
  pages.push(...getQueryModelRoutes())

  return pages
}
