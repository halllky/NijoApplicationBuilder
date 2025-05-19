import { PageFrame, PageFrameTitle } from "../layout";
import { RouteObjectWithSideMenuSetting } from "../routes";
import { Outlet } from "react-router-dom";
import { NijoUi, NijoUiMainContent } from "./NijoUi";
import { NijoUiDebugMenu } from "./NijoUiDebugMenu";
import { ContextProviders } from "../App";

const SchemaDefinitionEditUI = () => {
  return (
    <>
      <style>
        {`
          .nijo-ui-debug-page-root {
            font-size: 16px;
            font-family: "Noto Sans JP", "BIZ UDGothic", sans-serif;
          }
        `}
      </style>
      <PageFrame
        headerContent={(
          <PageFrameTitle>スキーマ定義編集UI</PageFrameTitle>
        )}
        className="flex flex-col relative nijo-ui-debug-page-root"
      >
        <ul className="p-1 text-xs text-gray-600 list-inside list-disc">
          <li>
            nijo.xml をGUIから編集する画面。
            Windows Forms の WebView2 コントロールに埋め込んで使う。
          </li>
          <li>
            ビルド時は「npm run build:nijo-ui」を実行して、そのあとに「dotnet build Nijo.csproj」を実行する。
            Nijo.csproj のビルド時、nijo.exe の埋め込みリソースとしてこのReactアプリを埋め込んでいる。
            viteのmode機能を使い、modeが "nijo-ui" の場合は
            通常のアプリケーション全体ではなくこの下に見えている画面がメインに表示されるようにしている。
          </li>
          <li>
            デバッグ時は、「npm run dev」と「nijo.exe run-ui-service」を並行して実行する。
          </li>
        </ul>
        <div className="flex-1 p-8 overflow-hidden">
          <Outlet />
        </div>
      </PageFrame>
    </>
  )
}

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

/** ルーティングパス */
export const getNijoUiRoutesForDebug = (): RouteObjectWithSideMenuSetting[] => {
  return [{
    path: '/nijo-ui',
    element: <SchemaDefinitionEditUI />,
    sideMenuLabel: "【開発用】XMLスキーマ定義編集UIの試作",
    children: [{
      element: <NijoUi className="w-full h-full border border-gray-500" />,
      children: [{
        index: true,
        element: <NijoUiMainContent />,
      }, {
        path: `:aggregateId`,
        element: <NijoUiMainContent />,
      }, {
        path: 'debug-menu',
        element: <NijoUiDebugMenu />,
      }],
    }]
  }]
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
