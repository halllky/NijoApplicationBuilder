import React from "react"
import * as ReactRouter from "react-router-dom"
import { Home } from "./pages/Home"
import getQueryModelRoutes from "./pages"
import { getReflectionPages } from "./pages-reflection"
import { getExamplePagesRoutes } from "./examples"
import { getNijoUiRoutesForDebug } from "./スキーマ定義編集UI"
import { NijoUi } from "./スキーマ定義編集UI/NijoUi"
import { NijoUiDebugMenu } from "./スキーマ定義編集UI/デバッグメニュー/DebugMenu"
import { NijoUiMainContent } from "./スキーマ定義編集UI/NijoUi"
import { ContextProviders } from "./App"
import { PerspectivePage } from "./型つきドキュメント/PerspectivePage"
import { IS_EMBEDDED } from "./App"

/** RouteObject に sideMenuLabel を追加した型 */
export type RouteObjectWithSideMenuSetting = ReactRouter.RouteObject & {
  /** この値がundefinedでないものは、サイドメニューに表示される。 */
  sideMenuLabel?: string
  children?: RouteObjectWithSideMenuSetting[]
}

/** ルーティング定義を取得する。 */
export const getRouter = (): RouteObjectWithSideMenuSetting[] => {
  // Viteのmodeが nijo-ui のときはスキーマ定義編集UIのルートのみを返す
  if (IS_EMBEDDED()) {
    return getNijoUiRoutesForDebug()
  }

  // それ以外のモードの場合のルーティング
  const pages: RouteObjectWithSideMenuSetting[] = []

  // トップページ
  pages.push({ index: true, element: <Home />, sideMenuLabel: "ホーム" })

  // サンプル画面。開発環境でのみ表示する。
  if (import.meta.env.DEV) {
    pages.push(getExamplePagesRoutes())
  }

  // XMLスキーマ定義編集UI は nijo-ui モードで専用に表示するため、ここでは追加しません。
  // if (import.meta.env.DEV && !IS_EMBEDDED()) {
  //   pages.push(...getNijoUiRoutesForDebug())
  // }

  // // QueryModelの各種画面
  // pages.push(...getQueryModelRoutes())

  // リフレクションを用いて自動生成された画面
  pages.push(...getReflectionPages())

  return pages
}

export const SERVER_DOMAIN = import.meta.env.DEV
  ? 'https://localhost:8081'
  : '';

// ----------------------------
// ナビゲーション

/** ナビゲーション用URLを取得する。 */
export const getNavigationUrl = (arg?:
  { aggregateId?: string, page?: never } |
  { aggregateId?: never, page: 'debug-menu' } |
  { aggregateId?: never, page: 'outliner', outlinerId: string } |
  { aggregateId?: never, page: 'typed-document-entity', entityTypeId: string } |
  { aggregateId?: never, page: 'typed-document-perspective', perspectiveId: string, focusEntityId?: string }
): string => {
  if (arg?.page === 'debug-menu') {
    return '/nijo-ui/debug-menu'
  } else if (arg?.page === 'outliner') {
    return `/nijo-ui/outliner/${arg.outlinerId}`
  } else if (arg?.page === 'typed-document-entity') {
    return `/nijo-ui/typed-doc/entity-type/${arg.entityTypeId}`
  } else if (arg?.page === 'typed-document-perspective') {
    const searchParams = new URLSearchParams()
    if (arg.focusEntityId) searchParams.set(NIJOUI_CLIENT_ROUTE_PARAMS.FOCUS_ENTITY_ID, arg.focusEntityId)
    return `/nijo-ui/typed-doc/perspective/${arg.perspectiveId}?${searchParams.toString()}`
  } else {
    return `/nijo-ui/schema/${arg?.aggregateId ?? ''}`
  }
}

export const getNijoUiRoutesForEmbedded = (): RouteObjectWithSideMenuSetting[] => {
  return [{
    path: '/nijo-ui',
    element: (
      <ContextProviders>
        <NijoUi className="w-full h-full border border-gray-500" />
      </ContextProviders>
    ),
    children: [{
      path: `schema/:${NIJOUI_CLIENT_ROUTE_PARAMS.AGGREGATE_ID}?`,
      element: <NijoUiMainContent />,
    }, {
      path: 'debug-menu',
      element: <NijoUiDebugMenu />,
    }, {
      path: `typed-doc/perspective/:${NIJOUI_CLIENT_ROUTE_PARAMS.PERSPECTIVE_ID}`,
      element: <PerspectivePage />,
    }]
  }]
}

/** ルーティングパラメーター */
export const NIJOUI_CLIENT_ROUTE_PARAMS = {
  /** ルート集約単位の画面の表示に使われるID */
  AGGREGATE_ID: 'aggregateId',
  OUTLINER_ID: 'outlinerId',
  /** 型つきドキュメントの画面の表示に使われるID */
  PERSPECTIVE_ID: 'perspectiveId',
  /** 型つきドキュメントの画面の初期表示にフォーカスが当たるエンティティのID（クエリパラメータ） */
  FOCUS_ENTITY_ID: 'f',
}
