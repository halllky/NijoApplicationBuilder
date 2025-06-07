import { RouteObjectWithSideMenuSetting } from "../routes"
import { NijoUi } from "./NijoUi"
import { NijoUiDebugMenu } from "./デバッグメニュー/DebugMenu"
import { NijoUiMainContent } from "./NijoUi"
import { ContextProviders } from "../App"
import { PerspectivePage } from "../型つきドキュメント/PerspectivePage"

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
