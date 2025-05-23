import { RouteObjectWithSideMenuSetting } from "../routes"
import { NijoUi } from "./NijoUi"
import { NijoUiDebugMenu } from "./デバッグメニュー/DebugMenu"
import { NijoUiMainContent } from "./NijoUi"
import { ContextProviders } from "../App"
// ----------------------------
// ナビゲーション

/** ナビゲーション用URLを取得する。 */
export const getNavigationUrl = (arg?
  : { aggregateId?: string, page?: never }
  | { aggregateId?: never, page: 'debug-menu' }
): string => {
  if (arg?.page === 'debug-menu') {
    return '/nijo-ui/debug-menu'
  } else {
    return `/nijo-ui/${arg?.aggregateId ?? ''}`
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
      index: true,
      element: <NijoUiMainContent />,
    }, {
      path: `:aggregateId`,
      element: <NijoUiMainContent />,
    }, {
      path: 'debug-menu',
      element: <NijoUiDebugMenu />,
    }]
  }]
}

/** ルーティングパラメーター */
export const NIJOUI_CLIENT_ROUTE_PARAMS = {
  /** ルート集約単位の画面の表示に使われるID */
  AGGREGATE_ID: 'aggregateId',
}
