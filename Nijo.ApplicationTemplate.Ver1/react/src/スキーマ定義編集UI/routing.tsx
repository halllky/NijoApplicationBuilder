import { RouteObjectWithSideMenuSetting } from "../routes"
import { NijoUi } from "./NijoUi"
import { NijoUiDebugMenu } from "./デバッグメニュー/DebugMenu"
import { NijoUiMainContent } from "./NijoUi"
import { ContextProviders } from "../App"
import { OutlinerPage } from "./型つきアウトライナー/OutlinerPage"

export const SERVER_DOMAIN = import.meta.env.DEV
  ? 'https://localhost:8081'
  : '';

// ----------------------------
// ナビゲーション

/** ナビゲーション用URLを取得する。 */
export const getNavigationUrl = (arg?
  : { aggregateId?: string, page?: never }
  | { aggregateId?: never, page: 'debug-menu' }
  | { aggregateId?: never, page: 'outliner', outlinerId: string }
): string => {
  if (arg?.page === 'debug-menu') {
    return '/nijo-ui/debug-menu'
  } else if (arg?.page === 'outliner') {
    return `/nijo-ui/outliner/${arg.outlinerId}`
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
      path: `schema/:aggregateId?`,
      element: <NijoUiMainContent />,
    }, {
      path: 'debug-menu',
      element: <NijoUiDebugMenu />,
    }, {
      path: 'outliner/:outlinerId',
      element: <OutlinerPage />,
    },
      // {
      //   path: 'typed-doc/entity-type/:entityTypeId',
      //   element: <EntityTypePage />,
      // }, {
      //   path: 'typed-doc/perspective/:perspectiveId',
      //   element: <PerspectivePage />,
      // }
    ]
  }]
}

/** ルーティングパラメーター */
export const NIJOUI_CLIENT_ROUTE_PARAMS = {
  /** ルート集約単位の画面の表示に使われるID */
  AGGREGATE_ID: 'aggregateId',
  OUTLINER_ID: 'outlinerId',
}
