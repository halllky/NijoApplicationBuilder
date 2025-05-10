import { PageFrame, PageFrameTitle } from "../../layout";
import { NijoUi } from "./NijoUi";

export default function () {
  return (
    <PageFrame
      headerContent={(
        <PageFrameTitle>スキーマ定義編集UI</PageFrameTitle>
      )}
      className="p-8"
    >
      <NijoUi className="h-full w-full border border-gray-500" />
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
