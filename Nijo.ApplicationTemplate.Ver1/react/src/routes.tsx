import React from "react"
import * as ReactRouter from "react-router-dom"
import { Home } from "./pages/Home"
import getQueryModelRoutes from "./pages"
import { getExamplePagesRoutes } from "./examples"
import SchemaDefinitionEditUI, { NIJOUI_CLIENT_ROUTE } from "./debug-rooms/スキーマ定義編集UIの試作"
import { getReflectionPages } from "./pages-reflection"

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

  // サンプル画面。開発環境でのみ表示する。
  if (import.meta.env.DEV) {
    pages.push(getExamplePagesRoutes())
  }

  // XMLスキーマ定義編集UI
  pages.push({ path: NIJOUI_CLIENT_ROUTE, element: <SchemaDefinitionEditUI />, sideMenuLabel: "【開発用】XMLスキーマ定義編集UIの試作" })

  // // QueryModelの各種画面
  // pages.push(...getQueryModelRoutes())

  // リフレクションを用いて自動生成された画面
  pages.push(...getReflectionPages())

  return pages
}
