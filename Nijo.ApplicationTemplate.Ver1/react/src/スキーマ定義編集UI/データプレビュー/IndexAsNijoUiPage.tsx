import * as Icon from "@heroicons/react/24/outline"
import DataPreview from "."
import { PageFrame } from "../PageFrame"

/** 自動生成されたあとのアプリケーションのwebapiのURL。とりあえず決め打ち */
export const BACKEND_URL = "https://localhost:7098"

export const DataPreviewAsNijoUiPage = () => {
  return (
    <PageFrame
      title="データプレビュー"
      headerComponent={(
        <span className="flex-1 text-xs text-gray-500">
          ※ 自動生成後のアプリケーションのwebapiを用いて動作しています。
          起動しない場合は、デバッグメニューからdotnetのデバッグ用サーバーを起動してください。
        </span>
      )}
    >
      <DataPreview
        backendUrl={BACKEND_URL}
        className="h-full border-t border-gray-300"
      />
    </PageFrame>
  )
}
