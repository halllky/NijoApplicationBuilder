import { PageFrame, PageFrameTitle } from "../../layout";
import { NijoUi } from "./NijoUi";

export default function () {
  return (
    <PageFrame
      headerContent={(
        <PageFrameTitle>スキーマ定義編集UI</PageFrameTitle>
      )}
      className="flex flex-col"
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
        <NijoUi className="w-full h-full border border-gray-500" />
      </div>
    </PageFrame>
  )
}

// ----------------------------
// ナビゲーション

/** ナビゲーション用URLを取得する。 */
export const getNavigationUrl = (aggregateId?: string): string => {
  return `/nijo-ui/${aggregateId ?? ''}`
}

/** ルーティングパラメーター */
export const NIJOUI_CLIENT_ROUTE_PARAMS = {
  /** ルート集約単位の画面の表示に使われるID */
  AGGREGATE_ID: 'aggregateId',
}
/** ルーティングパス */
export const NIJOUI_CLIENT_ROUTE = `/nijo-ui/:${NIJOUI_CLIENT_ROUTE_PARAMS.AGGREGATE_ID}?`
