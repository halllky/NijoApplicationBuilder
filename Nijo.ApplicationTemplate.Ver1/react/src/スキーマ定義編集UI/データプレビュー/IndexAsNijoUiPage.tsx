import * as Icon from "@heroicons/react/24/outline"
import DataPreview from "."
import { ToTopPageButton } from "../ToTopPageButton"

/** 自動生成されたあとのアプリケーションのwebapiのURL。とりあえず決め打ち */
export const BACKEND_URL = "https://localhost:7098"

export const DataPreviewAsNijoUiPage = () => {
  return (
    <div className="flex flex-col gap-1 h-full overflow-y-auto">
      <div className="flex items-center gap-2 p-1">
        <ToTopPageButton />
        <Icon.ChevronRightIcon className="w-4 h-4" />
        <span className="font-semibold select-none">
          データプレビュー
        </span>
        <span className="flex-1 text-xs text-gray-500">
          ※ 自動生成後のアプリケーションのwebapiを用いて動作しています。
          起動しない場合は、デバッグメニューからdotnetのデバッグ用サーバーを起動してください。
        </span>
      </div>
      <DataPreview
        backendUrl={BACKEND_URL}
        className="flex-1 border-t border-gray-300"
      />
    </div>
  )
}
