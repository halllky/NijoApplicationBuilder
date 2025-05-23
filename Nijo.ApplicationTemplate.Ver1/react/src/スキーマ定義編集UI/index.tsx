import { PageFrame, PageFrameTitle } from "../layout";
import { RouteObjectWithSideMenuSetting } from "../routes";
import { Outlet } from "react-router-dom";
import { NijoUi, NijoUiMainContent } from "./NijoUi";
import { NijoUiDebugMenu } from "./デバッグメニュー/DebugMenu";
import { ContextProviders } from "../App";
import { getNijoUiRoutesForEmbedded } from "./routing";

export * from "./routing"

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

/** ルーティングパス */
export const getNijoUiRoutesForDebug = (): RouteObjectWithSideMenuSetting[] => {
  const children = getNijoUiRoutesForEmbedded()
  delete children[0]['path']

  return [{
    path: '/nijo-ui',
    element: <SchemaDefinitionEditUI />,
    sideMenuLabel: "【開発用】XMLスキーマ定義編集UIの試作",
    children,
  }]
}
