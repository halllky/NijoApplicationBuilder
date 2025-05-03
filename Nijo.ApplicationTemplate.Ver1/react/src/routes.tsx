import React from "react"
import * as ReactRouter from "react-router-dom"
import { Home } from "./pages/Home"
import { 顧客一覧検索 } from "./pages/顧客/顧客一覧検索"
import { 顧客詳細編集 } from "./pages/顧客/顧客詳細編集"
import { 従業員一覧検索 } from "./pages/従業員/従業員一覧検索"
import { 従業員詳細編集 } from "./pages/従業員/従業員詳細編集"
import { DEBUG_ROOMS_ROUTES } from "./debug-rooms"

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
  pages.push({ path: '顧客', element: <顧客一覧検索 />, sideMenuLabel: "顧客一覧" })
  pages.push({ path: '顧客/new', element: <顧客詳細編集 /> })
  pages.push({ path: '顧客/:顧客ID', element: <顧客詳細編集 /> })

  pages.push({ path: '従業員', element: <従業員一覧検索 />, sideMenuLabel: "従業員一覧" })
  pages.push({ path: '従業員/new', element: <従業員詳細編集 /> })
  pages.push({ path: '従業員/:従業員ID', element: <従業員詳細編集 /> })

  return pages
}