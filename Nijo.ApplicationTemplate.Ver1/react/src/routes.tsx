import React from "react"
import * as ReactRouter from "react-router-dom"
import { Home } from "./pages/Home"
import { DEBUG_ROOMS_ROUTES } from "./debug-rooms"
import getQueryModelRoutes from "./pages"

/** RouteObject に sideMenuLabel を追加した型 */
export type RouteObjectWithSideMenuSetting = ReactRouter.RouteObject & {
  /** この値がundefinedでないものは、サイドメニューに表示される。 */
  sideMenuLabel?: string
  children?: never
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

  // QueryModelの各種画面
  pages.push(...getQueryModelRoutes())

  return pages
}